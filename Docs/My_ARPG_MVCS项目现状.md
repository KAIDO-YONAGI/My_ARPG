# My_ARPG MVCS 项目现状

本文记录 My_ARPG 分层重构的工程现状：已迁移的线、未迁移的域、已确认缺陷、待办与测试现状。只写当前成立的事实，路径与代码一致。

## 1. 玩家数值这条线已经分层

### 1.1 文件与职责

| 文件 | 层 | 职责 |
| --- | --- | --- |
| `Gameplay/Player/Models/PlayerStatsModel.cs` | Model | 玩家状态与规则：15 个字段（经 `PlayerStatsData`）、经验曲线、四个事件 |
| `Gameplay/Player/Models/IPlayerStatsReadOnly.cs` | 契约 | 玩家数值的只读查询与事件视图；外部可以读和订阅，不能调用 Model 写方法 |
| `Gameplay/Player/Models/PlayerStatsData.cs` | Model | 存档传输格式，字段名就是存档 JSON 的键，`Clone()` 提供字段级拷贝 |
| `Gameplay/Player/Services/StatsService.cs` | Service | 私有持有 `PlayerStatsModel`；对外暴露 `IPlayerStatsReadOnly Stats` 与薄写转发，负责 `Respawn()`、存档注册/注销和读写生命周期 |
| `Gameplay/Player/Controllers/ExperienceController.cs` | Controller | 纯 C#；经构造注入订阅 `EnemyDefeated` 通道与 `ExpChanged`，推经验条数据；由 `ExperiencePanelView` 托管 |
| `Gameplay/Player/Controllers/HealthController.cs` | Controller | 纯 C#；订阅 `HealthChanged`，推血量文本；由 `HealthView` 托管 |
| `Gameplay/Player/Controllers/StatsPanelController.cs` | Controller | 纯 C#；订阅 `StatsChanged`，推属性面板；由 `StatsPanelView` 托管 |
| `Gameplay/Player/Controllers/PlayerDamageController.cs` | Controller | 输入侧；订阅 `PlayerDamaged` 广播，扣血、击退、死亡编排 |
| `Gameplay/Player/Views/ExperiencePanelView.cs` | View | 经验条与等级文本的场景组件，持击杀通道的序列化引用，托管 `ExperienceController` |
| `Gameplay/Player/Views/HealthView.cs` | View | 血量文本的场景组件，托管 `HealthController` |
| `Gameplay/Player/Views/StatsPanelView.cs` | View | 属性面板的场景组件，托管 `StatsPanelController`；兼管画布开关/焦点/层级（`ICanvasManager` 是全 UI 共用基建） |
| `Gameplay/Player/PlayerLocator.cs` | 定位器 | 给存档系统提供玩家坐标 |
| `Pipeline/SO/PlayerStatsSO.cs` | 配置 | 初始值模板，`CreateInitialData()` 导出拷贝 |
| `Pipeline/SO/Events/PlayerDamagedEventSO.cs` | 事件通道 | 玩家受击广播：伤害值、攻击者、击退力、眩晕时长 |
| `Pipeline/SO/Events/EnemyDefeatedEventSO.cs` | 事件通道 | 敌人死亡广播：经验奖励、死者 Transform |

数值域所有类都在 `Gameplay.Player.*` 命名空间下；三个 SO 类（`PlayerStatsSO` 与两个事件通道）在全局命名空间。三个 View 都是普通 `MonoBehaviour`（不挂单例），每个面板只有这一个场景组件，Inspector 只接控件引用。

### 1.2 数据怎么走

加经验：`EnemyHealth` 死亡时广播 `EnemyDefeatedEventSO` 通道（经验奖励与死者 Transform）→ `ExperienceController.GainExp` → `StatsService.AddExperience` → `PlayerStatsModel.AddExp` 完成升级结算，先广播 `ExpChanged` 刷经验条，后广播 `LevelUp` → `SkillTreeManager.UpdateAbilityPoints` 调 `StatsService.UpdateSkillPoints` 加点并直接刷新点数文本，整条链在一次调用栈内完成。

