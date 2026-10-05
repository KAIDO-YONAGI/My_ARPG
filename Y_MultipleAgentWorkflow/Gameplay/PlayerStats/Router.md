# Gameplay.PlayerStats Router

文档 ID：`BUS-GAMEPLAY-PLAYERSTATS`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `血量` `HP` `扣血` `回血` `Respawn` | `PlayerStats_Guide.md` |
| `经验` `升级` `LevelUp` `经验曲线` `expToUpgrade` `maxLevel` | `PlayerStats_Guide.md` |
| `技能点` `SkillPoints` `加点` | `PlayerStats_Guide.md` |
| `攻击力` `速度` `武器范围` `击退` `冷却` | `PlayerStats_Guide.md` |
| `StatsService` `Stats` `IPlayerStatsReadOnly` `只读视图` | `PlayerStats_Guide.md` |
| `PlayerStatsModel` `PlayerStatsData` `PlayerStatsSO` `拷贝语义` | `PlayerStats_Guide.md` |
| `playerStatsData` `读档` `坏档` `存档数值` | `PlayerStats_Guide.md` |
| `PlayerDamaged` `EnemyDefeated` `AddExperience` | `PlayerStats_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| None | None |

## 并发资源

- `workflow:Gameplay.PlayerStats`
- `path:Assets/Scripts/Gameplay/Player/Models/`
- `path:Assets/Scripts/Gameplay/Player/Services/StatsService.cs`
- `path:Assets/Scripts/Gameplay/Player/Controllers/`
- `path:Assets/Scripts/Gameplay/Player/Views/`
- `path:Assets/Scripts/Pipeline/SO/PlayerStatsSO.cs`
- `path:Assets/GameSO/PlayerStatsSO.asset`
- `path:Assets/Tests/Editor/PlayerStatsModelTests.cs`
- `path:Assets/Tests/Editor/PlayerStatsSOTests.cs`
- `path:Y_MultipleAgentWorkflow/Gameplay/PlayerStats/`

## 能力边界

### Active

- `GP-PLAYERSTATS-GUIDE`（`PlayerStats_Guide.md`）：本域当前实现的事实基线，覆盖 `PlayerStatsModel` / `PlayerStatsData` / `IPlayerStatsReadOnly` / `StatsService` / `PlayerStatsSO`、三个显示 Controller 与四个跨域写入调用点。
- 本域是全项目玩家数值写操作的唯一汇聚点：对外写入口只有 `StatsService` 的 6 个转发方法加 `Respawn()`。
- 只读视图 `IPlayerStatsReadOnly` 已锁死（`PlayerStatsModelTests.cs:171` 用反射断言 `StatsService.Model` 属性不存在）。
- 26 个 EditMode 规格构成本域回归网：`PlayerStatsModelTests.cs` 24 个 + `PlayerStatsSOTests.cs` 2 个。

### Proposal

- 修 D2（技能点下限钳制下沉到 `PlayerStatsModel.UpdateSkillPoints`）尚未实施。
- 修 D3（`UpdateMaxHealth` 降上限时同步收拢 `currentHealth`）尚未实施，现由 `UseItem.cs:31-33` 的调用方补偿兜住。

### 需要用户确认

- 技能等级与解锁不入档（每局重置），技能点数随 `PlayerStatsData` 入档——这是现状还是待补，需用户确认后再决定要不要动 `SaveData` 顶层结构。
- `PlayerStatsSO.Data`（返回模板本体）是否收敛为只读或返回拷贝，属接口变更，需用户确认。
