# 玩家数值与经验线（PlayerStats）

文档 ID：`GP-PLAYERSTATS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本域只负责 `PlayerStatsModel` / `PlayerStatsData` / `IPlayerStatsReadOnly` / `StatsService` / `PlayerStatsSO` 这条数值线的当前实现，以及挂在它上面的三个显示 Controller、四个跨域写入调用点；技能树规则本身、背包装备的物品效果设计、存档文件格式与 UI 布局归对应业务根。
上游来源：以下当前代码与资产是本域结论的依据，按类型名与资产名定位。

- 数值线：`PlayerStatsModel`、`PlayerStatsData`、`IPlayerStatsReadOnly`、`StatsService`、`PlayerStatsSO` 与玩家数值配置资产
- 显示侧：`HealthController`、`ExperienceController`、`StatsPanelController` 及 `HealthView`、`ExperiencePanelView`、`StatsPanelView`
- 跨域写入与存档：`PlayerDamageController`、`SceneChanger`、`SaveDataManager`、`SaveData`、`SaveSystem`、`UseItem`、`SkillTreeManager`、`SkillManager`、`InventoryManager`
- 事件与入口：重试请求事件资产、`RetryButton`
- 回归网：`PlayerStatsModelTests`、`PlayerStatsSOTests`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `血量` `HP` `扣血` `回血` `Respawn` | §2.2、§2.9、§3.2 |
| `重试` `RetryRequestEvent` `RetryButton` `GameOver` | §2.9、§3.2 |
| `经验` `升级` `LevelUp` `阈值` `expToUpgrade` `经验曲线` | §2.4、§3.1 |
| `技能点` `SkillPoints` `加点` | §2.4、§3.1，无下限钳制 |
| `攻击力` `速度` `武器范围` `击退` `冷却` | §2.2、§2.5 |
| `StatsService` `Stats` `IPlayerStatsReadOnly` `只读视图` | §2.1、§2.2、§3.5 |
| `PlayerStatsSO` `初始值` `模板` `资产污染` | §2.6、§3.3 |
| `playerStatsData` `读档` `存档数值` `坏档` | §2.7、§3.4 |
| `26 个测试` `EditMode` `回归网` | §2.8 |

## 2. 当前实现

### 2.1 四层职责与依赖方向

依赖是单向的：`View → Controller → StatsService → PlayerStatsModel`，Model 不反向引用任何上层。

| 层 | 类型形态 | 关键类型 | 关键事实 |
|---|---|---|---|
| Model | 纯 C# 类 | `PlayerStatsModel` | 只引用 `System` 与 `UnityEngine`，不引用 Services |
| 契约 | interface | `IPlayerStatsReadOnly` | 4 个事件 + 15 个只读属性 |
| DTO | `[Serializable]` 普通类 | `PlayerStatsData` | 15 个 public 字段 + `Clone()` |
| Service | `SaveableService<StatsService>` | `StatsService` | 私有持有 model，唯一写入口 |
| Controller | 3 个纯 C# + 1 个 MonoBehaviour | `HealthController`、`ExperienceController`、`StatsPanelController`、`PlayerDamageController` | 只读经 `IPlayerStatsReadOnly` |
| View | MonoBehaviour | `HealthView`、`ExperiencePanelView`、`StatsPanelView` | 只碰控件，在 `Start` 里造 Controller 并首刷 |
| 配置 | `ScriptableObject` | `PlayerStatsSO` | 落在全局命名空间，不在 `Gameplay.Player.*` |

`StatsService` 的 `model` 字段是 private，对外只给 `public IPlayerStatsReadOnly Stats => model;`。`GetStats()` 与 `LoadStats()` 是 private，仅存档流程使用。

### 2.2 唯一写入口：StatsService 的转发边界

Service 只做边界转发，不复制数值规则；钳制、结算、事件都在 Model 内。

| 服务方法 | 转发目标 | 附加规则 |
|---|---|---|
| `UpdateMaxHealth(int)` | `model.UpdateMaxHealth` | 无 |
| `UpdateHealth(int)` | `model.UpdateHealth` | 无 |
| `UpdateSpeed(float)` | `model.UpdateSpeed` | 无 |
| `UpdateDamage(int)` | `model.UpdateDamage` | 无 |
| `UpdateSkillPoints(int)` | `model.UpdateSkillPoints` | 无 |
| `AddExperience(int)` | `model.AddExp` | 模型侧方法名是 `AddExp` |
| `Respawn()` | `model.SetCurrentHealth(MaxHealth)` | 仅 `CurrentHealth <= 0` 时复活 |
| `SaveData` / `LoadData` | `ToData()` / `LoadFrom()` | `LoadData` 在 `playerStatsData == null` 时直接 return |

`Respawn` 留在服务层是刻意的：它是唯一带条件判断的写操作。全工程唯一的调用点是重试入口 `PlayerDamageController.OnRetryRequest`；`SceneChanger` 的重试相关公开面是 `GetCurrentScene()`、`GetCurrentGameScene()`、`GetCurrentScenes()` 与 `RequestSceneLoad(...)`，复活调用只出现在 `PlayerDamageController`。

### 2.3 Model 的构造与状态修复

- `PlayerStatsModel()` 用全零 `new PlayerStatsData()`。
- `PlayerStatsModel(PlayerStatsData initial)`：先 `data = initial.Clone()`，再 `RepairExpThreshold()`。构造即拷贝，不持有传入对象。
- `RepairExpThreshold()`：`expToUpgrade < MinExpToUpgrade` 时修回 1；构造与 `LoadFrom` 两个入口都过这一关。

### 2.4 经验曲线、等级上限与钳制（公式与常量）

常量是 `MinExpToUpgrade = 1`、`ExpStepLevels = 10`、`ExpGrowthDivisor = 4f`。

升级循环的条件是三项与：阈值不低于 1、`currentExp >= expToUpgrade`、`maxLevel <= 0 || level < maxLevel`。每轮执行 `currentExp -= expToUpgrade`、`level++`、`GrowExpToUpgrade()`，循环到条件不成立，所以一次 `AddExp` 可以连升多级。

曲线由 `GrowExpToUpgrade` 计算：

```
truncated = (expToUpgrade / 10) * 10              // 整数除法，等价于截断到 10 的整数倍
step      = (int)(truncated * expMultiplier / 4)
expToUpgrade = Mathf.Max(1, expToUpgrade + step)  // 下限仍受 MinExpToUpgrade 约束
```

两点后果都由测试锁住：阈值小于 10 时 `truncated` 与 `step` 都是 0，阈值恒定；`expMultiplier == 0` 时步长为 0，退化成每次达到阈值才升一级，不会每次获得经验都升级。

钳制规则：

| 字段 | 规则 |
|---|---|
| `maxHealth` | `+= amount` 后下限钳到 1 |
| `currentHealth` | `Clamp(current + amount, 0, maxHealth)` |
| `currentHealth` 直接赋值 | `Clamp(value, 0, maxHealth)` |
| `expToUpgrade` | 构造与读档修到 ≥ 1；增长时取 `Max(1, …)` |
| `skillPoints` | 无任何钳制，可被写成负数 |
| `maxLevel` | `0` 表示不限制 |

`AddExp` 对非正数的处理：`amount == 0` 静默忽略；`amount < 0` 记一条 `Debug.LogWarning("忽略非正数经验")` 后忽略。两者都不改状态、不发事件。

### 2.5 事件广播边界

| 事件 | 触发点 | 不触发的情形 |
|---|---|---|
| `HealthChanged` | `UpdateMaxHealth`、`UpdateHealth`、`SetCurrentHealth`、`LoadFrom` | 无 |
| `StatsChanged` | `UpdateSpeed`、`UpdateDamage`、`LoadFrom` | 改 `maxHealth` 时不发，面板只显示 damage 与 speed |
| `ExpChanged` | `AddExp`、`LoadFrom` | 非正数经验被忽略时不发 |
| `LevelUp(int)` | 仅 `AddExp`，且 `levelsGained > 0` 时 | `LoadFrom` 不发，读档的技能点直接取存档字段 |

同一方法内的广播顺序是契约：`AddExp` 先 `ExpChanged` 后 `LevelUp`；`LoadFrom` 依次 `HealthChanged → StatsChanged → ExpChanged`。`UpdateSkillPoints` 完全不发事件。`LevelUp` 的订阅方 `SkillTreeManager.UpdateAbilityPoints` 会回调 `StatsService.UpdateSkillPoints`，形成一次嵌套调用；该调用落在不同字段且不发事件，不会递归。

### 2.6 PlayerStatsSO 作为初始值模板（拷贝语义）

- `CreateInitialData()` 返回 `stats.Clone()`，`StatsService` 用的就是它。
- `Data` 属性返回的是模板本体。运行时代码不读 `Data`，唯一使用者是 `PlayerStatsSOTests`，它用 `Data` 配置初始值。取初始值走 `CreateInitialData()`。
- 配置资产现值：damage 2、weaponRange 1、knockBackForce 0.5、knockBackTime 0.2、stunTime 0.2、coolDown 0.3、speed 5、maxHealth 2、currentHealth 2、skillPoints 1、level 0、currentExp 0、expToUpgrade 10、expMultiplier 1.5、maxLevel 0。

### 2.7 存档边界

- `SaveData.playerStatsData` 是 DTO 槽位。
- 存：`SaveDataManager.PrepareManualSaveData` 遍历 `SaveRegistry.All` 调 `SaveData`；`OnAutoSave` 同样遍历，并在重建 `dataToSave` 后补写一次数值。
- 读：`SaveDataManager.LoadFromData` 遍历所有注册者调 `LoadData`。
- `OnAutoLoad` 在场景加载完成时跳过 `GetDataID()` 返回 null 的服务。`StatsService` 正属此类，其 `SaveableService` 返回 null，所以数值不会被场景加载回灌：广播段抓到的快照不会覆盖运行时，重试复活补的血也不会被读档撤销。
- `SaveSystem.IsLoadableSaveFile` 要求 `save.data.playerStatsData != null`，坏档回退。
- 序列化用 Newtonsoft `JsonConvert`，因此 `PlayerStatsData` 的字段名就是 JSON 键。

### 2.8 26 个 EditMode 规格的实际断言范围

测试集没有 asmdef，全部落 `Assembly-CSharp-Editor`。

- `PlayerStatsModelTests` 有 24 个 `[Test]`。断言覆盖：钳制与事件隔离；`ExpChanged` 契约；只读接口与 Service 边界，其中一条用反射断言 `Stats` 的静态类型是 `IPlayerStatsReadOnly` 且 `Model` 属性不存在；曲线与上限；拷贝语义与阈值持久化。另有 `LoadFrom_ReplacesStateAndFiresBothEvents` 一条。
- `PlayerStatsSOTests` 有 2 个 `[Test]`，只断言 `CreateInitialData()` 反映配置值且返回拷贝。
- 合计 26 个。真实场景里的 `StatsService`、`Respawn()`、`UseItem` 与 `SkillTreeManager` 等跨域调用方、`skillPoints` 下限、`UpdateMaxHealth` 降上限时 `currentHealth` 的收拢都没有测试覆盖。

### 2.9 所有写这一域的调用方

写命令必须经 `StatsService`，这是唯一路径。

| 调用方 | 写命令 |
|---|---|
| `PlayerDamageController.OnDamaged` | `UpdateHealth(-damage)` |
| `PlayerDamageController.OnRetryRequest` | `Respawn()`，随后 `SceneChanger.Instance.RequestSceneLoad(GetCurrentScenes(), Vector3.zero, true)` |
| `RetryButton.HandleRetry` | 只广播重试请求事件，不直接写数值 |
| `ExperienceController.GainExp` | `AddExperience(exp)`；源通道 `EnemyDefeatedEventSO` 由 `EnemyHealth` 广播 |
| `InventoryManager.UpdateInventorySlots` 的 `isEXP` 分支 | `AddExperience(quantity)` |
| `UseItem.ApplyItemEffects` | `UpdateMaxHealth` / `UpdateHealth` / `UpdateSpeed` / `UpdateDamage` |
| `UseItem.EffectTimer` 到期回滚 | 上述四个命令的反向调用 |
| `SkillTreeManager.UpdateAbilityPoints` | `UpdateSkillPoints(amount)` |
| `SkillManager` 的 `MaxHealthBoost` 分支 | `UpdateMaxHealth(1)` 加 `UpdateHealth(1)` |

只读消费方直接读 `Stats`，不经过服务方法：`PlayerCombat` 读 WeaponRange 与 Damage；`PlayerBow` 读 CoolDown；`PlayerMovement` 读 CoolDown 与 Speed；`Arrow` 读 Damage；`EnemyHealth` 读 KnockBackForce / StunTime / KnockBackTime；`HealthController`、`ExperienceController`、`StatsPanelController`、`SkillTreeManager` 读各自需要的字段。

## 3. 约定与硬边界

1. **升序迭代是收敛的，阈值为 1 时按级数消耗**。`GrowExpToUpgrade` 保证阈值不低于 1，循环条件里含这一项，因此不会死循环；阈值为 1 时每级只扣 1 点经验，`AddExp(n)` 的迭代次数是 n 次。绕过 Service 直接构造退化数据再灌巨量经验会长时间占用主线程。
2. **重试复活必须「先回血、后重载」**。链路是 `RetryButton.HandleRetry()` 广播重试请求事件 → `PlayerDamageController.OnRetryRequest` → 先 `StatsService.Instance.Respawn()` → 再 `SceneChanger.Instance.RequestSceneLoad(SceneChanger.Instance.GetCurrentScenes(), Vector3.zero, true)`。顺序的理由是 `SaveDataManager` 在广播段同步抓存档快照，回血晚于快照就会把死亡态血量写进存档；`OnAutoLoad` 跳过固定槽位服务，保证读档不把它覆盖回去。顺序反了会坏档。
3. **不要写 `PlayerStatsSO.Data`**。它返回模板本体，运行期写入会永久污染初始值且不会报错，下一次新建模型就带着被污染的值。取初始值只能走 `CreateInitialData()`。
4. **`PlayerStatsData` 的字段名不可改**。Newtonsoft 按字段名序列化，改名会让所有旧档读不出数值；类名可以改。
5. **`Stats` 在 `StatsService.Awake` 完成前是 null**。`model` 在 `OnSingletonInitialized` 里创建，该钩子由 `YSingleton.Awake` 调用；在 `Awake` 早于它的组件里访问 `StatsService.Instance.Stats` 会 NRE。三个 View 都在 `Start` 建 Controller 来规避，`SkillTreeManager` 额外做了可重试订阅。
6. **`IPlayerStatsReadOnly` 是唯一的对外读类型**。`StatsService` 不暴露具体 `Model`，`PlayerStatsModelTests` 用反射锁死这一点。加写操作就加 Service 方法，把 Model 留在内部。
7. **`UpdateSkillPoints` 不发事件**。省略的前提是唯一消费者就是写入者：`SkillTreeManager` 写完自己刷文本。新增消费者必须自己刷，不能等事件。
8. **`LoadData` 的 null 语义是「保留现状」**。`playerStatsData` 为 null 时它直接返回，空数值段不会把运行时数值清零。

## 4. 已知缺陷与风险

- **D1** `PlayerStatsSO.Data` 是可变逃生舱：返回模板本体，运行时任何写入都静默污染初始值，只有 `CreateInitialData()` 有拷贝语义。
- **D2** 技能点无下限钳制。守卫写在调用方 `SkillTreeManager` 的 `> 0` 判断里，任何新调用方漏写守卫即可把点数写成负数。
- **D3** `UpdateMaxHealth` 降低上限时不收拢 `currentHealth`。`UseItem.EffectTimer` 用 `CurrentHealth - MaxHealth` 手算补偿，该补偿方向在代码上写作 `UpdateHealth(healthDiff)`，正数表示加血，最终靠 `Clamp` 兜住。换个调用方就得自己再写一遍。
- **D4** `AddExp` 的迭代次数上界是经验量除以 `expToUpgrade`；阈值为 1 时按 1 点每级迭代，是潜在的主线程卡顿点。
- **D5** 服务与模型的加经验命名不一致，服务侧是 `AddExperience`，模型侧是 `AddExp`，检索时容易漏掉一侧。
- **D6** `RetrySceneSO` 配置资产在工程内零引用。重试走 `RequestSceneLoad(GetCurrentScenes(), Vector3.zero, true)`，只重载当前场景组，该资产不参与运行。

## 5. 未核验事项

- 假设：场景或预制体里 `StatsService` 组件的 `statsConfig` 指向 `PlayerStatsSO` 配置资产。代码层 `statsConfig` 为空会在 `OnSingletonInitialized` 抛 NullReferenceException；该接线待编辑器实测。
- 假设：`StatsService` 所在 GameObject 在场景启动时处于激活态，且早于其它组件 Awake。运行期时序待编辑器实测。
- 假设：重试链路按 `PlayerDamageController` 的实现顺序执行，先 `Respawn` 再抓快照。静态读码与接线均已核对，端到端时序待编辑器实测。
- 假设：26 个 EditMode 用例当前全绿。待 Unity Test Runner 实测。
- 假设：`PlayerStatsData` 的实际 JSON 键与字段名完全一致。代码只证明用了 `JsonConvert`，存档文件内容待核对。
- 假设：测试集落 `Assembly-CSharp-Editor` 装配体。该结论由工作区内无 asmdef 推断，待编辑器实测。
- 假设：触发词表中「击退」「冷却」的消费方 `EnemyKnockBack` 与 `PlayerBow` 行为与本域数值语义一致。其内部实现待逐一核验。