受击：`EnemyCombat.Attack` 广播 `PlayerDamaged`（伤害、攻击者、击退参数）→ `PlayerDamageController.OnDamaged` → `StatsService.UpdateHealth(-damage)` 与 `PlayerMovement.KnockBack`；血量归零时请求 GameOver 画布并隐藏玩家根节点。

技能点由 `SkillTreeManager` 订阅 `StatsService.Instance.Stats.LevelUp` 发放，读技能点经 `Stats`，写入经 `StatsService.UpdateSkillPoints`。

读档：`SaveDataManager.LoadFromData` 遍历 `SaveRegistry.All` → `StatsService.LoadData` → `PlayerStatsModel.LoadFrom` → 广播 `HealthChanged`、`StatsChanged`、`ExpChanged` → 血量文本、属性面板、经验条同时刷新。

存档：`SaveDataManager` 遍历 `SaveRegistry.All` → `StatsService.SaveData` → 私有 `model.ToData()` 写入 `data.playerStatsData`。

启动：`StatsService` 的 `OnSingletonInitialized` 一次性完成建模型与存档注册——注册进静态 `SaveRegistry`，先于一切实例存在，无时序依赖、无需重试；销毁时由基类注销。

### 1.3 读与写的约定

读数值：`StatsService.Instance.Stats.Damage` 这类只读属性；`Stats` 的静态类型是 `IPlayerStatsReadOnly`，外部只能查询和订阅。

改数值：`StatsService.Instance.UpdateHealth(...)`、`AddExperience(...)` 等 Service 写命令。Service 只做边界转发，不复制数值规则；具体钳制、经验结算和事件广播仍在 `PlayerStatsModel`。`Respawn()` 自带业务规则（仅死亡时回满血），`GetStats`/`LoadStats` 与具体 `model` 都是私有实现，供存档流程使用。

存档只经 `ISaveable` 一条通道。

跨域调用写方法的现状：`InventoryManager` 的经验道具走 `StatsService.AddExperience`，`UseItem` 走 `StatsService` 的血量、速度、伤害和上限命令；命令最终进入 Model，写方法自带守卫，`AddExp` 忽略非正数，血量钳制在写方法内，单聚合不变量不被绕过。数值域没有需要一次原子改两个聚合的用例，`StatsService` 因此不复制规则，只承担生命周期、边界和 `Respawn`。

### 1.5 当前边界结论

玩家数值线采用轻量 MVCS，而不是把每个动作继续细分成更多层：

- `PlayerStatsModel` 是纯 C# 的状态与规则实现，只由 `StatsService` 私有持有；它仍可在 EditMode 测试中直接 `new`。
- `StatsService` 是 Unity 单例、存档身份和外部写入口，同时保留跨场景的 `Respawn()` 业务。
- `IPlayerStatsReadOnly` 只暴露查询与事件，业务类不再拿到具体 Model，也不能绕过 Service 修改数值。
- Service 的转发方法只负责边界，不复制 Model 的规则；因此不会形成两套实现。

### 1.4 经验曲线与守卫

```text
expToUpgrade += ((expToUpgrade / 10) * 10 * expMultiplier) / 4
```

`(expToUpgrade / 10) * 10` 是整数除法，等于把阈值截断到 10 的整数倍；阈值小于 10 时截断结果为 0，步长为 0，阈值保持不变。末尾按 `int` 截断：`(10 × 1.5) / 4 = 3.75 → 3`。

起始 10、算子 1.5 的序列：10 → 13 → 16 → 19 → 22 → 29 → 36 → 47 → 62 → 84 → 114 → 155 → 211 → 289。

两个旋钮：资产里的 `expMultiplier`，当前 1.5；模型里的 `ExpGrowthDivisor`，当前 4。

守卫：

1. 阈值下限 `MinExpToUpgrade`，取值 1。构造函数与 `LoadFrom` 两个入口都经私有的 `RepairExpThreshold` 兜底，升级循环的条件里有它，`GrowExpToUpgrade` 里也有它；没有下限时 `currentExp >= expToUpgrade` 会恒成立。
2. `AddExp` 忽略非正数，负数留下警告。
3. `maxLevel` 为 0 时不限制等级，上限判断在升级循环的条件里。

