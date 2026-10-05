# Gameplay.Dialog Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Gameplay.Dialog 权威文档

**写入的文件**
- `Y_MultipleAgentWorkflow/Gameplay/Dialog/Dialog_Guide.md`（新建，ID `GP-DIALOG-GUIDE`，状态 Active）
- `Y_MultipleAgentWorkflow/Gameplay/Dialog/Router.md`（重写为项目中文模板，维护计数 `0/5`）
- 本 DeveloperLog

**依据的证据路径**
- 代码：`Assets/Scripts/Gameplay/Dialog/DialogManager.cs`、`Assets/Scripts/Gameplay/Dialog/HistoryManager/{ConversationHistoryManager,ItemHistoryManager,VisitedHistoryManager}.cs`、`Assets/Scripts/Pipeline/SO/{DialogSO,RefuseDialogSO,CharacterSO}.cs`、`Assets/Scripts/Gameplay/Units/NPC/{NPCDialogTrigger,NPCStateController}.cs`、`Assets/Scripts/Gameplay/Inventory/InventoryManager.cs`、`Assets/Scripts/Contracts/{MyEnums,ICanvasManager,YSingleton}.cs`、`Assets/Scripts/Pipeline/UI/SystemCanvasManagers/{UIManager,CanvasFocusStack}.cs`
- 资产：`Assets/GameSO/ChatSOs/**`（PurpleBob 线全部节点）、`Assets/GameSO/Events/ToggleCanvasEvents/ToggleDialogEvent.asset`
- 接线：`Assets/Scenes/GameScene/PersistentScene.unity`（DialogManager 组件、UIManager 的 toggleCanvasEvents/inputBindings）、`Scene1.unity`/`Scene2.unity`（NPCDialogTrigger 入口对话）
- 旧文档线索：`Docs/My_ARPG_MVCS项目现状.md`（第 108 行 DialogManager 职责描述，判定为未实施的设计建议）

**已核验**
- 会话推进三段式、Refuse 判定（`isDefaultChat` 与 `require*` 两套反向语义）、一次性拦截需父子两处配合、历史写在 `EndDialog` 且 `ForeceEndDialog` 不写、NPC 组件启停做范围判定、ToggleDialogEvent 已在 UIManager 列表并有 canvas:4 绑定。
- 全部论断均落到具体 file:line（见 Guide 正文）。

**未核验**
- 未运行 Unity：按键开/关对话的实际行为、`onlyTriggeredOnce` 拦截在运行时的手感、空 `speaker` 是否会命中线上资产、`PurpleBobDefaultChat.asset` 是弃用还是待接线。
- 未逐个检查所有 `dialogLines` 的 speaker 配置完整性。

**发现的缺陷**
- 选项已显示时重复 `AdvanceDialog` 会重复 `AddListener`（`DialogManager.cs:95-100`、`:228`），一次点击可触发多次节点跳转。
- `StartRefuseDialog` 绕过条件判定（`:190-197`），拒绝节点上的 `refuseDialogs` 是死配置。
- 资产级陈旧序列化字段：`chatType`、`canOnlyBeTriggeredOnce`、`refusingDialogs`（后者与现名 `refuseDialogs` 不同）。
- 孤儿资产 `PurpleBobDefaultChat.asset`（全库除 `.meta` 外零引用）。
- 对话史/物品史均无持久化；物品史带符号累加会让 `requireItems` 门槛回退（隐性依赖背包域）。