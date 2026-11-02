# Gameplay.Dialog Developer Log

## 维护规则

按项目约定记录带日期的任务事实、验证证据与维护计数，现行结论同步到对应 Guide。了解现状优先读取 Guide；需要任务历史时，只定位并读取最近一天或两天的相关条目，禁止整份日志载入上下文。只有任务需要追溯历史时才按日期、标题或关键词定向读取更早条目。读取范围不改变存储保留，删除具体历史条目须经用户明确确认。纯文档维护不增加业务计数。

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-01 对话视图直接读取 State

`DialogCanvasView` 从 `DialogManager.CurrentState` 读取当前行、对话与选项显隐，绘制后调用 `ClearDirty()`；不创建刷新快照。维护计数达到 `5/5`，复核实现并更新 Guide 后清零为 `0/5`。

## 2026-11-01 对话面板使用版本快照

`DialogManager` 隐藏可变刷新状态，`DialogCanvasView` 按当前行、对话资产和选项显隐字段读取快照，并只确认已经绘制且版本未变化的字段。维护计数更新为 `3/5 -> 4/5`。

## 2026-10-31 对话界面迁入缓存预制体

- `DialogManager` 不再实现 `ICanvasManager`、不持控件引用，只留对话状态与推进；新增 `DialogCanvasView`（继承 `CachedCanvasView`）承接立绘、正文与选项。
- `ShowDialog` / `ShowChoices` 改为纯状态推进并 `MarkDirty()`，渲染交给视图。
- 界面实体在 `DialogCanvas.prefab`，接线全在预制体内部。

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