读档原样采用存档里的阈值，不从等级反算；低于下限的坏档值在 `LoadFrom` 与构造函数里统一修回 `MinExpToUpgrade`。早前只有 `GrowExpToUpgrade` 兜底、两个入口不兜底，同一份阈值在增长路径被判非法、在读档路径被判合法；坏档（阈值为 0）会让升级循环条件恒不成立，玩家永远升不了级。兜底落在 Model 而不是 `PlayerStatsData`——后者是存档传输格式，不带规则。

### 1.5 事件

| 事件 | 触发时机 | 订阅者 |
| --- | --- | --- |
| `HealthChanged` | 血量或上限变化、读档 | `HealthController`（推给 `HealthView`） |
| `StatsChanged` | 速度、伤害变化、读档 | `StatsPanelController`（推给 `StatsPanelView`） |
| `LevelUp` | 结算里产生升级 | `SkillTreeManager` |
| `ExpChanged` | 加经验结算后、读档后 | `ExperienceController`（推给 `ExperiencePanelView`） |
| `PlayerDamaged`（SO 事件通道） | 伤害源广播 | `PlayerDamageController` |
| `EnemyDefeated`（SO 事件通道） | 敌人死亡 | `ExperienceController`（加经验） |

Controller 的订阅在面板失活期间保持：`SetXxx` 只写序列化属性，对失活对象安全，重激活时数值天然最新，无需补刷。面板销毁时在 `OnDestroy` 里恰好一次调用 `Dispose` 退订——通道与模型的生命周期长于面板，不退订会让订阅跨局残留。

`UpdateSkillPoints` 只更新技能点字段、不广播事件；技能树的点数文本由 `UpdateAbilityPoints` 经 `StatsService` 写入后再同步 `pointsText`。事件省略的前提是唯一消费者就是写入者。

---

## 2. 还没分层的功能（已冻结的剩余清单）

> **本节是冻结的剩余清单，不是进行中的待办。** 分层重构已收束：玩家数值这条线迁完即停，下表的功能域不再继续迁移——它们只是把已成文的九步流程再跑一遍，边际结论为零。唯一有意留下的验证点是**技能域**（§2.2）：它是「一次写要原子地改动两个数据聚合」这条准则目前唯一未被本工程验证的场景（技能点属于数值聚合，消耗发生在技能聚合），其余五域都是既有结论的重复应用。

现状的共同问题：一个类里叠了好几层。下表列出每个功能的现状与拆分方向。

