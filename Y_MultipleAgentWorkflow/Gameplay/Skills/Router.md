# Gameplay.Skills Router

文档 ID：`BUS-GAMEPLAY-SKILLS`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `技能树` / `技能面板` / `SkillTree` / `SkillSlot` | `Skills_Guide.md` |
| `技能点` / `SkillPoints` / `pointsText` / `UpdateAbilityPoints` | `Skills_Guide.md` |
| `解锁` / `isUnlocked` / `CanUnlockSkill` / `前置依赖` | `Skills_Guide.md` |
| `升级技能` / `TryUpgradeSkill` / `maxLevel` | `Skills_Guide.md` |
| `SkillManager` / `技能没效果` / `MaxHealthBoost` / `SwordSlash` | `Skills_Guide.md` |
| `SkillSO` / 技能改名 / 技能资源 | `Skills_Guide.md` |
| `OnAbilityPointSpent` / `OnMaxSkillLevel` / static 事件 | `Skills_Guide.md` |
| `ToggleSkillsEvent` / `SkillTreeCanvasManager` | `Skills_Guide.md` |
| `保留的验证点` / `SkillService` / 技能域迁移 | `Skills_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Gameplay.Skills`
- `path:Y_MultipleAgentWorkflow\Gameplay\Skills`
- `path:Assets\Scripts\Gameplay\Skills`
- `path:Assets\GameSO\UI SO\SkillButtonSO`

## 能力边界

**Active 能力**
- `Skills_Guide.md`（ID `GP-SKILLS-GUIDE`）是本域当前实现的权威描述：槽位状态与解锁依赖、技能点收支、`SkillTreeManager`/`SkillManager` 分工、可重试 LevelUp 订阅、效果作用域。
- 本域实现文件：`Assets/Scripts/Gameplay/Skills/{SkillSlot,SkillTreeManager,SkillManager,SkillTreeCanvasManager}.cs`、`Assets/Scripts/Pipeline/SO/SkillSO.cs`；配置资产 `Assets/GameSO/UI SO/SkillButtonSO/{MaxHealthBoost,CombatUnlock}.asset`；接线在 `Assets/Scenes/GameScene/PersistentScene.unity` 与 `Assets/Prefabs/UI/Buttons/SkillButton.prefab`。
- 跨域边界：技能点的字段与升级事件属 `Gameplay.PlayerStats`；技能面板开关基建属 UI 域；剑/弓行为属战斗域。

**Proposal**
- 无落地的 Proposal 文档。旧文档 `Docs/My_ARPG_MVCS项目现状.md:134-161` 的 `SkillSystemModel`/`SkillService`/`SkillEffectApplier` 迁移方案经代码核对**未动工**（相关类型不存在），仅作为历史输入。

**需要用户确认**
- 技能等级与解锁是否入档（现状不入档，每局重置；技能点入档）。
- 是否按旧文档方向做技能域分层重构（消除两个 static 事件、字符串分发、点数校验/扣减分裂）。
- `SkillSO` 是否接入 `GuidSO` 以获得稳定 id（为将来存档铺路）。
- 同一个 `SkillSO` 被 23 个槽位复用、每槽位独立计 5 级的现状是否符合设计意图。
