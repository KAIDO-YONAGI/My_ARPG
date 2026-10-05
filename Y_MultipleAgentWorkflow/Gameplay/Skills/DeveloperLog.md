# Gameplay.Skills Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 技能树 权威文档

- 任务：为 `Gameplay.Skills` 建立当前实现的事实性权威文档，并按项目中文模板重写 Router。
- 写入的文件：
  - `Skills_Guide.md`（新建，ID `GP-SKILLS-GUIDE`，状态 Active）
  - `Router.md`（按中文模板重写，ID `BUS-GAMEPLAY-SKILLS`）
  - `DeveloperLog.md`（本条目，追加）
- 依据的证据路径（全部实读）：
  - 代码：`SkillSlot`、`SkillTreeManager`、`SkillManager`、`SkillTreeCanvasManager`、`SkillSO`、`PlayerStatsModel`、`PlayerStatsData`、`StatsService`、`PlayerCombat`、`ShiftEquipment`、`YSingleton`、`SaveData`
  - 配置资产与接线：SkillButtonSO 目录下的 MaxHealthBoost、CombatUnlock 两份配置，以及 SkillButton 预制体与 PersistentScene 场景。
- 已核验项（静态读文件确认）：
  - 状态在槽位而非技能：`currentLevel`/`isUnlocked` 是 `SkillSlot` 字段；解锁条件是前置槽位「已解锁且已满级」。
  - 场景接线实数：`SkillTreeManager.skillSlots` 接了 **24 个** 槽位，其中 23 个指向 MaxHealthBoost 配置、1 个指向 CombatUnlock 配置，该配置的 `skillName` 为 `SwordSlash`；仅 3 个实例把 `isUnlocked` 改为 1。
  - 技能点来源：`PlayerStatsModel.AddExp` 的 `LevelUp(levelsGained)` → `SkillTreeManager.UpdateAbilityPoints` → `StatsService.UpdateSkillPoints`；`UpdateSkillPoints` 不广播事件，`pointsText` 只在 `UpdateAbilityPoints` 里刷新。
  - 可重试订阅：`TrySubscribeLevelUp` 用 `listeningToLevelUp` 幂等，`OnEnable` 与 `Start` 各调一次。
  - 效果作用域只有两处：`MaxHealthBoost` → `StatsService.UpdateMaxHealth(1)`+`UpdateHealth(1)`；`SwordSlash` → `PlayerCombat.SetActive(true)`。
  - 技能等级与解锁不入档，技能点随 `PlayerStatsData.skillPoints` 入档。
- 结构说明：原 Router 的「下级导航」子表只有占位行 `| None | None |`（无真实子类），重写时按项目中文模板 `..\..\Workflow\Templates\BusinessRouter.template.md` 改写为 `| 无 | 无 |`，未删除任何真实子类行。
- 维护计数：`0/5`（未变更，本次仅建立文档）。