| 功能 | 现状问题 | 拆分方向 | 完成判据 |
| --- | --- | --- | --- |
| 任务 | `QuestManager` 同时装任务数据、状态机、发奖、面板显隐、任务槽刷新 | `QuestProgressModel` 存目标进度，`QuestRuntimeModel` 存状态，`QuestService` 做接取、推进、完成、发奖，`QuestBoardController` 转发输入，`QuestLogView` 只读显示 | 打开任务面板不改变任何进度；状态只能由 Service 改成合法值；奖励只发一次 |
| 对话 | `DialogManager` 同时装会话流程、条件判断、节点跳转、按钮监听、文本与立绘显示，UI 字段全在它身上 | `DialogSessionModel` 存当前节点与历史，`DialogConditionService` 做条件判断，`DialogService` 做开始、选择、跳转、结束，`DialogView` 承接 UI 字段 | 节点跳转能在不加载场景 UI 的测试里跑；关掉对话后按钮监听没有残留 |
| 背包与商店 | `InventoryManager` 装背包数据、金币、拾取、商店校验、买卖、掉落池、物品使用、金币 UI；`InventorySlot` 同时依赖 `ShopManager.Instance` 与 `InventoryManager.Instance` | `InventoryModel` 存槽位与金币，`InventoryService` 做增删堆叠使用，`ItemEffectService` 做物品效果，`ShopModel` 与 `ShopService` 做商品与买卖，View 只读显示 | 金币不足、背包已满、商品不存在都返回明确结果；交易失败时背包与金币不变 |
| 存档与场景 | 存档边界只传纯数据（`SaveData` 三字段全为值类型，`Vector3` 经 `SerializableVector3`），文件读写收在 `SaveSystem`；剩余问题是动态存档清理未覆盖任务/物品栏/背包（`SaveDataManager` 内 TODO），保存与读档的编排挂在保存面板的按钮回调里 | 保存/读档编排从面板按钮收进 Service；动态数据清理补齐三域；场景加载统一经 `SceneChanger.RequestSceneLoad` 入口 | 存档读写能脱离 UI 执行；读档失败不会把运行状态改一半 |
| 移动、战斗、寻路 | 输入、状态、表现混在 `PlayerMovement`、`PlayerCombat`、`PlayerBow` 里；`PlayerMovement` 另有一个 static 定位入口 `Main` | `PlayerInputAdapter` 读输入，`MovementModel` 存移动状态，`CombatService` 做攻击与冷却，`EquipmentService` 做装备切换，`PlayerView` 承接 Animator 与特效 | 行为不变，只换入口 |
| 技能 | `SkillSlot` 同时持有状态（等级/解锁/前置图）、升级规则、static 事件广播与 UI；`SkillTreeManager` 兼管点数扣减与依赖解锁；`SkillManager` 用技能名字符串分发效果；技能等级与解锁不入存档，技能点数随 `PlayerStatsData` 入档 | `SkillSystemModel`（等级/解锁/前置图+规则+事件）、`SkillService`（唯一写入口，点数读写经 `StatsService`）、`SkillSlot` 瘦成纯 View、`SkillTreeManager` 拆 View+Controller、`SkillManager` 解体为 `SkillEffectApplier` | 升级只能经 `SkillService.TryUpgrade`；两个 static 事件消失，由模型事件替代；效果分发编译期可查 |

任务面板的显示与数据已解耦：`QuestLogPanel.HandleQuestClicked` 只调 `QuestManager.Instance.OpenQuest` 并刷新自身文本，状态与进度都由 `QuestManager` 在内部完成，`RefreshObjectiveProgress` 按物品与对话历史重算目标进度。任务域剩余的问题集中在 `QuestManager` 一个类里混装四责。

任务这条线按九步流程拆：先抽 `QuestProgressModel` 与 `QuestRuntimeModel` 装进度与状态，再把推进、完成、发奖收进 `QuestService` 作为唯一写入口，接着把 `QuestLogPanel` 的显示部分独立成 `QuestLogView`，最后让"打开面板"这个动作只调用 `QuestService`。拆分顺序从任务开始：它的写入路径最少。技能域方案已定，两个设计点拍板后可随时插入。

### 2.1 已确认的缺陷

**D1：出售经验道具会同时出三个问题。**

出售时 `ShopManager.SellItem()` 用负数价格与数量 -1 发事件，`InventoryManager.HandleShopping()` 收到后先结算金币，再调 `UpdateInventorySlots(item, -1)`，而这个方法里的 `isEXP` 分支没有判断数量正负就加经验并 `return`。后果有三条：金币照样增加，因为结算在判断物品类型之前；道具留在背包里，因为 `return` 跳过了通用出售分支；第三条由 `PlayerStatsModel.AddExp` 忽略非正数挡住。

修的方向要先定设计意图。允许出售，就把 EXP 分支限制在正数上，负数落进通用出售分支；不允许出售，就明确拒绝并留下提示。

**D2：技能效果按名字字符串分发。**

`SkillManager.HandleAbilityPointSpent` 用 `switch (skillSO.skillName)` 比较 `"MaxHealthBoost"`、`"SwordSlash"` 这类字符串派发效果，技能改名即静默失效。技能域迁移时改为按 `SkillSO` 引用或类型分发（编译期可查），效果应用收进 `SkillEffectApplier`。

**D3：技能点的校验与扣减分裂在两个类、两个时刻。**

点击时 `SkillTreeManager.Start` 的 onClick 闭包先查 `SkillPoints > 0` 才调 `TryUpgradeSkill`，升级后 `HandleAbilityPointSpent` 事件回调里再扣减点数。校验和写入分居两处，中间隔着事件广播。收进 `SkillService.TryUpgrade` 原子完成。

### 2.2 技能域：迁移方案（方向已定，待动工）

