# Gameplay.Skills Router

文档 ID：`BUS-GAMEPLAY-SKILLS`
状态：`Active`
维护计数：`4/5`
最后更新：`2026-11-01`

## 维护规则

任务路由、权威文档或职责边界变化时更新本文，详细文档索引在所属功能域 Router 就近维护。证据记录在[开发日志](DeveloperLog.md)；计数与维护周期遵循[工作流维护规则](../../Workflow/Workflow_Guide.md#维护规则)。

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `技能树` / `技能面板` / `SkillTree` / `SkillSlot` | `Skills_Guide.md` |
| `技能点` / `SkillPoints` / `pointsText` / `UpdateAbilityPoints` | `Skills_Guide.md` |
| `解锁` / `isUnlocked` / `CanUnlockSkill` / `前置依赖` | `Skills_Guide.md` |
| `升级技能` / `TryUpgradeSkill` / `maxLevel` | `Skills_Guide.md` |
| `SkillSlot` / `MaxHealthBoost` / `CombatUnlock` / `技能没效果` | `Skills_Guide.md` |
| `SkillSO` / 技能改名 / 技能资源 | `Skills_Guide.md` |
| `OnAbilityPointSpent` / `OnMaxSkillLevel` / static 事件 | `Skills_Guide.md` |
| `ToggleSkillsEvent` / `SkillsCanvasView` | `Skills_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Gameplay.Skills`
- `path:Y_MultipleAgentWorkflow\Gameplay\Skills`
- 技能实现脚本 `SkillSlot`、`SkillTreeManager`、`SkillsCanvasView`
- 配置类型与技能配置资产 `SkillSO`、`MaxHealthBoost`、`CombatUnlock`
- 场景与预制体接线 常驻场景、技能按钮预制体

## 能力边界

**Active 能力**
- `Skills_Guide.md` 是本域当前实现的权威描述，ID 为 `GP-SKILLS-GUIDE`，覆盖槽位状态与解锁依赖、技能点收支、`SkillTreeManager` 的职责与状态同步、可重试的 LevelUp 订阅。
- 本域实现类型：`SkillSlot`、`SkillTreeManager`、`SkillsCanvasView`、`SkillSO`；技能配置资产 `MaxHealthBoost` 与 `CombatUnlock`。
- 跨域边界：技能点的字段与升级事件属 `Gameplay.PlayerStats`；技能面板开关基建属 UI 域；剑与弓的行为属战斗域。
