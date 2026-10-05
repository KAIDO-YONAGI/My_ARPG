# Gameplay.Skills Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 技能树 权威文档

- 任务：为 `Gameplay.Skills` 建立当前实现的事实性权威文档，并按项目中文模板重写 Router；核实「保留的验证点」说法的代码现状。
- 写入的文件：
  - `Y_MultipleAgentWorkflow\Gameplay\Skills\Skills_Guide.md`（新建，ID `GP-SKILLS-GUIDE`，状态 Active）
  - `Y_MultipleAgentWorkflow\Gameplay\Skills\Router.md`（按中文模板重写，ID `BUS-GAMEPLAY-SKILLS`）
  - `Y_MultipleAgentWorkflow\Gameplay\Skills\DeveloperLog.md`（本条目，追加）
- 依据的证据路径（全部实读）：
  - 代码：`Assets/Scripts/Gameplay/Skills/{SkillSlot,SkillTreeManager,SkillManager,SkillTreeCanvasManager}.cs`、`Assets/Scripts/Pipeline/SO/SkillSO.cs`、`Assets/Scripts/Gameplay/Player/Models/{PlayerStatsModel,PlayerStatsData}.cs`、`Assets/Scripts/Gameplay/Player/Services/StatsService.cs`、`Assets/Scripts/Gameplay/Player/{PlayerCombat,ShiftEquipment}.cs`、`Assets/Scripts/Contracts/YSingleton.cs`、`Assets/Scripts/Gameplay/Save/SaveData.cs`
  - 配置资产与接线：`Assets/GameSO/UI SO/SkillButtonSO/{MaxHealthBoost,CombatUnlock}.asset`、`Assets/Prefabs/UI/Buttons/SkillButton.prefab`、`Assets/Scenes/GameScene/PersistentScene.unity`
  - 旧文档线索：`Docs/My_ARPG_MVCS项目现状.md`（101、112、126-132、156、160、166、174-179 行）
- 已核验项（静态读文件确认）：
  - 状态在槽位而非技能：`currentLevel`/`isUnlocked` 是 `SkillSlot` 字段；解锁条件是前置槽位「已解锁且已满级」。
  - 场景接线实数：`SkillTreeManager.skillSlots` 接了 **24 个** 槽位（`PersistentScene.unity:36337-36361`），其中 23 个指向 `MaxHealthBoost.asset`、1 个指向 `CombatUnlock.asset`（其 `skillName` 为 `SwordSlash`）；仅 3 个实例把 `isUnlocked` 改为 1。
  - 技能点来源：`PlayerStatsModel.AddExp` 的 `LevelUp(levelsGained)` → `SkillTreeManager.UpdateAbilityPoints` → `StatsService.UpdateSkillPoints`；`UpdateSkillPoints` 不广播事件，`pointsText` 只在 `UpdateAbilityPoints` 里刷新。
  - 可重试订阅：`TrySubscribeLevelUp` 用 `listeningToLevelUp` 幂等，`OnEnable` 与 `Start` 各调一次（`SkillTreeManager.cs:18,30-46,66`）。
  - 效果作用域只有两处：`MaxHealthBoost` → `StatsService.UpdateMaxHealth(1)`+`UpdateHealth(1)`；`SwordSlash` → `PlayerCombat.SetActive(true)`。
  - 「保留的验证点」核实结论：旧文档 §2.2 的迁移方案**未动工**——`SkillSystemModel`/`SkillService`/`SkillEffectApplier`/`SkillTreeController` 在 `Assets/Scripts` 全量检索无命中，`SkillSO` 未接 `GuidSO`。
  - 技能等级/解锁不入档（`SaveData.cs:9-11`），技能点随 `PlayerStatsData.skillPoints` 入档。
- 未核验项：见 Guide §5（24 个槽位是否同属一个面板分组、3 个预解锁槽位是否为设计根节点、`combat`/`pointsText` 接线、`OnEnable` 与 `StatsService.Awake` 的实际先后、技能不入档是否为设计意图等 7 条，均标注为「未运行 Unity 验证」）。
- 发现的缺陷：字符名分发（D2）与点数校验/扣减分裂（D3）代码现状与旧文档一致；`SkillManager` 未覆盖的技能名静默无效果；点数不足无反馈；`UnsubscribeLevelUp` 在 `StatsService.Instance == null` 时无法真正退订；`SkillSlot.UpdateUI` 无 `skillSO` 空守卫；满级解锁为全局扫描；技能域无测试覆盖。
- 文档冲突记录：`Docs/游戏指南.txt:20` 写「左键技能槽位：消耗技能点解锁技能」，与代码不符——点击消耗技能点做的是**升级**（`TryUpgradeSkill`），解锁是由 `HandleSkillMaxed` 在前置满级时自动完成（`SkillTreeManager.cs:56-63`）；已在 Guide §2.1/§2.3 按代码描述。
- 结构说明：原 Router 的「下级导航」子表只有占位行 `| None | None |`（无真实子类），重写时按项目中文模板 `Workflow\Templates\BusinessRouter.template.md:16-18` 改写为 `| 无 | 无 |`，未删除任何真实子类行。
- 维护计数：`0/5`（未变更，本次仅建立文档）。