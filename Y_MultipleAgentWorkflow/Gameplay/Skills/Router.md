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
| `分层重构` / 技能域迁移 / `SkillService` | `Skills_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Gameplay.Skills`
- `path:Y_MultipleAgentWorkflow\Gameplay\Skills`
- 技能实现脚本 `SkillSlot`、`SkillTreeManager`、`SkillManager`、`SkillTreeCanvasManager`
- 配置类型与技能配置资产 `SkillSO`、`MaxHealthBoost`、`CombatUnlock`
- 场景与预制体接线 常驻场景、技能按钮预制体

## 能力边界

**Active 能力**
- `Skills_Guide.md` 是本域当前实现的权威描述，ID 为 `GP-SKILLS-GUIDE`，覆盖槽位状态与解锁依赖、技能点收支、`SkillTreeManager` 与 `SkillManager` 分工、可重试的 LevelUp 订阅、效果作用域。
- 本域实现类型：`SkillSlot`、`SkillTreeManager`、`SkillManager`、`SkillTreeCanvasManager`、`SkillSO`；技能配置资产 `MaxHealthBoost` 与 `CombatUnlock`。
- 跨域边界：技能点的字段与升级事件属 `Gameplay.PlayerStats`；技能面板开关基建属 UI 域；剑与弓的行为属战斗域。

**Proposal**
- 无。

**需要用户确认**
- 技能等级与解锁是否入档；当前每局重置，技能点入档。
- 技能域是否做分层重构，消解两个 static 事件、字符串分发与点数校验扣减分裂。
- `SkillSO` 是否接入 `GuidSO` 以获得稳定 id。
- 同一个 `SkillSO` 被 23 个槽位复用、每槽位独立计 5 级是否符合设计意图。
