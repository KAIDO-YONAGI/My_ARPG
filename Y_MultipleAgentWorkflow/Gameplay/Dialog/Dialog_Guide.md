# Gameplay.Dialog 权威指南（对话域当前实现）

文档 ID：`GP-DIALOG-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：对话域当前代码与资产的实际行为，含对话树结构与推进、Refuse 前置条件判定、角色与物品历史记录的写入与读取、NPC 触发链、对话画布的开关归属。任务域的状态机与发奖、背包内部堆叠规则、UIManager 的焦点栈实现细节分别归任务域、背包域与架构域文档。
上游来源：对话域运行时脚本 `DialogManager` 与 HistoryManager 三个管理器；数据资产类型 `DialogSO`、`RefuseDialogSO`、`CharacterSO`；NPC 侧脚本 `NPCDialogTrigger`、`NPCStateController`；背包记账方 `InventoryManager`；线上对话资产 `ChatSOs` 系列。

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

`DialogManager : YSingleton<DialogManager>, ICanvasManager` 是唯一的会话驱动者，持有全部 UI 引用。它的画布开关事件返回 null，设计意图是对话面板只上报状态，开与关的请求由 NPC 触发器承接，见 §2.5。

- `StartDialog(DialogSO)`：先做空引用与条件判定，再开画布、`DisableButtons()`、记录 `currentDialog` 与 `currentLineIndex = 0`、调用 `ShowDialog()`。
- `AdvanceDialog()`：三段式推进。台词未播完时 `ShowDialog()` 播下一句；台词播完且 `nextDialogOptions` 为空时 `EndDialog()`；台词有内容且 `currentLineIndex` 已到台词数末尾时 `ShowChoices()`。
- `ShowDialog()`：逐句写立绘、名字与文本，末尾自增 `currentLineIndex`。
- `ShowChoices()`：先 `InitializeButtons()` 控制按钮显隐并写选项文本，再逐个 `AddListener` 绑定 `nextDialogNode`。
- `OnOptionSelected(DialogSO)`：目标非空时 `StartDialog(nextDialog)` 并 `DisableButtons()`，为空时 `EndDialog()`。
- `EndDialog()`：关画布、`DisableButtons()`、写历史，最后清空 `currentDialog` 与 `currentLineIndex`。
- `ForeceEndDialog()`：只关画布并清状态，历史记录留给 `EndDialog()` 写。
- 生命周期：`OnSingletonInitialized` 初始关闭画布；`OnEnable` 与 `OnDisable` 订阅同一 `sceneLoadedEvent`；切场景时调用 `ForeceEndDialog()`。

### 2.2 数据模型（对话树）

`DialogSO` 字段：`mainCharacter`、`dialogLines[]`、`nextDialogOptions[]`、`refuseDialogs`（`List<RefuseDialogSO>`）、`onlyTriggeredOnce`、`parentDialog`。结构体三个：`DialogLine` 含 `speaker` 与 `text`；`DialogOption` 含 `optionText` 与 `nextDialogNode`；`Item` 含 `itemSO` 与 `quantity`，被 `RefuseDialogSO` 复用，与 `ItemSO` 是不同用途的类型。树由 SO 之间互相引用构成，父节点的选项直接引用子 `DialogSO`。

`CharacterSO : GuidSO` 提供 `characterName` 与 `characterPortrait`。

线上真实资产（紫 Bob 线，可作配置范例）：

- 主节点 `PurpleBobs Greeting`：3 句台词、2 个选项，Know More 指向 `KnowMoreDialog`，No Intersted 指向 `NoIntersted`，`refuseDialogs` 挂 `RefuseByCharacter`。
- 主节点 `PurpleBobPickMushroom`：`refuseDialogs` 为 `DefaultChat` 与 `RefuseByPickMushRoom` 两项，选项 Agree 指向 `AgreeToGiveMushroom`，Refuse 指向 `RefuseToGiveMushroom`。
- 子节点 `AgreeToGiveMushroom`：`onlyTriggeredOnce` 为真，`parentDialog` 指回 `PurpleBobPickMushroom`。

### 2.3 Refuse 前置条件与一次性拦截

入口只有一处：`StartDialog` 内的 `MatchConditionsToStartDialog(dialog)`，它按列表顺序取第一个命中的拒绝策略。

- `refuse.isDefaultChat` 为真时，`shouldRefuse = HasDialogChated(dialog)`，判定对象是正在启动的主节点本身。
- 否则 `shouldRefuse = !HasRefuseConditions(refuse)`，语义是要求不满足才拒绝。

`HasRefuseConditions`：`requireCharacters` 全部满足 `HasChatedWith`，且 `requireItems` 全部满足 `HasPickedOverAmount(itemSO, quantity)`，才返回真。位置条件处于待实现状态，代码中留有对应 TODO。任一要求未满足即落到该拒绝节点上。

命中后 `StartRefuseDialog(refuse)` 直接开画布并显示台词，跳过 `MatchConditionsToStartDialog`，拒绝节点自身的 `refuseDialogs` 处于不会被求值的状态。

一次性与父分支拦截靠两处配合闭环：

1. 子节点结束时写父节点：`EndDialog()` 中当 `currentDialog.onlyTriggeredOnce` 为真且 `currentDialog.parentDialog` 非空时，调用 `RecordDialogHasChated(currentDialog.parentDialog)`。
2. 父节点下次启动时被 `isDefaultChat` 策略拦住：`DefaultChat` 资产的 `isDefaultChat` 为真，挂在 `PurpleBobPickMushroom` 的 `refuseDialogs` 首位。

因此 `onlyTriggeredOnce` 单独勾选不会产生拦截效果，需配合父节点侧的 `isDefaultChat` 拒绝节点。

写历史的时机只在 `EndDialog()` 内，写入两件事：`RecordCharacter(currentDialog.mainCharacter)` 与上述父节点标记。`ForeceEndDialog()` 走另一条路径，不写历史。拒绝节点走 `EndDialog` 时同样会 `RecordCharacter`，该调用对当时的 `currentDialog` 一律生效。

### 2.4 历史记录（对话史与物品史）

`ConversationHistoryManager : YSingleton`：

- 持有 `HashSet<CharacterSO> charactersHasChated` 与 `HashSet<int> dialogsHasChated`。
- 对话节点用 `dialog.GetInstanceID()` 做键，是运行时实例 ID。

`ItemHistoryManager : YSingleton`：`Dictionary<ItemSO,int> itemHasPicked` 配合 `RecordItem`、`HasPickedOverAmount`、`GetItemQuantity`；数量归零的条目保留在字典中。

`VisitedHistoryManager : YSingleton` 是空类，无任何成员。

写入方全部在背包域的 `InventoryManager`：金币进出、出售扣减、拾取与购买入库、使用消耗四处调用 `RecordItem`。读取方有对话域 `DialogManager` 的 Refuse 判定与任务域 `QuestManager` 的进度重算。

### 2.5 NPC 触发链与画布归属

`NPCStateController` 用组件启停做范围内判定：玩家进入触发器时把状态切到 `Chat` 并启用对话触发器，离开时切回 `DefaultState`，初始状态为 `Patrol`。

`NPCDialogTrigger` 承接 UIManager 的对话开关请求：订阅 `toggleDialogEvent`，收到开启只置 `openDialogRequested`，收到关闭直接 `ForeceEndDialog()`。`Update()` 中，存在开启请求且对话未激活时才 `StartDialog(dialogSO)`；对话进行中且 `advanceDialogAction.WasPressedThisFrame()` 时 `AdvanceDialog()`。`OnDisable` 同样 `ForeceEndDialog()`。

资产与接线：`ToggleDialogEvent` 事件资产的 `canvasToToggle` 为 4，对应 `MyEnums` 画布枚举中 Dialog 的取值；该资产登记在常驻场景 UIManager 的 `toggleCanvasEvents` 列表里，并有 canvas 4 的输入绑定。`DialogManager` 组件挂在常驻场景中，配了 4 个选项按钮与 4 个选项文本。场景内 `NPCDialogTrigger` 的入口对话引用：Scene1 的两个触发器分别用 `PurpleBobPickMushroom` 与 `PurpleBobs Greeting`，Scene2 的一个触发器用 `PurpleBobs Greeting`。

## 3. 约定与硬边界

1. **选项节点至少需要一句台词**。`StartDialog` 进入 `ShowDialog()` 时若 `dialogLines` 为空会直接 `EndDialog()`，`ShowChoices()` 因此不会被调用，纯选项节点会一闪而过地结束对话。
2. **`onlyTriggeredOnce` 依赖父节点的 `isDefaultChat` 策略**。缺少父侧拒绝节点时该字段不生效，表现为分支可以反复进入。
3. **对话史的键是 `GetInstanceID()`**，只在单次进程生命内有效，不随存档持久化，查询对话史只能按运行时实例 ID 进行。
4. **Refuse 判定读背包域写的物品史**。`RecordItem(..., -removed)` 是带符号累加，卖出与使用会让数量回退甚至为负，`requireItems` 的门槛可能在满足之后再次失效；背包域任何记账口径的改动都会影响对话可进入性。
5. **对话画布的开关请求由 NPC 触发器承接**：触发器订阅 `ToggleDialogEvent`，`DialogManager` 的 `ToggleCanvasEvent` 返回 null。按键开对话与关对话的改点在触发器。
6. **`ForeceEndDialog()` 只清状态与画布**。切场景、离开 NPC 触发范围、收到关闭请求都走它，本次对话的历史记录只由 `EndDialog()` 写入。
7. **`dialogLines[i].speaker` 无空值保护**：`ShowDialog()` 直接取 `characterPortrait` 与 `characterName`，漏配说话人会抛 `NullReferenceException`。

## 4. 已知缺陷与风险

1. **重复监听累积**：选项已显示时 `AdvanceDialog()` 再次被调用会重复走到 `ShowChoices()`，而 `ShowChoices()` 只 `AddListener`，从不 `RemoveAllListeners`，同一按钮会挂多份回调，一次点击可能连续触发多次 `OnOptionSelected`，进而多次 `StartDialog`。
2. **拒绝节点不递归判条件**：`StartRefuseDialog` 跳过 `MatchConditionsToStartDialog`，拒绝节点上挂的 `refuseDialogs` 是死配置。
3. **判定与记账的对象不同**：`isDefaultChat` 拦截的是正在启动的主节点，写历史写的是 `parentDialog`，二者靠子节点的 `parentDialog` 指向父节点这一人工约定对齐，配错即静默失效。
4. **资产上仍保留三个序列化字段**：`chatType`、`canOnlyBeTriggeredOnce`、`refusingDialogs`，当前代码不读取它们。`KnowMoreDialog` 与 `RefuseByCharacter` 带前两者，`PurpleBobDefaultChat` 三者都有；`refusingDialogs` 与代码读取的 `refuseDialogs` 字段名不同，该资产的拒绝列表实际为空。
5. **零引用资产**：`PurpleBobDefaultChat` 除自身元数据外全库无引用。
6. **`ItemHistoryManager` 无持久化**：物品史与对话史同为进程内状态，未接入存档，跨读档后已捡过的物品会丢，进而影响 `requireItems` 类目标与任务进度。

## 5. 未核验事项

- 假设：`ToggleDialogEvent` 在 UIManager 的 `inputBindings` 里对应的按键实际可用；只核对了资产登记与输入绑定存在，运行期按键行为待编辑器实测。
- 假设：`DialogManager.IsDialogActive` 与 NPC 触发器的启停配合下，离开范围立刻结束对话是预期设计；代码如此，设计说明待补。
- 假设：`onlyTriggeredOnce` 与 `parentDialog` 的配对是策划约定，工程内未见校验代码，运行期约束力待编辑器实测。
- 假设：空 speaker 崩溃在现网资产中不会触发；未逐个检查所有 `dialogLines` 的 speaker 配置。
- 假设：`VisitedHistoryManager` 空类为位置条件预留；`DialogManager` 内的位置检测 TODO 与它之间暂无代码关联，意图待确认。
- 假设：`PurpleBobDefaultChat` 的定位待确认，当前只确认零引用这一事实。
