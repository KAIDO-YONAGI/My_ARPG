# Gameplay.PlayerStats Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Gameplay.PlayerStats 权威文档

**写入的文件**

- `PlayerStats_Guide.md` 新建，文档 ID `GP-PLAYERSTATS-GUIDE`，状态 Active。
- `Router.md` 按项目中文模板重写，文档 ID 改为 `BUS-GAMEPLAY-PLAYERSTATS`，下级导航表保留 `None` 行。
- `DeveloperLog.md` 本条目，未删除既有内容。

**依据**

- 模型与契约：`PlayerStatsModel`、`PlayerStatsData`、`IPlayerStatsReadOnly`
- 服务：`StatsService`
- 控制器：`HealthController`、`ExperienceController`、`StatsPanelController`、`PlayerDamageController`
- 视图：`HealthView`、`ExperiencePanelView`、`StatsPanelView`
- 配置：`PlayerStatsSO` 类型与其配置资产
- 测试：`PlayerStatsModelTests`、`PlayerStatsSOTests`
- 调用方：`InventoryManager`、`UseItem`、`SkillTreeManager`、`SkillManager`、`PlayerCombat`、`PlayerBow`、`PlayerMovement`、`Arrow`、`EnemyHealth`
- 存档链：`SaveData`、`SaveDataManager`、`SaveSystem`、`SaveableService`、`SaveRegistry`、`YSingleton`

**已核验**

- 四层职责与单向依赖方向：Model 的 using 只有 `System` 与 `UnityEngine`，Service 私有持有 model。
- 唯一写入口：`StatsService` 的六个转发方法与 `Respawn()`；`GetStats` 与 `LoadStats` 是 private。
- 经验曲线公式与常量：`MinExpToUpgrade = 1`、`ExpStepLevels = 10`、`ExpGrowthDivisor = 4f`；`GrowExpToUpgrade` 负责增长，升级循环含 maxLevel 条件。
- 事件广播边界：4 个事件的触发点与非触发点逐条定位；`UpdateSkillPoints` 不发事件；`AddExp` 内 `ExpChanged` 先于 `LevelUp`。
- 拷贝语义：`PlayerStatsData.Clone()` 用于构造、`LoadFrom`、`ToData` 与 `CreateInitialData`；`PlayerStatsSO.Data` 返回本体。
- 26 个 EditMode 规格：`Select-String '[Test]'` 实测 ModelTests 24 个加 SOTests 2 个，合计 26。
- 工程内没有 asmdef，测试落 `Assembly-CSharp-Editor`。
- 存档链：`SaveData.playerStatsData` 字段、`SaveableService.GetDataID()` 返回 null、`OnAutoLoad` 跳过 `GetDataID()` 为 null 的服务。

**维护计数**

- `BUS-GAMEPLAY-PLAYERSTATS` 维持 `0/5`；首次建档，未发生 5 次触发的维护轮次。
