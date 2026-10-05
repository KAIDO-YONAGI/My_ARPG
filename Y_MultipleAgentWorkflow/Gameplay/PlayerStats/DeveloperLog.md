# Gameplay.PlayerStats Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Gameplay.PlayerStats 权威文档

**写入的文件**

- `Y_MultipleAgentWorkflow/Gameplay/PlayerStats/PlayerStats_Guide.md`（新建，ID `GP-PLAYERSTATS-GUIDE`，状态 Active）
- `Y_MultipleAgentWorkflow/Gameplay/PlayerStats/Router.md`（按项目中文模板重写，Document ID 改为 `BUS-GAMEPLAY-PLAYERSTATS`，下级导航表原样保留 `None` 行）
- `Y_MultipleAgentWorkflow/Gameplay/PlayerStats/DeveloperLog.md`（本条目，未删除既有内容）

**依据的证据路径**

- `Assets/Scripts/Gameplay/Player/Models/PlayerStatsModel.cs`、`PlayerStatsData.cs`、`IPlayerStatsReadOnly.cs`
- `Assets/Scripts/Gameplay/Player/Services/StatsService.cs`
- `Assets/Scripts/Gameplay/Player/Controllers/HealthController.cs`、`ExperienceController.cs`、`StatsPanelController.cs`、`PlayerDamageController.cs`
- `Assets/Scripts/Gameplay/Player/Views/HealthView.cs`、`ExperiencePanelView.cs`、`StatsPanelView.cs`
- `Assets/Scripts/Pipeline/SO/PlayerStatsSO.cs`、`Assets/GameSO/PlayerStatsSO.asset`
- `Assets/Tests/Editor/PlayerStatsModelTests.cs`、`Assets/Tests/Editor/PlayerStatsSOTests.cs`
- 调用方：`Inventory/InventoryManager.cs`、`Inventory/UseItem.cs`、`Skills/SkillTreeManager.cs`、`Skills/SkillManager.cs`、`Player/PlayerCombat.cs`、`Player/PlayerBow.cs`、`Player/PlayerMovement.cs`、`Player/Arrow.cs`、`Units/Enemy/EnemyHealth.cs`
- 存档链：`Save/SaveData.cs`、`Save/SaveDataManager.cs`、`Save/SaveSystem.cs`、`Contracts/SaveableService.cs`、`Contracts/SaveRegistry.cs`、`Contracts/YSingleton.cs`
- 旧文档：`Docs/My_ARPG_MVCS项目现状.md`、`Docs/My_ARPG_重构优化清单_已解决.md`、`Docs/My_ARPG_重构优化清单_未解决.md`、`Docs/UML/02_Player_System.puml`

**已核验**

- 四层职责与单向依赖方向：Model 的 using 只有 System/UnityEngine（`PlayerStatsModel.cs:1-2`），Service 私有持有 model（`StatsService.cs:21,24`）。
- 唯一写入口：`StatsService.cs:40-50` 六个转发方法 + `Respawn()`（:32-36）；`GetStats/LoadStats` 为 private（:53,56）。
- 经验曲线公式与常量：`MinExpToUpgrade=1`、`ExpStepLevels=10`、`ExpGrowthDivisor=4f`（:28-30），`GrowExpToUpgrade`（:168-174），升级循环含 maxLevel 条件（:148）。
- 事件广播边界：4 个事件的触发点与非触发点逐条定位；`UpdateSkillPoints` 不发事件（:128-129）；`AddExp` 内 `ExpChanged` 先于 `LevelUp`（:158,160）。
- 拷贝语义：`PlayerStatsData.Clone()`（:35）用于构造（:39）、`LoadFrom`（:76）、`ToData`（:65）、`CreateInitialData`（`PlayerStatsSO.cs:17`）；`PlayerStatsSO.Data` 返回本体（:14）。
- 26 个 EditMode 规格：`Select-String '[Test]'` 实测 ModelTests 24 + SOTests 2 = 26。
- `Assets/**/*.asmdef` 无结果，测试落 `Assembly-CSharp-Editor`。
- 存档链：`SaveData.playerStatsData`（`SaveData.cs:11`）、`SaveableService.GetDataID() => null`（`SaveableService.cs:25`）、`OnAutoLoad` 跳过 `GetDataID()==null` 的服务（`SaveDataManager.cs:129-130`）。

**未核验**

- 未运行 Unity：场景/Prefab 中 `statsConfig` 的接线、`StatsService` 的激活态与 Awake 顺序、26 个用例的实际绿灯结果、`Assets/Tests` 的最终装配体。
- 未打开存档 JSON 核对 `PlayerStatsData` 的键名与字段名是否逐字一致。
- 未核实 `HealthHeartUI` 是否在场景/预制体中留有组件引用（仅确认 `Assets/**/HealthHeartUI.cs` 不存在）。

**发现的缺陷**（详见 Guide §4）

- D1 `PlayerStatsSO.Data` 返回模板本体，运行期写入会静默污染初始值。
- D2 `PlayerStatsModel.UpdateSkillPoints`（:129）无下限钳制，守卫只在调用方。
- D3 `UpdateMaxHealth`（:97-102）降低上限时不收拢 `currentHealth`，靠 `UseItem.cs:31-33` 补偿。
- D4 阈值为 1 时 `AddExp` 按 1 点/级迭代（:146-156），大额经验存在主线程卡顿风险。
- D5 `Docs/UML/02_Player_System.puml:44-54` 与代码冲突（StatsManager、公开 GetStats/LoadStats、字段拼写）。
- D6 旧文档计数与路径过期（`已解决.md:112` 记 SOTests 7 个；`:96` 记错目录）。
- D7 `未解决.md:50` 引用不存在的 `HealthHeartUI.cs`。
- D8 加经验命名不一致：服务侧 `AddExperience` / 模型侧 `AddExp`。

**维护计数**

- `BUS-GAMEPLAY-PLAYERSTATS` 维持 `0/5`（首次建档，未发生 5 次触发的维护轮次）。