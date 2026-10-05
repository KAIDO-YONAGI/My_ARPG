# Gameplay.Dialog 权威指南（对话域当前实现）

文档 ID：`GP-DIALOG-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责"对话域当前代码与资产的实际行为"——对话树结构与推进、Refuse 前置条件判定、历史记录（角色/物品）的写入与读取、NPC 触发链、对话画布的开关归属。不负责任务域的状态机与发奖、不负责背包内部堆叠规则、不负责 UIManager 的焦点栈实现细节（仅在对话相关处引用）。
上游来源：`Assets/Scripts/Gameplay/Dialog/DialogManager.cs`、`Assets/Scripts/Gameplay/Dialog/HistoryManager/*.cs`、`Assets/Scripts/Pipeline/SO/DialogSO.cs`、`Assets/Scripts/Pipeline/SO/RefuseDialogSO.cs`、`Assets/Scripts/Pipeline/SO/CharacterSO.cs`、`Assets/Scripts/Gameplay/Units/NPC/NPCDialogTrigger.cs`、`Assets/Scripts/Gameplay/Units/NPC/NPCStateController.cs`、`Assets/Scripts/Gameplay/Inventory/InventoryManager.cs`、`Assets/GameSO/ChatSOs/**`、旧文档 `Docs/My_ARPG_MVCS项目现状.md`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `对话`、`Dialog`、`DialogManager` | §2.1 会话流程 |
| `DialogSO`、`对话树`、`nextDialogOptions`、`parentDialog` | §2.2 数据模型 |
| `RefuseDialogSO`、`isDefaultChat`、`requireItems`、`拒绝对话` | §2.3 Refuse 判定 |
| `onlyTriggeredOnce`、`一次性对话`、`重复对话` | §2.3 与 §3 第 2 条 |
| `ConversationHistoryManager`、`HasChatedWith`、`RecordDialogHasChated` | §2.4 对话史 |
| `ItemHistoryManager`、`HasPickedOverAmount` | §2.4 物品史 |
| `NPCDialogTrigger`、`NPCStateController`、`Chat` 状态 | §2.5 NPC 触发链 |
| `对话框不显示`、`选项没出现`、`按钮重复` | §4 已知缺陷 |

## 2. 当前实现

### 2.1 会话流程（`DialogManager`）

`DialogManager : YSingleton<DialogManager>, ICanvasManager`（`Assets/Scripts/Gameplay/Dialog/DialogManager.cs:5`）是唯一的会话驱动者，持有全部 UI 引用（`:8-16`）。它的画布开关事件**刻意返回 null**：`:19` 注释写明"Dialog 为仅上报面板：不接收 UIManager 的开关请求"，因为开/关请求实际由 NPC 触发器承接（见 §2.5）。

- `StartDialog(DialogSO)`：空引用与条件判定（`:58-68`）→ `SetDialogCanvas(true)`、`DisableButtons()`、`currentDialog=dialog`、`currentLineIndex=0`、`ShowDialog()`（`:70-74`）。
- `AdvanceDialog()`：三段式（`:77-101`）。①`currentLineIndex < dialogLines.Length` → `ShowDialog()` 播下一句；②台词播完且 `nextDialogOptions.Length == 0` → `EndDialog()`；③台词数非 0 且 `currentLineIndex == dialogLines.Length` → `ShowChoices()`。
- `ShowDialog()`：逐句写立绘/名字/文本，末尾 `currentLineIndex++`（`:232-263`）。
- `ShowChoices()`：先 `InitializeButtons()`（显隐并写选项文本 `:284-323`），再逐个 `AddListener` 绑定 `nextDialogNode`（`:213-229`）。
- `OnOptionSelected(DialogSO)`：非空则 `StartDialog(nextDialog)` + `DisableButtons()`，为空则 `EndDialog()`（`:325-336`）。
- `EndDialog()`（`:103-124`）：关画布、`DisableButtons()`，然后写历史（§2.3 末），最后清 `currentDialog`/`currentLineIndex`。
- `ForeceEndDialog()`（`:126-132`）：只关画布与清状态，**不写历史**。
- 生命周期：`OnSingletonInitialized` 初始关闭画布（`:25-29`）；`OnEnable/OnDisable` 订阅同一 `sceneLoadedEvent`（`:31-40`）；切场景 `ForeceEndDialog()`（`:42-45`）。

### 2.2 数据模型（对话树）

`DialogSO`（`Assets/Scripts/Pipeline/SO/DialogSO.cs:6-20`）字段：`mainCharacter`、`dialogLines[]`、`nextDialogOptions[]`、`refuseDialogs`（`List<RefuseDialogSO>`）、`onlyTriggeredOnce`、`parentDialog`。结构体：`DialogLine{speaker,text}`（`:22-26`）、`DialogOption{optionText,nextDialogNode}`（`:28-32`）、`Item{itemSO,quantity}`（`:34-38`，被 RefuseDialogSO 复用，勿与 `ItemSO` 混淆）。树是 SO 之间互相引用构成：父节点的选项直接引用子 `DialogSO`（`:31`）。

`CharacterSO : GuidSO` 提供 `characterName` + `characterPortrait`（`Assets/Scripts/Pipeline/SO/CharacterSO.cs:5-9`）。

线上真实资产（紫 Bob 线，可作配置范例）：
- 主节点 `PurpleBobs Greeting`：3 句台词 → 2 个选项（Know More → `KnowMoreDialog`，No Intersted → `NoIntersted`），`refuseDialogs` 挂 `RefuseByCharacter`（`Assets/GameSO/ChatSOs/PurpleBobChats/NormalChats/PurpleBobs Greeting.asset:23-29`）。
- 主节点 `PurpleBobPickMushroom`：`refuseDialogs = [DefaultChat, RefuseByPickMushRoom]`，选项 Agree → `AgreeToGiveMushroom`、Refuse → `RefuseToGiveMushroom`（`.../NormalChats/PurpleBobPickMushroom.asset:21-28`）。
- 子节点 `AgreeToGiveMushroom`：`onlyTriggeredOnce: 1`、`parentDialog` 指回 `PurpleBobPickMushroom`（`.../Options/AgreeToGiveMushroom.asset:22-23`）。

### 2.3 Refuse 前置条件与一次性拦截

入口只有一处：`StartDialog` 内的 `MatchConditionsToStartDialog(dialog)`（`DialogManager.cs:65`），它按**列表顺序**取第一个命中的拒绝策略（`:134-158`）：

- `refuse.isDefaultChat == true` → `shouldRefuse = HasDialogChated(dialog)`，注意判定对象是**正在启动的主节点本身**（`:142-144`）。
- 否则 `shouldRefuse = !HasRefuseConditions(refuse)`（`:147`）——语义是"要求不满足才拒绝"。

`HasRefuseConditions`（`:160-188`）：`requireCharacters` 全部满足 `HasChatedWith`，且 `requireItems` 全部满足 `HasPickedOverAmount(itemSO, quantity)`，才返回 true；位置条件未实现（`:185` 的 `//TODO 添加检测位置有没有去过的逻辑`）。任一要求未满足即落到该拒绝节点上。

命中后 `StartRefuseDialog(refuse)`（`:190-197`）**直接** `SetDialogCanvas(true)` + `ShowDialog()`，不再递归做 `MatchConditionsToStartDialog`——即拒绝节点自身的 `refuseDialogs` 不会被求值。

一次性/父分支拦截的完整闭环（两处配合）：
1. 子节点结束时写父节点：`EndDialog()` 中 `if (currentDialog.onlyTriggeredOnce && currentDialog.parentDialog != null) → RecordDialogHasChated(currentDialog.parentDialog)`（`:115-119`）。
2. 父节点下次启动时被 `isDefaultChat` 策略拦住：`DefaultChat.asset` 的 `isDefaultChat: 1`（`Assets/GameSO/ChatSOs/PurpleBobChats/ChatsWhileRefuse/DefaultChat.asset:23`）挂在 `PurpleBobPickMushroom.refuseDialogs[0]`。

**因此 `onlyTriggeredOnce` 单独勾选不会产生任何拦截效果**：必须有父节点侧的 `isDefaultChat` 拒绝节点才能生效。

写历史的时机：仅在 `EndDialog()` 内，且只写两件事——`RecordCharacter(currentDialog.mainCharacter)`（`:110-113`）与上述父节点标记（`:115-119`）。`ForeceEndDialog()` 不写。拒绝节点走 `EndDialog` 时也会 `RecordCharacter`（`:108-113` 对当前 `currentDialog` 一律生效）。

### 2.4 历史记录（对话史 / 物品史）

`ConversationHistoryManager : YSingleton`（`Assets/Scripts/Gameplay/Dialog/HistoryManager/ConversationHistoryManager.cs:6`）：
- `HashSet<CharacterSO> charactersHasChated`（`:8`）、`HashSet<int> dialogsHasChated`（`:10`）。
- 对话节点用 `dialog.GetInstanceID()` 做键（`:23`、`:27`）——**是运行时实例 ID，不是 SO guid**。

`ItemHistoryManager : YSingleton`（`.../ItemHistoryManager.cs:6`）：`Dictionary<ItemSO,int> itemHasPicked`（`:9`）、`RecordItem`（`:11-22`）、`HasPickedOverAmount`（`:24-30`）、`GetItemQuantity`（`:31-36`）。`:24` 注释自述"当前数量归零的并不会删除"。

`VisitedHistoryManager : YSingleton` 是**空类**（`.../VisitedHistoryManager.cs:5-7`），无任何成员。

写入方全部在背包域 `InventoryManager`：金币进出 `:102`、出售扣减 `:123`、拾取/购买入库 `:136`、使用消耗 `:225`（`Assets/Scripts/Gameplay/Inventory/InventoryManager.cs`）。读取方有两个域：对话域 `DialogManager.cs:178-179`，任务域 `QuestManager.cs:292`。

### 2.5 NPC 触发链与画布归属

`NPCStateController` 用组件启停做"范围内"判定：`dialogTrigger.enabled = currentState == NPCState.Chat`（`Assets/Scripts/Gameplay/Units/NPC/NPCStateController.cs:25`），玩家进入/离开触发器切到 `Chat` / `DefaultState`（`:28-41`，默认 `Patrol`，`:14`）。

`NPCDialogTrigger`（`.../NPCDialogTrigger.cs`）承接 UIManager 的对话开关请求：订阅 `toggleDialogEvent`（`:9`、`:30`），`state == true` 只置 `openDialogRequested`（`:83`），`state == false` 直接 `ForeceEndDialog()`（`:76-81`）。`Update()` 中：请求打开且 `!isDialogActive` 才 `StartDialog(dialogSO)`（`:92-100`）；对话中且 `advanceDialogAction.WasPressedThisFrame()` 才 `AdvanceDialog()`（`:102-105`）。`OnDisable` 也会 `ForeceEndDialog()`（`:64-67`）。

资产与接线证据：`ToggleDialogEvent.asset` 的 `canvasToToggle: 4`（`Assets/GameSO/Events/ToggleCanvasEvents/ToggleDialogEvent.asset:15`，枚举序见 `MyEnums.cs:56-72`，Dialog=4），该资产确实登记在 UIManager 的 `toggleCanvasEvents` 列表里（`Assets/Scenes/GameScene/PersistentScene.unity:15110`，同列表 `:15106-15115`），并有 `canvas: 4` 的输入绑定（`:15125-15126`）。`DialogManager` 组件本身在 `PersistentScene.unity:9261`，配了 4 个选项按钮与 4 个选项文本（`:9269-9278`）。场景内 `NPCDialogTrigger` 的入口对话引用：`Scene1.unity:8680` 用 `PurpleBobPickMushroom`、`Scene1.unity:21081` 与 `Scene2.unity:645` 用 `PurpleBobs Greeting`。

## 3. 约定与硬边界

1. **选项节点必须有至少一句台词**。`StartDialog → ShowDialog()` 在 `dialogLines.Length == 0` 时直接 `EndDialog()`（`DialogManager.cs:234-238`），`ShowChoices()` 因此永远不会被调用——纯选项节点会一闪而过地结束对话。
2. **`onlyTriggeredOnce` 依赖父节点的 `isDefaultChat` 策略**。缺少父侧拒绝节点时该字段完全不生效（§2.3），表现是"分支可以反复进入"。
3. **对话史的键是 `GetInstanceID()`**（`ConversationHistoryManager.cs:23`）。它只在单次进程生命内有效，且不随存档持久化；任何"按资源身份查对话史"的假设都不成立。
4. **Refuse 判定会读背包域写的物品史**（`DialogManager.cs:178-179` ← `InventoryManager.cs:102/123/136/225`）。`RecordItem(..., -removed)` 是**带符号累加**，卖出/使用会让数量回退甚至为负，因此 `requireItems` 的门槛可能在"已满足"之后再次失效——这是对背包域行为的隐性依赖，改背包任何记账口径都会改对话可进入性。
5. **对话画布的开关请求不进 `DialogManager`**。`ToggleCanvasEvent => null`（`DialogManager.cs:19`），真正订阅 `ToggleDialogEvent` 的是 NPC 触发器（`NPCDialogTrigger.cs:30`）。要改"按键开对话/关对话"的行为，改点在触发器，不在管理器。
6. **`ForeceEndDialog()` 不写历史**（`:126-132`）：切场景、离开 NPC 触发范围、收到 `toggle(false)` 都会走它，等于放弃本次对话的全部历史记录。
7. `dialogLines[i].speaker` 无空值保护（`:249`、`:254` 直接取 `characterPortrait`/`characterName`）：漏配说话人会抛 `NullReferenceException`。

## 4. 已知缺陷与风险

1. **重复监听累积**：`AdvanceDialog()` 在选项已显示时再次被调用，会重复走到 `ShowChoices()`（`:95-100`），而 `ShowChoices()` 只 `AddListener` 从不 `RemoveAllListeners`（`:228`），同一按钮会挂多份回调；一次点击可能连续触发多次 `OnOptionSelected`（进而多次 `StartDialog`）。
2. **拒绝节点不递归判条件**：`StartRefuseDialog` 绕过了 `MatchConditionsToStartDialog`（`:190-197`），拒绝节点上挂的 `refuseDialogs` 是死配置。
3. **`isDefaultChat` 的拦截对象是主节点自身**（`:143`），但书写历史写的是 `parentDialog`（`:118`）。二者靠"子节点的 parentDialog 恰好等于父节点"的人工约定对齐，配错即静默失效。
4. **资产级陈旧序列化字段**：旧字段名残留在 YAML 中，当前代码已无这三个字段——`chatType`、`canOnlyBeTriggeredOnce`、`refusingDialogs`。实例：`.../Options/KnowMoreDialog.asset:15-16`（`chatType` + `canOnlyBeTriggeredOnce`）、`.../ChatsWhileRefuse/RefuseByCharacter.asset:15`（`canOnlyBeTriggeredOnce`，且该资产无 `onlyTriggeredOnce` 行）、`.../PurpleBobDefaultChat.asset:15-16,31`（三处全有）。其中 `refusingDialogs` 与当前字段名 `refuseDialogs` 不同，说明该资产从未带上拒绝列表（其 `refuseDialogs` 实际为空）。
5. **孤儿资产**：`PurpleBobDefaultChat.asset`（guid `589e62de…`）在全工程内除自身 `.meta` 外无任何引用（全库 grep 仅命中 `.meta`），是死资源。
6. **`ItemHistoryManager` 无持久化**：与对话史同为进程内状态，未接入存档（`Assets/Scripts/Gameplay/Save/` 内无相关引用）。跨读档后"捡过什么"会丢。
7. 旧文档 `Docs/My_ARPG_MVCS项目现状.md:108` 把 `DialogManager` 描述为"同时装会话流程、条件判断、节点跳转、按钮监听、文本与立绘显示"并给出拆分方案，属**未实施的设计建议**，不是当前实现。

## 5. 未核验事项

- 假设：`ToggleDialogEvent` 在 UIManager 的 `inputBindings` 里对应的按键实际可用（未运行 Unity 验证按键行为，只核对了资产登记与 binding 存在）。
- 假设：`DialogManager.IsDialogActive` 与 NPC 触发器的启停配合下，"离开范围立刻结束对话"是预期设计（代码如此，但未见设计说明）。
- 假设：`onlyTriggeredOnce` 与 `parentDialog` 的配对是策划约定而非工具校验项（工程内未发现校验代码，未运行 Unity 验证）。
- 假设：`SpeakerPortrait` 的空 speaker 崩溃在现网资产中不会触发（未逐个检查所有 `dialogLines` 的 speaker 配置）。
- 假设：`VisitedHistoryManager` 空类是为位置条件预留（`DialogManager.cs:185` 有 TODO，但两处无代码关联，未验证意图）。
- 假设：`PurpleBobDefaultChat.asset` 属于弃用而非待接线（无法从代码判断，仅确认当前零引用）。
