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

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Gameplay.Dialog`
- 脚本区：对话域运行时脚本目录及其中的 HistoryManager 子目录
- 文档区：本 Router 与 `Dialog_Guide.md` 所在目录

## 能力边界

**Active 能力**

- 本域权威文档为 `Dialog_Guide.md`（ID `GP-DIALOG-GUIDE`），覆盖会话流程、对话树数据模型、Refuse 判定、历史记录的写入与读取、NPC 触发链、对话画布开关归属。
- HistoryManager 子目录承载 `ConversationHistoryManager`、`ItemHistoryManager`、`VisitedHistoryManager` 三个管理器，未建立下级 Router，事实记在 `Dialog_Guide.md` 的 §2.4 与 §3。
- 对话画布的开关请求由 NPC 触发器承接，`DialogManager.ToggleCanvasEvent` 返回 null；改动这一归属需同时核对 `Dialog_Guide.md` §2.5 与 `Units\Router.md`。
