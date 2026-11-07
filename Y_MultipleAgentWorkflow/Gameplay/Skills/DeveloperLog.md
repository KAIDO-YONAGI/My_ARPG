# Gameplay.Skills Developer Log

## 维护规则

按项目约定记录带日期的任务事实、验证证据与维护计数，现行结论同步到对应 Guide。了解现状优先读取 Guide；需要任务历史时，只定位并读取最近一天或两天的相关条目，禁止整份日志载入上下文。只有任务需要追溯历史时才按日期、标题或关键词定向读取更早条目。读取范围不改变存储保留，删除具体历史条目须经用户明确确认。纯文档维护不增加业务计数。

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-07 技能升级使用数据命令并缓存状态键

- `SkillTreeManager.TryUpgradeSkill` 接收技能键、等级上限与初始状态，优先读取已保存状态，统一校验解锁、上限与余额后保存等级并扣点。`SkillSlot` 读取命令结果更新显示；扣点静态事件及管理器对槽位类型的依赖已移除。
- 技能和点数提供独立版本，画布分别记录已绘制版本；`OnMaxSkillLevel` 继续由画布订阅以推进前置解锁。状态键在首次读取或绑定不同根时构建，常规刷新复用字符串。
- 回归验证成功升级、重复命令读取持久等级、无点/锁定/满级拒绝保持状态与版本，以及状态键缓存和根切换。全套 EditMode 80/80 通过，其中项目用例 79 项，证据为 `Logs/ui-version-editmode.xml`。
- 维护计数达到 `5/5`，对照三份运行脚本复核升级命令、版本读取、状态键和事件生命周期，更新 Guide 与 Router 后清零为 `0/5`。

## 2026-11-01 技能面板直接查询 State

`SkillsCanvasView` 通过 `CurrentState` 按键查询技能状态并读取技能点与脏标记，绘制后清理标记；不创建刷新快照。维护计数更新为 `3/5 -> 4/5`。

## 2026-11-01 技能面板改用只读刷新快照

`SkillTreeManager` 不再公开可变 `State`；槽位通过状态键读取技能记录，技能点文本和刷新字段通过版本快照同步并按绘制结果确认。维护计数更新为 `2/5 -> 3/5`。

## 2026-10-31 技能界面迁入缓存预制体

- `SkillTreeManager` 不再持 `skillSlots` 与 `pointsText`，改为 `SkillState` 存取 + `State` 脏标记；界面被淘汰前由 `SkillsCanvasView` 触发保存。
- 槽位规模更正：24 个 `SkillSlot`（23 个 `MaxHealthBoost`、1 个 `CombatUnlock`）现在位于 `SkillsCanvas.prefab`，由视图 `GetComponentsInChildren` 抓取。
- `SkillSlot` 自己在 `OnEnable` 绑定 `TryUpgradeSkill`，不再依赖管理器挂回调。

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
