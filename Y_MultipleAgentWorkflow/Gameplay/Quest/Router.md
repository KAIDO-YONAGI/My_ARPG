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
| `QuestProgressData`、`目标进度`、`进度不刷新` | `Quest_Guide.md` |
| `任务奖励`、`发奖`、`QuestRewardRequest`、`重复发奖` | `Quest_Guide.md` |
| `任务板`、`QuestBoardManager`、`范围触发`、`questsOnBoard` | `Quest_Guide.md` |
| `任务面板`、`QuestLogPanel`、`QuestLogSlot`、`画布` | `Quest_Guide.md` |
| `IsToComplete`、`Completed`、`任务面板只读` | `Quest_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Gameplay.Quest`
- `path:Assets/Scripts/Gameplay/Quest/`
- `path:Y_MultipleAgentWorkflow/Gameplay/Quest/`

## 能力边界

- Active：本域权威文档为 `Quest_Guide.md`（`GP-QUEST-GUIDE`），覆盖状态机与面板显隐、进度存储与重算、奖励通道、任务板/任务面板分层。
- Active：任务奖励只经 `InventorySlotsStatsSO`（`QuestRewardEvent.asset`）事件通道交付，`QuestManager` 不持背包引用；改动接收侧须同时核对 `InventoryShop\Router.md`。
- Active：任务进度不落 SO、也未接入存档，`QuestObjective.currentAmount` 为死字段；不得把资产上的 `currentAmount` 当进度来源。
- Proposal：`QuestManager` 的职责拆分（`QuestProgressModel`/`QuestRuntimeModel`/`QuestService`/`QuestLogView`，见旧文档 `Docs/My_ARPG_MVCS项目现状.md:107`）尚未实施，属方案而非现状。
- 需要用户确认：为 `Completed` 增加发奖幂等标记、或把任务进度接入存档，须由用户确认后再动。
