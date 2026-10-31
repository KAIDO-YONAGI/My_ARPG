# Gameplay.Quest 权威指南（任务域当前实现）

文档 ID：`GP-QUEST-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
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

`QuestManager : YSingleton<QuestManager>` 是任务域唯一管理者，只持进度数据与事件资产、不持 UI 引用；任务板界面在 `QuestCanvasView`（预制体 `QuestCanvas.prefab`），由 `UICacheManager` 按需创建。对外暴露 `CanOpenBoard`、`CurrentBoardQuests`、`CurrentQuest`、`CurrentQuestState` 与 `State`，视图按脏标记拉取。

状态枚举 `QuestState { Idle, Accepted, Decline, IsToComplete, Completed }` 依次取值 0 到 4，管理器的状态字段初值为 `Idle`。

写入口是 `QuestStateChanged(QuestSO, QuestState)`，流程固定为：未注册的任务直接返回；写入 `questProgress[quest].questState`，当前任务同步 `currentQuestState`；由非完成态首次转 `Completed` 时发奖 `RaiseRewardEvent`；调用 `RefreshObjectiveProgress(quest)` 重算目标；最后 `MarkDirty()` 把 `State` 的各项标记为脏，按钮组与槽位由视图刷新。

各状态点亮的 CanvasGroup：

| 状态 | 点亮的 CanvasGroup |
|---|---|
| `Idle` | accept |
| `Accepted` | decline + complete |
| `Decline` | accept + decline |
| `IsToComplete` | decline + complete |
| `Completed` | 无，另触发 `SetQuestSlotToDoneState` 与 `RaiseRewardEvent` |

外部写入路径三条：

- 选项按钮：`QuestOptionsButton.OnOptionButtonClicked()` 经 `QuestOptionsEventSO.OnQuestOptionsEventRaised(questStateToShift)` 走到 `QuestManager.OnQuestOptionChose`。切到 `Completed` 前先验 `IsQuestObjDone(currentQuest)`，未完成时只打日志 `"Quest Not Done"`；其余状态直接写入。
- 打开任务：`OpenQuest(quest)` 先 `SetCurrentQuest`，再按当前进度重放一次 `QuestStateChanged`。
- 任务板装载：`OnReFreshQuestState` 把目标已完成且状态为 `Accepted` 的任务自动提升为 `IsToComplete`。

`Decline` 表示该任务可再次接取：面板重新点亮 accept 与 decline，状态迁移只由选项按钮写入。

`Decline`、`IsToComplete`、`Completed` 的具体取值由常驻场景里的三个 `QuestOptionsButton` 实例决定，`questStateToShift` 分别覆盖为 1、2、4；`QuestOptionsButton` 预制体的默认值是 0。没有任何按钮写入 0 或 3。

面板开关与焦点：`QuestCanvasView.CanOpen()` 用 `CanOpenBoard` 拒绝无任务可展示时的打开；打开时调 `OnCachedViewOpened()`、关闭时调 `OnCachedViewClosed()`；`QuestManager` 自己仍订阅 `openQuestEventSO` 与 `sceneLoadedEvent`，切场景调用 `CloseQuestBoard()`。ESC、互斥与排序由 `UIManager` 的焦点栈统一处理。

`CloseQuestBoard()` 会把 `currentBoardLoadQuests` 与 `currentQuest` 置空；任务板用 `IsDisplayingQuestBoard(quests)` 做引用相等判定，避免关掉别人的面板。

### 2.2 进度存储（`QuestProgressData`）

进度存在 `QuestManager` 的内存字典里。`QuestProgressData` 是 `QuestManager` 的私有嵌套类，成员为 `questState` 与 `Dictionary<QuestObjective,int> questObjectives`，构造函数按目标列表把每个目标初值置 0。所有实例存在 `Dictionary<QuestSO, QuestProgressData> questProgress` 中，惰性注册且不覆盖已有进度。

进度只存在 `questProgress` 中。

进度重算只有 `UpdateObjectiveProgress` 一处实现，按目标类型取值：`targetItem` 非空时取 `ItemHistoryManager.Instance.GetItemQuantity(targetItem)`；否则当 `targetCharacter` 非空且 `ConversationHistoryManager.Instance.HasChatedWith(targetCharacter)` 为真时直接记满 `requiredAmount`。`targetLocation` 没有分支，位置类目标进度恒为 0。`RefreshObjectiveProgress` 在任务已 `Completed` 时整体跳过，否则逐目标调用；它只被 `QuestStateChanged` 调用，物品拾取与对话结束都不驱动它。

读取接口：`GetCurrentObjAmount` 缺键返回 0；`GetProgressText` 完成时返回 `√`，否则返回当前值与需求值的组合文本；`IsObjDone` 在当前值小于需求值时为未完成；`IsQuestObjDone` 对已完成状态直接返回真。

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

- 订阅 `openQuestEvent`；`ShowQuestOffer()` 用 `GetFirstIncompletedQuest()` 遍历 `currentBoardLoadQuests` 取第一个非 `Completed` 的任务，再走 `HandleQuestClicked`。
- `HandleQuestClicked(quest)`：记 `currentQuest`、调 `QuestManager.OpenQuest(quest)`、写标题与描述、调 `DisplayRewards()`。
- `DisPlayObjectives()` 逐槽取 `GetCurrentObjAmount` 与 `GetProgressText`，超出数量的槽位 `SetActive(false)`；`DisplayRewards()` 用 `reward.rewardItem.icon` 与数量。

`QuestCanvasView` 在 `Awake` 里用 `GetComponentInChildren` / `GetComponentsInChildren` 抓取 `questLogPanel`、全部 `QuestLogSlot` 与五个 `CanvasGroup`（按名字 `Accept`、`Decline`、`Complete`、`QuestDetails`、`NoQuestsText` 匹配），接线全在预制体内部。

### 2.5 槽位视图

`QuestLogSlot : MonoBehaviour`：`Awake` 缓存 `CanvasGroup` 并复位，`SlotCanvas` 对外暴露；`SetQuest` 写名字与等级文本，复位后激活；`SetQuestActive(false)` 会清 `currentQuest`；`ResetSlotState` 把 alpha、interactable、blocksRaycasts 全置真。槽位填充搬到视图：`QuestCanvasView.RefreshIfDirty` 先全量 `SetQuestActive(false)`，再按数量 `SetQuest`，已完成的任务调 `SetCompletedState()`。`OnValidate` 在编辑器里执行 `SetActive(false)`。

`QuestLogSlot.SetCompletedState()` 把槽位置为 alpha 0.2、interactable 假、blocksRaycasts 假，这是当前唯一的已完成不可再点保护。

`QuestObjectiveSlot.RefreshObjectives` 写说明与进度文本，完成时整体转灰；`QuestRewardsSlot.DisplayReward` 写图标与数量。

## 3. 约定与硬边界

### 3.1 进度只活在 `questProgress` 字典里

`QuestObjective.currentAmount` 是资产上的零读写字段，改进度须改运行时数据。

### 3.2 进度重算的时机只有 `QuestStateChanged`

拾取物品与结束对话都不触发刷新，面板关闭期间显示的是上次重算的陈旧值，下次打开或切换状态时才重算。

### 3.3 奖励只有事件通道

`QuestManager` 与背包之间没有直接引用，改奖励发放要改 `InventorySlotsStatsSO` 的订阅侧。两个管理器需指向同一资产实例，否则事件静默不达；当前常驻场景已指向同一资产。

### 3.4 `Completed` 没有幂等保护

任何重复进入 `Completed` 分支的调用都会再发一次奖励。当前拦截只在 UI 层，完成后三组按钮全部关闭，槽位 `interactable` 置假。

### 3.5 进入 `Completed` 的唯一数据前提是 `IsQuestObjDone`

该判定对状态已是 `Completed` 的任务直接返回真。

### 3.6 任务板需要先装载再开面板

少了 `OnLoadQuestEventRaised`，`currentBoardLoadQuests` 为 null，`GetFirstIncompletedQuest` 会抛 `NullReferenceException`。

### 3.7 `IsDisplayingQuestBoard` 用引用相等判定

`questsOnBoard` 是各板实例自己的 List 对象，这也是哪块板开的、哪块板负责关的依据。

### 3.8 目标进度只认物品史与对话史

`targetLocation` 没有实现分支；角色类目标一旦对话过就直接记满。
