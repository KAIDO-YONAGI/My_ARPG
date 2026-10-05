# Gameplay.Dialog Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Gameplay.Dialog 权威文档

**写入的文件**
- `Dialog_Guide.md`（新建，ID `GP-DIALOG-GUIDE`，状态 Active）
- `Router.md`（重写为项目中文模板，维护计数 `0/5`）
- 本 DeveloperLog

**依据的证据路径**
- 代码：`DialogManager`、`ConversationHistoryManager`、`ItemHistoryManager`、`VisitedHistoryManager`、`DialogSO`、`RefuseDialogSO`、`CharacterSO`、`NPCDialogTrigger`、`NPCStateController`、`InventoryManager`、`MyEnums`、`ICanvasManager`、`YSingleton`、`UIManager`、`CanvasFocusStack`
- 资产：ChatSOs 目录下 PurpleBob 线的全部节点，以及 `ToggleDialogEvent`。
- 接线：PersistentScene 场景的 DialogManager 组件与 UIManager 的 toggleCanvasEvents、inputBindings；Scene1 与 Scene2 的 NPCDialogTrigger 入口对话。

**已核验**
- 会话推进三段式、Refuse 判定（`isDefaultChat` 与 `require*` 两套反向语义）、一次性拦截需父子两处配合、历史写在 `EndDialog` 且 `ForeceEndDialog` 不写、NPC 组件启停做范围判定、ToggleDialogEvent 已在 UIManager 列表并有 canvas:4 绑定。
- 全部论断均落到具体 file:line（见 Guide 正文）。
