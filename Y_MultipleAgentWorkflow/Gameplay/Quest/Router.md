# Gameplay.Quest Router

文档 ID：`BUS-GAMEPLAY-QUEST`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `任务`、`QuestManager`、`任务状态机`、`接取`、`Decline` | `Quest_Guide.md` |
| `QuestSO`、`任务目标`、`requiredAmount`、`currentAmount` | `Quest_Guide.md` |
| `QuestProgressData`、`目标进度` | `Quest_Guide.md` |
| `任务奖励`、`发奖`、`QuestRewardRequest` | `Quest_Guide.md` |
| `任务板`、`QuestBoardManager`、`范围触发`、`questsOnBoard` | `Quest_Guide.md` |
| `任务面板`、`QuestLogPanel`、`QuestLogSlot`、`画布` | `Quest_Guide.md` |
| `IsToComplete`、`Completed`、`任务面板只读` | `Quest_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Gameplay.Quest`
- 脚本区：任务域运行时脚本目录
- 文档区：本 Router 与 `Quest_Guide.md` 所在目录

## 能力边界

**Active 能力**

- 本域权威文档为 `Quest_Guide.md`（ID `GP-QUEST-GUIDE`），覆盖状态机与面板显隐、进度存储与重算、奖励通道、任务板与任务面板分层。
- 任务奖励只经 `InventorySlotsStatsSO` 的事件通道交付，`QuestManager` 不持背包引用；改动接收侧须同时核对 `..\InventoryShop\Router.md`。
- 任务进度只存在 `QuestManager` 的内存字典里，未接入存档；资产上的 `QuestObjective.currentAmount` 是零读写字段，进度来源只看内存字典。