**事件处置判定**：`SkillSlot.OnAbilityPointSpent` 与 `OnMaxSkillLevel` 的订阅方（SkillManager、SkillTreeManager）全部在技能域内，走 Model 的 C# 事件，**不建 SO 通道**。对照案例是 `EnemyDefeatedEventSO`：它有跨域消费者（经验系统，将来任务域的击杀目标同用这条通道），`EnemyHealth` 持序列化引用 Raise（接线只在 TorchGoblin_Red 预制体一处），`ExperienceController` 经构造函数注入订阅（`ExperiencePanelView` 持序列化引用传入）。判定标准是消费者是否跨域：域内走模型事件，跨域输入走通道。

**目标结构**：

```text
SkillSlot(点击) → SkillTreeController → SkillService.TryUpgrade(skill)
                                        ↓ 唯一写入口：校验点数+等级+解锁 → 写 Model
SkillSystemModel（纯C#）：per-skill 等级/解锁、前置依赖图、规则
事件：SkillUpgraded / SkillMaxed
      ├→ SkillTreeController 刷槽位显示与点数文本
      └→ SkillEffectApplier（场景组件，持 PlayerCombat 引用）应用效果
```

**步骤**（按九步流程映射）：

1. `SkillSystemModel`：`Dictionary<skillId, level>` + 解锁集合 + 前置图；规则校验与事件都在写方法里发出。技能点不复制进本模型——它是升级域的货币，`SkillService` 通过 `StatsService.Instance.Stats.SkillPoints` 查询，并通过 `StatsService.UpdateSkillPoints` 写入。
2. `SkillSO` 继承 `GuidSO` 拿稳定 id（为将来存档铺路；`GuidSO` 在 `Pipeline/SO/`，`CharacterSO` 是现有使用者）；前置依赖图从槽位的场景接线（`preRiquriedForSkillUnlock_List`）挪进 `SkillSO` 资产——依赖关系是配置数据，不是场景接线。
3. `SkillService : YSingleton`（域数据入口，符合单例预算），持 Model，暴露 `TryUpgrade/CanUnlock`。
4. `SkillSlot` 瘦成纯显示 + 点击转发；`SkillTreeManager` 拆成 View（面板基建+点数文本）+ 纯 C# Controller（订阅模型事件推显示）；`SkillManager` 解体，效果分发进 `SkillEffectApplier`（修 D2）。
5. `TryUpgrade` 原子完成"校验点数 + 扣减 + 升级"（修 D3）。
6. 建立 `SkillSystemModel` 的 EditMode 回归网：升级、前置解锁、满级守卫、点数不足拒绝。

**动工前待拍板**：

- 技能等级与解锁要不要存档？现状不存：`SaveData.cs` 顶层无技能字段，技能点数随 `PlayerStatsData` 入档，每局重置。要存则照 `PlayerStatsData` 模式补 DTO + `ISaveable`；确认不存则暂不接线，但 GuidSO 的 id 先铺。
- 前置图进 SO 需要重接资产（技能数量少，成本可忽略）；退化方案是 View 启动时把槽位接线报给 Model——能用，但把配置数据留在了场景层，不推荐。

### 2.3 剩余清单（方案已定，随冻结一并归档）

- build 局间复位收尾：切往 Menu 时清动态存档数据在 `SaveDataManager.OnAutoSave` 里实现（`DynamicDataHandler.ClearDynamicData`，Menu 场景切出时触发）；剩余部分是任务/物品栏/背包三域接入动态存档，以及 `SaveRegistry.Clear()` 的调用点建立——重开新局不重启进程，静态注册表需要在这时清空。
- static 状态纪律：新增持有 static 状态的类时，Domain Reload 关闭场景的清空要集中处理（`SaveRegistry` 已内置；剩余两个 static 事件在 `SkillSlot`，靠订阅方成对退订兜底，随技能域迁移消灭）。

---

## 3. 测试现状

### 3.1 现有用例

`Assets/Tests/Editor/` 下 4 个文件，命名空间 `Gameplay.Tests`，共 46 个用例：

