# Gameplay.Dialog Router

文档 ID：`BUS-GAMEPLAY-DIALOG`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `对话`、`DialogManager`、`会话流程`、`选项按钮` | `Dialog_Guide.md` |
| `DialogSO`、`对话树`、`nextDialogOptions`、`parentDialog` | `Dialog_Guide.md` |
| `RefuseDialogSO`、`isDefaultChat`、`requireItems`、`拒绝对话` | `Dialog_Guide.md` |
| `onlyTriggeredOnce`、`一次性对话`、`重复对话` | `Dialog_Guide.md` |
| `ConversationHistoryManager`、`ItemHistoryManager`、`对话史`、`物品史` | `Dialog_Guide.md` |
| `NPCDialogTrigger`、`NPCStateController`、`Chat` 状态、`按键开对话` | `Dialog_Guide.md` |
| `对话框不显示`、`选项没出现`、`按钮重复触发` | `Dialog_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Gameplay.Dialog`
- `path:Assets/Scripts/Gameplay/Dialog/`
- `path:Y_MultipleAgentWorkflow/Gameplay/Dialog/`

## 能力边界

- Active：本域权威文档为 `Dialog_Guide.md`（`GP-DIALOG-GUIDE`），覆盖会话流程、对话树数据模型、Refuse 判定、历史记录、NPC 触发链。
- Active：`HistoryManager\` 目前是域内子目录（`ConversationHistoryManager` / `ItemHistoryManager` / `VisitedHistoryManager`），**未建立下级 Router**，其事实在 `Dialog_Guide.md` 的 §2.4 与 §3。
- Active：对话的开关画布请求由 NPC 触发器承接（`DialogManager.ToggleCanvasEvent` 为 null），改动此归属需同时核对 `Dialog_Guide.md` §2.5 与 `Units\Router.md`。
- Proposal：`VisitedHistoryManager` 为空类，位置条件（`DialogManager.cs:185` 的 TODO）未实现；补齐前不得据此写实现性文档。
- 需要用户确认：新增 `HistoryManager\Router.md` 或改变对话画布开关归属，须由用户确认后再动。
