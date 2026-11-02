# Gameplay.Quest 权威指南（任务域当前实现）

文档 ID：`GP-QUEST-GUIDE`
状态：`Active`
最后更新：`2026-11-01`
## 维护规则

本文件对应的实现、约束或规则发生变化时，按已核验事实更新相关章节，证据记录在[开发日志](DeveloperLog.md)。修改小标题时同步文档内目录；计数与维护周期遵循[工作流维护规则](../../Workflow/Workflow_Guide.md#维护规则)。

## 文档内目录

- [1. 触发线索](#1-触发线索)
- [2. 当前实现](#2-当前实现)
  - [2.1 状态机与面板（`QuestManager`）](#21-状态机与面板questmanager)
  - [2.2 进度存储（`QuestProgressData`）](#22-进度存储questprogressdata)
  - [2.3 奖励通道（事件交付）](#23-奖励通道事件交付)
  - [2.4 分层：任务板与任务面板](#24-分层任务板与任务面板)
  - [2.5 槽位视图](#25-槽位视图)
- [3. 约定与硬边界](#3-约定与硬边界)
  - [3.1 进度只活在 `questProgress` 字典里](#31-进度只活在-questprogress-字典里)
  - [3.2 进度按业务入口重算](#32-进度按业务入口重算)
  - [3.3 奖励只有事件通道](#33-奖励只有事件通道)
  - [3.4 完成奖励按状态迁移发放](#34-完成奖励按状态迁移发放)
  - [3.5 进入 `Completed` 的唯一数据前提是 `IsQuestObjDone`](#35-进入-completed-的唯一数据前提是-isquestobjdone)
  - [3.6 任务板需要先装载再开面板](#36-任务板需要先装载再开面板)
  - [3.7 `IsDisplayingQuestBoard` 用引用相等判定](#37-isdisplayingquestboard-用引用相等判定)
  - [3.8 目标进度只认物品史与对话史](#38-目标进度只认物品史与对话史)

先读目录，再按任务读取相关章节，避免整份指南载入上下文。

权威范围：任务域当前代码与资产的实际行为，含任务状态机与面板显隐、目标进度 `QuestProgressData` 的存储与重算、奖励经事件通道交付、任务板与任务面板的分层、任务日志槽位刷新。背包内部堆叠与记账规则、对话域的会话流程、存档系统的读写实现分别归背包域、对话域与存档域文档，本指南只在任务进度未落档这一事实上引用存档。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `任务`、`Quest`、`QuestManager` | §2.1 状态机与面板 |
| `接取`、`Accept`、`Decline`、`IsToComplete`、`Completed` | §2.1 状态机分支 |
| `QuestProgressData`、`目标进度`、`currentAmount` | §2.2 进度存储 |
| `奖励`、`发奖`、`QuestRewardRequest` | §2.3 奖励通道 |
| `任务板`、`QuestBoardManager`、`任务面板`、`QuestLogPanel` | §2.4 分层 |
| `QuestLogSlot`、`槽位`、`任务日志` | §2.5 视图 |

## 2. 当前实现

### 2.1 状态机与面板（`QuestManager`）

`QuestManager : YSingleton<QuestManager>` 是任务域唯一管理者，只持进度数据与事件资产、不持 UI 引用；任务板界面在 `QuestCanvasView`（预制体 `QuestCanvas.prefab`），由 `UICacheManager` 按需创建。任务板界面通过 `CanOpenBoard` 判断是否可打开，经稳定的 `CurrentState` 对象读取任务列表、当前任务和当前状态；列表通过 `BoardQuestCount` 与 `GetBoardQuest(index)` 按索引查询，不复制集合。视图按脏标记绘制并调用 `State.ClearDirty()`。

状态枚举 `QuestState { Idle, Accepted, Decline, IsToComplete, Completed }` 依次取值 0 到 4，管理器的状态字段初值为 `Idle`。

状态写入口是 `QuestStateChanged(QuestSO, QuestState)`：未注册的任务直接返回；写入进度状态，当前任务同步 `CurrentQuestState`；首次转入 `Completed` 时发奖；调用 `RefreshObjectiveProgress` 重算目标。实际状态变化时标记 `BoardQuests`，当前任务的状态字段驱动详情与按钮刷新。

各状态点亮的 CanvasGroup：

| 状态 | 点亮的 CanvasGroup |
|---|---|
| `Idle` | accept |
| `Accepted` | decline + complete |
| `Decline` | accept + decline |
| `IsToComplete` | decline + complete |
| `Completed` | 无，列表条目置为完成态，首次完成时发奖 |

任务数据入口：

- 选项按钮：`QuestOptionsButton.OnOptionButtonClicked()` 经 `QuestOptionsEventSO` 到 `QuestManager.OnQuestOptionChosen`。切到 `Completed` 前先验证所有目标完成；其余状态直接写入。
- 选择任务：`OpenQuest` 写入 `CurrentQuest` 与 `CurrentQuestState`，重算目标进度，只标记详情相关字段。
- 任务板装载：`OnRefreshQuestState` 登记列表与进度，把目标已完成且状态为 `Accepted` 的任务提升为 `IsToComplete`。

`Decline` 表示该任务可再次接取：面板重新点亮 accept 与 decline，状态迁移只由选项按钮写入。

操作按钮的目标状态在任务面板预制体的 `QuestOptionsButton.questStateToShift` 中配置。

任务板先发装载事件再发打开事件，`QuestManager` 准备好数据后调用缓存打开。`QuestCanvasView.CanOpen()` 检查任务板列表已装载，空列表可显示提示；`OpenOnToggleEvent` 关闭直接打开路径。打开时回调 `OnCachedViewOpened` 并完整绘制，关闭时回调 `OnCachedViewClosed`。ESC、互斥与排序由 `UIManager` 的焦点栈处理，缓存始终接收关闭事件。

`CloseQuestBoard` 请求缓存关闭后，复用 `OnCachedViewClosed` 清理激活状态、任务板与选择。清理是幂等的，视图尚未创建时也能清空已装载数据。任务板用 `IsDisplayingQuestBoard` 做列表引用相等判定，退出范围时关闭对应任务板。

### 2.2 进度存储（`QuestProgressData`）

进度存在 `QuestManager` 的内存字典里。`QuestProgressData` 是 `QuestManager` 的私有嵌套类，成员为 `questState` 与 `Dictionary<QuestObjective,int> questObjectives`，构造函数按目标列表把每个目标初值置 0。所有实例存在 `Dictionary<QuestSO, QuestProgressData> questProgress` 中，惰性注册且不覆盖已有进度。

进度只存在 `questProgress` 中。

进度重算由 `UpdateObjectiveProgress` 实现：物品目标取 `ItemHistoryManager.GetItemQuantity`，角色目标按 `ConversationHistoryManager.HasChatedWith` 判定，地点目标的当前数量为零。`RefreshObjectiveProgress` 在打开面板、选择任务与任务状态变化时逐目标重算，已完成任务保持冻结。

读取接口：`GetCurrentObjAmount` 缺键返回零；`GetProgressText` 完成时返回对勾，否则返回当前数量与需求数量；`IsQuestObjDone` 对已完成状态直接返回真。

### 2.3 奖励通道（事件交付）

`RaiseRewardEvent` 逐条读 `quest.rewards`，调用 `QuestRewardRequest.RaiseInventoryUpdateRequest(item, 0, quantity)`，价格位传 0，数量走第三个参数。`QuestRewardRequest` 是序列化的 `InventorySlotsStatsSO`。

`QuestRewardEvent` 事件资产同时赋给 `InventoryManager` 与 `QuestManager` 的 `QuestRewardRequest` 字段，两侧指向同一资产实例。接收侧 `InventoryManager.HandleQuestReward` 调 `UpdateInventorySlots(item, amount)`；金币类物品累加 `goldAmount` 并调用 `ItemHistoryManager.RecordItem`。

`RaiseRewardEvent` 只在 `QuestStateChanged` 的 `Completed` 分支触发。

任务资产实例：`PickAndChat` 有 2 个目标，集 3 个蘑菇属物品类、与 Purple Bob 对话属角色类，另有 3 条奖励；`DefaultQuest` 的目标与奖励都为空；`Shopping` 是任务配置实例。需求量为 0 的空目标会被判为已完成，`DefaultQuest` 因无目标而 `IsQuestObjDone` 恒真。

### 2.4 分层：任务板与任务面板

任务板 `QuestBoardManager : MonoBehaviour` 挂在任务板预制体 `QuestBoard` 上，带 `CircleCollider2D`：

- 持有 `List<QuestSO> questsOnBoard`，注释说明要区分任务实例需用深拷贝类包装。
- 范围触发：`OnTriggerEnter2D` 与 `OnTriggerExit2D` 按 `"Player"` tag 置 `isInRange`。
- 装载顺序：收到 `toggleQuestEvent(true)` 且 `isInRange` 为真时，先调 `loadQuestEventSO.OnLoadQuestEventRaised(questsOnBoard)` 初始化，再调 `openQuestEvent.OnEventRaised()` 打开面板。
- 离场收尾：`OnTriggerExit2D` 与 `OnDisable` 都调用 `CloseQuestBoardIfShowing()`，只在 `QuestManager.IsDisplayingQuestBoard(questsOnBoard)` 为真时关闭。
- 每块板的清单在场景实例上覆盖：预制体自身是空数组；Scene1 的 3 个板实例中一个覆盖为 `DefaultQuest` 与 `PickAndChat`，Scene2 的 3 个板实例同样带清单覆盖。

任务面板 `QuestLogPanel : MonoBehaviour`：

- 默认选择由 `QuestManager.OnCachedViewOpened` 决定，详情面板只显示状态。
- `HandleQuestClicked` 向管理器提交选择；`currentQuest` 仅记录已绘制任务。`RefreshFromManager` 比较任务引用，切换时整页刷新，同一任务只刷新目标进度。
- `DisplayObjectives` 读取目标数量与进度文本；`DisplayRewards` 显示物品图标与数量；多余槽位隐藏。

`QuestCanvasView` 的详情面板、列表槽位和各组 `CanvasGroup` 均在预制体上序列化接线。`BoardQuests` 脏时刷新列表与空任务提示，选择或当前任务状态脏时刷新详情与操作按钮；完成刷新后调用 `State.ClearDirty()` 清理状态标记。

### 2.5 槽位视图

`QuestLogSlot` 的控件与 `CanvasGroup` 在预制体上接线。`SetQuest` 写名称与等级并激活条目；`SetQuestActive(false)` 隐藏多余条目并清空任务引用；`SetCompletedState` 置灰并禁用交互。列表刷新直接更新有效条目，编辑器预览通过 `OnValidate` 更新文本。

`QuestLogSlot.SetCompletedState()` 把槽位置为 alpha 0.2、interactable 假、blocksRaycasts 假，这是当前唯一的已完成不可再点保护。

`QuestObjectiveSlot.RefreshObjectives` 写说明与进度文本，完成时整体转灰；`QuestRewardsSlot.DisplayReward` 写图标与数量。

## 3. 约定与硬边界

### 3.1 进度只活在 `questProgress` 字典里

`QuestObjective.currentAmount` 是资产上的零读写字段，改进度须改运行时数据。

### 3.2 进度按业务入口重算

打开面板、选择任务与切换任务状态时读取物品史和对话史，重算目标进度。

### 3.3 奖励只有事件通道

`QuestManager` 与背包之间没有直接引用，改奖励发放要改 `InventorySlotsStatsSO` 的订阅侧。两个管理器需指向同一资产实例，否则事件静默不达；当前常驻场景已指向同一资产。

### 3.4 完成奖励按状态迁移发放

`QuestStateChanged` 只在任务从其他状态转入 `Completed` 时发奖。完成态在视图中关闭操作按钮并禁用任务条目。

### 3.5 进入 `Completed` 的唯一数据前提是 `IsQuestObjDone`

该判定对状态已是 `Completed` 的任务直接返回真。

### 3.6 任务板需要先装载再开面板

`CanOpenBoard` 以任务板列表已装载为前提；`QuestBoardManager` 先发布装载事件，再请求打开。

### 3.7 `IsDisplayingQuestBoard` 用引用相等判定

`questsOnBoard` 是各板实例自己的 List 对象，这也是哪块板开的、哪块板负责关的依据。

### 3.8 目标进度只认物品史与对话史

`targetLocation` 没有实现分支；角色类目标一旦对话过就直接记满。