| 文件 | 用例数 | 覆盖 |
| --- | --- | --- |
| `PlayerStatsModelTests.cs` | 24 | 钳制规则、事件广播、经验曲线、等级上限、拷贝语义、`ExpChanged`、只读接口与 Service 边界 |
| `CanvasFocusStackTests.cs` | 15 | Canvas 焦点栈的开闭顺序、order 计算、ESC 行为 |
| `ObjectPoolTests.cs` | 5 | 对象池的预热注册、Get/Return 回调、池内对象销毁后的选取 |
| `PlayerStatsSOTests.cs` | 2 | 模板的拷贝语义 |

加上 Addressables 包自带的 1 个，Test Runner 里共 47 个。工程里 0 个 asmdef，产品代码全在预定义程序集 `Assembly-CSharp` 里。

`PlayerStatsModelTests` 是玩家数值这条线的回归网：改 `PlayerStatsModel`、`StatsService`、`ExperienceController` 之前先跑绿。

当前执行 `PlayerStatsModelTests` 的 24 个用例中 21 个通过、3 个失败：`ExpCurve_HugeGain_GainsSeveralLevels_AndTerminates`、`ExpCurve_WithZeroBase_IsFlooredToKeepCurveAlive`、`LoadFrom_FloorsGarbageThresholdToAtLeastOne`。本次新增的 `Model_CanBeViewedThroughReadOnlyStatsInterface` 与 `StatsService_ExposesOnlyReadOnlyStatsView` 均通过；剩余失败集中在原有经验曲线与读档阈值预期，和本次 Service/Model 边界调整无关。

### 3.2 建议先落地的测试

| 编号 | 内容 | 层 |
| --- | --- | --- |
| T1 | 存档往返：掉落物的位置与拾取状态只记改动量 | 一 |
| T2 | 场景接线检查：事件 SO、`ShopSlot.shopRef`、掉落物 ID、`sceneReference`、Addressables 失效引用 | 二 |
| T3 | D1 复现：出售经验道具时金币照加、道具不移除 | 一 |
| T4 | 任务面板只读回归：打开任务面板不改变任务进度 | 一 / 二 |
| T5 | PlayMode 发烟：进场景、注册的掉落物数等于场景里的数量、存档往返 | 三 |
| T6 | 技能模型规则：升级/前置解锁/满级守卫/点数不足拒绝（随技能域动工建立） | 一 |

### 3.3 本机命令行跑 EditMode

```powershell
& "D:\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe" `
  -runTests -batchmode `
  -projectPath "D:\Unity\Projects\My_ARPG" `
  -testPlatform EditMode `
  -testResults "D:\Unity\Projects\My_ARPG\Logs\test-editmode.xml" `
  -logFile      "D:\Unity\Projects\My_ARPG\Logs\test-editmode.log"
```

前提是 Unity 编辑器没有开着这个工程，否则第二个实例起不来；结果看 XML 根节点的 `result` 与失败计数，退出码不可靠。Play 流程实际写盘的存档目录是 `%USERPROFILE%\AppData\LocalLow\YONAGI\My_ARPG`。

---

## 4. 单例与执行序现状

工程里 18 个具体类直接继承 `YSingleton<T>`，另有抽象基类 `SaveableService<TSelf>`——`StatsService` 经它间接继承；基类在 `Assets/Scripts/Contracts/YSingleton.cs`。符合单例预算的数据入口与全局基建：`StatsService`、`SaveDataManager`、`SaveSystem`、`UIManager`、`SceneChanger`、`TimeManager`。玩家数值域只有两个单例：`StatsService` 与 `PlayerLocator`。

初始化时序三层解法的工程载体：静态注册表 `Contracts/SaveRegistry.cs`（`ResetStatics` 清空，`Clear` 供局间复位）；CRTP 基类 `Contracts/SaveableService.cs`（注册/注销/存档身份一体，子类只实现 `SaveData/LoadData`）；显式入口 `SceneChanger.RequestSceneLoad`（先广播后执行，不自订阅）。全工程的执行序属性只剩 `Pipeline/Scene/CameraPixelSnap.cs` 的 10000，语义是末帧像素修正，属于管线收尾而非时序依赖。
