# 玩家数值与经验线（PlayerStats）

文档 ID：`GP-PLAYERSTATS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本域只负责 `PlayerStatsModel` / `PlayerStatsData` / `IPlayerStatsReadOnly` / `StatsService` / `PlayerStatsSO` 这条数值线的当前实现，以及挂在它上面的三个显示 Controller、四个跨域写入调用点；不负责技能树规则本身、背包装备的物品效果设计、存档文件格式与 UI 布局。
上游来源：

- 旧文档 `Docs/My_ARPG_MVCS项目现状.md`（第 1 节与 1.4 节，与本域代码一致）
- 旧文档 `Docs/UML/02_Player_System.puml`（已过期，见 §4 D5）
- 代码 `Assets/Scripts/Gameplay/Player/Models/**`、`Services/StatsService.cs`、`Controllers/**`、`Views/**`
- 代码 `Assets/Scripts/Pipeline/SO/PlayerStatsSO.cs` + 资产 `Assets/GameSO/PlayerStatsSO.asset`
- 代码 `Assets/Scripts/Gameplay/Player/Controllers/PlayerDamageController.cs`（重试复活的编排点，本轮由 `SceneChanger` 迁入）、`Assets/Scripts/Pipeline/Scene/SceneChanger.cs`、`Assets/Scripts/Gameplay/Save/SaveDataManager.cs`
- 资产 `Assets/GameSO/Events/VoidEvents/RetryRequestEvent.asset`、`Assets/Scripts/Pipeline/UI/Buttons/RetryButton.cs`
- 测试 `Assets/Tests/Editor/PlayerStatsModelTests.cs`、`Assets/Tests/Editor/PlayerStatsSOTests.cs`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `血量` `HP` `扣血` `回血` `Respawn` | §2.2、§2.9、§3.2 |
| `重试` `RetryRequestEvent` `RetryButton` `GameOver` | §2.9、§3.2 |
| `经验` `升级` `LevelUp` `阈值` `expToUpgrade` `经验曲线` | §2.4、§3.1 |
| `技能点` `SkillPoints` `加点` | §2.4、§3.1（无钳制） |
| `攻击力` `速度` `武器范围` `击退` `冷却` | §2.2、§2.5 |
| `StatsService` `Stats` `IPlayerStatsReadOnly` `只读视图` | §2.1、§2.2、§3.5 |
| `PlayerStatsSO` `初始值` `模板` `资产污染` | §2.6、§3.3 |
| `playerStatsData` `读档` `存档数值` `坏档` | §2.7、§3.4 |
| `26 个测试` `EditMode` `回归网` | §2.8 |

## 2. 当前实现

### 2.1 四层职责与依赖方向

依赖是单向的：`View → Controller → StatsService → PlayerStatsModel`，Model 不反向引用任何上层。

| 层 | 类型 | 文件 | 关键事实 |
|---|---|---|---|
| Model | 纯 C#（非 MonoBehaviour） | `Assets/Scripts/Gameplay/Player/Models/PlayerStatsModel.cs` | 只 `using System/UnityEngine`（:1-2），不引用 Services |
| 契约 | interface | `Models/IPlayerStatsReadOnly.cs:9` | 4 个事件 + 15 个只读属性 |
| DTO | `[Serializable]` 普通类 | `Models/PlayerStatsData.cs:12` | 15 个 public 字段 + `Clone()`（:35） |
| Service | `SaveableService<StatsService>` | `Services/StatsService.cs:16` | 私有持有 model（:21），唯一写入口 |
| Controller | 3 个纯 C# + 1 个 MonoBehaviour | `Controllers/*.cs` | 只读经 `IPlayerStatsReadOnly`（`HealthController.cs:30`、`ExperienceController.cs:39`、`StatsPanelController.cs:30`） |
| View | MonoBehaviour | `Views/HealthView.cs:12`、`ExperiencePanelView.cs:13`、`StatsPanelView.cs:13` | 只碰控件，在 `Start` 里造 Controller 并首刷 |
| 配置 | `ScriptableObject` | `Assets/Scripts/Pipeline/SO/PlayerStatsSO.cs:8` | 全局命名空间（不在 `Gameplay.Player.*`） |

`StatsService` 的 `model` 字段是 private（:21），对外只给 `public IPlayerStatsReadOnly Stats => model;`（:24）。`GetStats()` / `LoadStats()` 是 private（:53、:56），仅存档流程使用。

### 2.2 唯一写入口：StatsService 的转发边界

Service 只做边界转发，不复制数值规则；钳制、结算、事件都在 Model 内。

| 服务方法 | 行 | 转发目标 | 附加规则 |
|---|---|---|---|
| `UpdateMaxHealth(int)` | :40 | `model.UpdateMaxHealth` | 无 |
| `UpdateHealth(int)` | :42 | `model.UpdateHealth` | 无 |
| `UpdateSpeed(float)` | :44 | `model.UpdateSpeed` | 无 |
| `UpdateDamage(int)` | :46 | `model.UpdateDamage` | 无 |
| `UpdateSkillPoints(int)` | :48 | `model.UpdateSkillPoints` | 无 |
| `AddExperience(int)` | :50 | `model.AddExp` | 注意：Model 侧名字是 `AddExp`，服务侧是 `AddExperience` |
| `Respawn()` | :32-36 | `model.SetCurrentHealth(MaxHealth)` | 带业务规则：仅 `CurrentHealth <= 0` 时复活 |
| `SaveData/LoadData` | :59-67 | `ToData()` / `LoadFrom()` | `LoadData` 在 `playerStatsData == null` 时直接 return（:64） |

`Respawn` 留在服务层是刻意的（:12 注释）：它是唯一带条件判断的写操作。全工程唯一的调用点是重试入口 `PlayerDamageController.OnRetryRequest`（`Controllers/PlayerDamageController.cs:56`）——`SceneChanger` 本轮已删掉 `retryEventSO` 字段与复活调用，不再碰这一域。

### 2.3 Model 的构造与状态修复

- `PlayerStatsModel()` 用全零 `new PlayerStatsData()`（:35）。
- `PlayerStatsModel(PlayerStatsData initial)`：`data = initial.Clone()` 后立刻 `RepairExpThreshold()`（:39-40）。**构造即拷贝**，不持有传入对象。
- `RepairExpThreshold()`（:89-93）：`expToUpgrade < MinExpToUpgrade` 时修回 1。构造（:40）与 `LoadFrom`（:77）两个入口都过这一关。

### 2.4 经验曲线、等级上限与钳制（公式与常量）

常量（`PlayerStatsModel.cs:28-30`）：`MinExpToUpgrade = 1`、`ExpStepLevels = 10`、`ExpGrowthDivisor = 4f`。

升级循环（:146-156）：条件是三个与——阈值不低于 1、`currentExp >= expToUpgrade`、`maxLevel <= 0 || level < maxLevel`（:148）。每轮 `currentExp -= expToUpgrade`、`level++`、`GrowExpToUpgrade()`（:150-153），循环直到条件不成立，所以**一次 `AddExp` 可以连升多级**。

曲线（`GrowExpToUpgrade`，:168-174）：

```
truncated = (expToUpgrade / 10) * 10            // 整数除法，等价于截断到 10 的整数倍
step      = (int)(truncated * expMultiplier / 4)
expToUpgrade = Mathf.Max(1, expToUpgrade + step)  // 下限仍受 MinExpToUpgrade 约束
```

两点后果都由测试锁住：阈值 < 10 时 `truncated == 0` → `step == 0` → 阈值恒定（`PlayerStatsModelTests.cs:248`）；`expMultiplier == 0` 时步长为 0，退化成"永远每阈值点升一级"，不会每次获得经验都升级（:278）。

钳制规则：

| 字段 | 规则 | 证据 |
|---|---|---|
| `maxHealth` | `+= amount` 后下限钳到 1 | :99-100 |
| `currentHealth` | `Clamp(current + amount, 0, maxHealth)` | :106 |
| `currentHealth`（直接赋值） | `Clamp(value, 0, maxHealth)` | :112 |
| `expToUpgrade` | 构造与读档修到 ≥ 1；增长时 `Max(1, …)` | :40、:77、:173 |
| `skillPoints` | **无任何钳制**，可被写成负数 | :129 |
| `maxLevel` | `0` 表示不限制 | :59-60、:28-29 |

`AddExp` 对非正数的处理（:137-141）：`amount == 0` 静默忽略；`amount < 0` 记一条 `Debug.LogWarning("忽略非正数经验")` 后忽略。两者都不改状态、不发事件。

### 2.5 事件广播边界

| 事件 | 触发点 | 不触发的情形 |
|---|---|---|
| `HealthChanged` | `UpdateMaxHealth`:101、`UpdateHealth`:107、`SetCurrentHealth`:113、`LoadFrom`:79 | — |
| `StatsChanged` | `UpdateSpeed`:119、`UpdateDamage`:125、`LoadFrom`:80 | **改 `maxHealth` 不发它**（面板只显示 damage/speed） |
| `ExpChanged` | `AddExp`:158、`LoadFrom`:81 | 非正数经验被忽略时不发（:137-141 提前 return） |
| `LevelUp(int)` | 仅 `AddExp`，且 `levelsGained > 0` 时（:160） | `LoadFrom` **不发**——读档的技能点直接取存档字段，不补发 |

同一方法内的广播顺序是契约：`AddExp` 先 `ExpChanged` 后 `LevelUp`（:158、:160）；`LoadFrom` 依次 `HealthChanged → StatsChanged → ExpChanged`（:79-81）。`UpdateSkillPoints` 完全不发事件（:128-129）。`LevelUp` 的订阅方 `SkillTreeManager.UpdateAbilityPoints` 会回调 `StatsService.UpdateSkillPoints`（`SkillTreeManager.cs:79`），形成一次嵌套调用，但落在不同字段且不发事件，不会递归。

### 2.6 PlayerStatsSO 作为初始值模板（拷贝语义）

- `CreateInitialData()` 返回 `stats.Clone()`（`PlayerStatsSO.cs:17`），`StatsService` 用的就是它（`StatsService.cs:29`）。
- **`Data` 属性返回的是模板本体，不是拷贝**（`PlayerStatsSO.cs:14`）。当前运行时代码没有任何地方读 `Data`；唯一使用者是测试（`PlayerStatsSOTests.cs:30-32`），测试故意用 `Data` 配置初始值。
- 资产现值（`Assets/GameSO/PlayerStatsSO.asset:15-30`）：damage 2 / weaponRange 1 / knockBackForce 0.5 / knockBackTime 0.2 / stunTime 0.2 / coolDown 0.3 / speed 5 / maxHealth 2 / currentHealth 2 / skillPoints 1 / level 0 / currentExp 0 / expToUpgrade 10 / expMultiplier 1.5 / maxLevel 0。

### 2.7 存档边界

- `SaveData.playerStatsData` 是 DTO 槽位（`Assets/Scripts/Gameplay/Save/SaveData.cs:11`）。
- 存：`SaveDataManager.PrepareManualSaveData` 遍历 `SaveRegistry.All` 调 `SaveData`（:64-67）；`OnAutoSave` 同样遍历（:81-84），并在重建 `dataToSave` 后补写一次数值（:95）。
- 读：`SaveDataManager.LoadFromData` 遍历所有注册者调 `LoadData`（:145-148）。
- `OnAutoLoad`（场景加载完成）**跳过** `GetDataID() == null` 的服务（:129-130，本轮新增的守卫 `if (saveable.GetDataID() == null) continue;`，理由见 `:124-128` 注释），`StatsService` 正属此类（`SaveableService.cs:25` 返回 null）——所以数值不会被场景加载回灌，避免把广播段抓的旧快照覆盖回运行时；重试复活补的血也因此不会被读档撤销。
- `SaveSystem.IsLoadableSaveFile` 要求 `save.data.playerStatsData != null`（`SaveSystem.cs:189`），坏档回退。
- 序列化用 Newtonsoft `JsonConvert`（`SaveSystem.cs:54`/`:122`/`:187`），所以 `PlayerStatsData` 的**字段名就是 JSON 键**（`PlayerStatsData.cs:6-7` 注释明写）。

### 2.8 26 个 EditMode 规格的实际断言范围

`Assets/Tests/Editor/` 下无 asmdef（`Assets/**/*.asmdef` 无结果），测试落 `Assembly-CSharp-Editor`。

- `PlayerStatsModelTests.cs`：24 个 `[Test]`，覆盖四组——钳制与事件隔离（:63、:73、:122、:129、:182）、`ExpChanged` 契约（:83、:94、:106）、只读接口与 Service 边界（:154、:171，后者用反射断言 `Stats` 静态类型是 `IPlayerStatsReadOnly` 且 `Model` 属性已不存在）、曲线与上限（:206、:220、:248、:263、:278、:292、:304、:315）、拷贝语义与阈值持久化（:327、:338、:350、:366）；另有 `LoadFrom_ReplacesStateAndFiresBothEvents`（:189）。
- `PlayerStatsSOTests.cs`：2 个 `[Test]`（:27、:41），只断言 `CreateInitialData()` 反映配置值且返回拷贝。
- 合计 26。**没有**任何测试覆盖：真实场景里的 `StatsService`（无 PlayMode 用例）、`Respawn()`、`UseItem`/`SkillTreeManager` 等跨域调用方、`skillPoints` 下限、`UpdateMaxHealth` 降上限时 `currentHealth` 的收拢。

### 2.9 所有写这一域的调用方

写命令必须经 `StatsService`，不存在第二条路径。

| 调用方 | 写命令 | 证据 |
|---|---|---|
| `PlayerDamageController.OnDamaged` | `UpdateHealth(-damage)` | `Controllers/PlayerDamageController.cs:33` |
| `PlayerDamageController.OnRetryRequest` | `Respawn()`，随后 `SceneChanger.Instance.RequestSceneLoad(GetCurrentScenes(), Vector3.zero, true)` | 同上 :56-57 |
| `RetryButton.HandleRetry`（触发源） | 只广播 `retryEventSO`，不直接写数值 | `Assets/Scripts/Pipeline/UI/Buttons/RetryButton.cs:8-16`；资产 `Assets/GameSO/Events/VoidEvents/RetryRequestEvent.asset`（订阅见 `PlayerDamageController.cs:16,22`，接线 `PersistentScene.unity:35549`、`Assets/Prefabs/UI/Buttons/RetryButton.prefab:150`） |
| `ExperienceController.GainExp` | `AddExperience(exp)` | `Controllers/ExperienceController.cs:45`（源通道 `EnemyDefeatedEventSO.cs:12`，由 `EnemyHealth.cs:32` 广播） |
| `InventoryManager.UpdateInventorySlots`（`isEXP` 分支） | `AddExperience(quantity)` | `Assets/Scripts/Gameplay/Inventory/InventoryManager.cs:110` |
| `UseItem.ApplyItemEffects` | `UpdateMaxHealth/UpdateHealth/UpdateSpeed/UpdateDamage` | `Inventory/UseItem.cs:12,14,16,18` |
| `UseItem.EffectTimer`（到期回滚） | 反向四个命令 | `Inventory/UseItem.cs:29,33,36,39` |
| `SkillTreeManager.UpdateAbilityPoints` | `UpdateSkillPoints(amount)` | `Assets/Scripts/Gameplay/Skills/SkillTreeManager.cs:79` |
| `SkillManager`（"MaxHealthBoost" 分支） | `UpdateMaxHealth(1)` + `UpdateHealth(1)` | `Skills/SkillManager.cs:24-25` |

只读消费方（读侧不走 Service 方法，直接读 `Stats`）：`PlayerCombat.cs:25`（WeaponRange）、:32（Damage）；`PlayerBow.cs:122`（CoolDown）；`PlayerMovement.cs:212`（CoolDown）、:270（Speed）；`Arrow.cs:38`（Damage）；`EnemyHealth.cs:47-49`（KnockBackForce / StunTime / KnockBackTime）；`HealthController.cs:31`；`ExperienceController.cs:40`；`StatsPanelController.cs:31`；`SkillTreeManager.cs:50,71`（SkillPoints）。

## 3. 约定与硬边界

1. **升序迭代是收敛的，但阈值为 1 时按级数消耗**。`GrowExpToUpgrade` 保证阈值 ≥ 1（:173），循环条件里有它（:146），所以永不死循环；但阈值为 1 时每级只扣 1 点经验，`AddExp(n)` 的迭代次数是 `n`。绕过 Service 直接构造退化数据再灌巨量经验会把主线程卡住。
2. **重试复活必须"先回血、后重载"**。本轮完整链路：`RetryButton.HandleRetry()` 广播 `retryEventSO`（`Assets/Scripts/Pipeline/UI/Buttons/RetryButton.cs:8-16`，资产 `Assets/GameSO/Events/VoidEvents/RetryRequestEvent.asset`）→ `PlayerDamageController.OnRetryRequest`（字段与订阅 `PlayerDamageController.cs:16,22`，实现 `:54-58`）→ 先 `StatsService.Instance.Respawn()`（`:56`）→ 再 `SceneChanger.Instance.RequestSceneLoad(SceneChanger.Instance.GetCurrentScenes(), Vector3.zero, true)`（`:57`）。顺序的硬理由写在 `:48-53` 注释里：`SaveDataManager` 在广播段同步抓存档快照（`SaveDataManager.cs:81-84`），回血晚于快照就会把死亡态血量写进存档；而 `OnAutoLoad` 跳过固定槽位服务（`SaveDataManager.cs:129-130`）保证读档不把它覆盖回去。反了会坏档。注意重试编排本轮已从 `SceneChanger` 迁到 `PlayerDamageController`：`SceneChanger.cs` 已无 `retryEventSO` 字段、无 OnEnable/OnDisable 订阅，也不再调用 `Respawn()`（全工程唯一调用点即 `PlayerDamageController.cs:56`）；`SceneChanger` 的重试相关公开面只剩 `GetCurrentScene()`、`GetCurrentGameScene()`、`GetCurrentScenes()`、`RequestSceneLoad(...)`。
3. **不要写 `PlayerStatsSO.Data`**。它返回模板本体（`PlayerStatsSO.cs:14`），运行期写入会永久污染初始值，且不会报错——下一次新建模型就带着被污染的值。取初始值只能走 `CreateInitialData()`。
4. **`PlayerStatsData` 的字段名不可改**。Newtonsoft 按字段名序列化（`SaveSystem.cs:54`），改名等于让所有旧档读不出数值；类名可以改（`PlayerStatsData.cs:6-7`）。
5. **`Stats` 在 `StatsService.Awake` 完成前是 null**。`model` 在 `OnSingletonInitialized` 里创建（`StatsService.cs:29`），而该钩子由 `YSingleton.Awake` 调用（`Assets/Scripts/Contracts/YSingleton.cs:24`）。在 `Awake` 早于它的组件里访问 `StatsService.Instance.Stats` 会 NRE。现有三个 View 都在 `Start` 建 Controller 来规避（`HealthView.cs:21`、`ExperiencePanelView.cs:23`、`StatsPanelView.cs:34`）；`SkillTreeManager.cs:31-37` 额外做了可重试订阅。
6. **`IPlayerStatsReadOnly` 是唯一的对外读类型**。`StatsService` 不再暴露具体 `Model`（`PlayerStatsModelTests.cs:179` 用反射锁死这一点）。要加写操作就加 Service 方法，不要把 Model 暴露出去。
7. **`UpdateSkillPoints` 不发事件**（:128-129）。省略的前提是唯一消费者就是写入者：`SkillTreeManager` 写完自己刷文本（`SkillTreeManager.cs:80`）。新增消费者必须自己刷，不能等事件。
8. **`LoadData` 的 null 语义是"保留现状"**（`StatsService.cs:64`）。空数值段不是"清零"。

## 4. 已知缺陷与风险

- **D1** `PlayerStatsSO.Data`（`PlayerStatsSO.cs:14`）是可变逃生舱：返回模板本体，运行时任何写入都静默污染初始值，只有 `CreateInitialData()` 有拷贝语义。
- **D2** 技能点无下限钳制（`PlayerStatsModel.cs:129`）。守卫写在调用方（`SkillTreeManager.cs:50,71` 的 `> 0`），任何新调用方漏写守卫即可把点数写成负数。
- **D3** `UpdateMaxHealth` 降低上限时不收拢 `currentHealth`（:97-102）。`UseItem.EffectTimer` 用 `CurrentHealth - MaxHealth` 手算补偿（`UseItem.cs:31-33`），且该补偿方向在代码上写作 `UpdateHealth(healthDiff)`（正数=加血），最终靠 `Clamp` 兜住。换个调用方就得自己再写一遍。
- **D4** `AddExp` 的迭代次数上界是 `经验量 / expToUpgrade`；阈值为 1 时按 1 点/级迭代（:146-156），是潜在的主线程卡顿点。
- **D5** `Docs/UML/02_Player_System.puml:40-54` 仍描述 `StatsManager`（类名已不存在）、`- stats : PlayerStatsData`（与"Model 持有数据"冲突）、公开 `GetStats()/LoadStats()`（实为 private）、拼写 `Respwan`（:48）/`expMutiplier`（:41），且缺 `AddExp` 与 `maxLevel`。
- **D6** 旧清单 `Docs/My_ARPG_重构优化清单_已解决.md` 已由用户在 2026-10-05 删除（内容见 `git HEAD:Docs/My_ARPG_重构优化清单_已解决.md`），其 `:112` 记 `PlayerStatsSOTests 7 个`（现为 2 个 + ModelTests 24 个）；同文档 `:96` 记路径 `Gameplay/SO/PlayerStatsSO.cs`，实际在 `Assets/Scripts/Pipeline/SO/`。
- **D7** 旧清单 `Docs/My_ARPG_重构优化清单_未解决.md` 已由用户在 2026-10-05 删除（内容见 `git HEAD:Docs/My_ARPG_重构优化清单_未解决.md`），其 `:50` 引用 `Scripts/UI/HUD/HealthHeartUI.cs`，该文件在本工程中不存在。
- **D8** 服务与模型的加经验命名不一致（`AddExperience` / `AddExp`），检索时容易漏掉一侧。
- **D9** `Assets/GameSO/GameSceneSO/OtherScenes/RetrySceneSO.asset` 已是孤儿资产：本轮重试编排迁到 `PlayerDamageController` 后，该场景 SO 在全部 `Assets/**` 的 `.unity`/`.prefab`/`.asset` 里零引用（除自身 `.meta`），仅作为历史残留存在；重试现在只重载"当前场景组"（`RequestSceneLoad(GetCurrentScenes(), Vector3.zero, true)`），不再跳到某个固定场景。

## 5. 未核验事项

- 假设：场景或预制体里 `StatsService` 组件的 `statsConfig` 已指向 `Assets/GameSO/PlayerStatsSO.asset`（未运行 Unity 验证；代码层 `statsConfig` 为空会在 `StatsService.cs:29` 抛 NullReferenceException）。
- 假设：`StatsService` 所在 GameObject 在场景启动时处于激活态，且早于其它组件 Awake（未运行 Unity 验证）。
- 假设：重试链路确实按 `PlayerDamageController.cs:48-58` 的实现顺序执行（先 `Respawn` 再抓快照）——静态读码与接线（`PersistentScene.unity:35549` 的 `retryEventSO`、`RetryButton.prefab:150`）均已核对，但未在 Unity 里实点重试按钮跑通端到端时序。
- 假设：26 个 EditMode 用例当前全绿（未运行 Unity Test Runner 验证）。
- 假设：`PlayerStatsData` 的实际 JSON 键与字段名完全一致（代码只证明用了 `JsonConvert`；未打开存档文件核对）。
- 假设：`Assets/Tests/**` 因无 asmdef 而落 `Assembly-CSharp-Editor`（未运行 Unity 验证，仅由 `Assets/**/*.asmdef` 无结果推断）。
- 假设：`HealthHeartUI` 在场景与预制体中均无残留引用（该文件名只出现在已删除的旧清单里，见 D7；此处仅按文件名 glob，未查场景/预制体内部组件）。
- 假设：触发词表中"击退/冷却"的消费方（`EnemyKnockBack`、`PlayerBow`）行为与本域数值语义一致，未逐一核验其内部实现。
