# Gameplay.Quest 权威指南（任务域当前实现）

文档 ID：`GP-QUEST-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责"任务域当前代码与资产的实际行为"——任务状态机与面板显隐、目标进度（`QuestProgressData`）的存储与重算、奖励经事件通道交付、任务板与任务面板的分层、任务日志槽位刷新。不负责背包内部堆叠与记账规则、不负责对话域的会话流程、不负责存档系统的读写实现（仅在"进度未落档"这一事实上引用）。
上游来源：`Assets/Scripts/Gameplay/Quest/*.cs`、`Assets/Scripts/Pipeline/SO/QuestSO.cs`、`Assets/Scripts/Pipeline/SO/Events/{LoadQuestEventSO,QuestOptionsEventSO,InventorySlotsStatsSO}.cs`、`Assets/Scripts/Pipeline/UI/Buttons/QuestOptionsButton.cs`、`Assets/Scripts/Gameplay/Inventory/InventoryManager.cs`、`Assets/GameSO/UI SO/QuestSO/*.asset`、`Assets/Prefabs/Grid/QuestBoard.prefab`、旧文档 `Docs/My_ARPG_MVCS项目现状.md`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `任务`、`Quest`、`QuestManager` | §2.1 状态机与面板 |
| `接取`、`Accept`、`Decline`、`IsToComplete`、`Completed` | §2.1 状态机分支 |
| `QuestProgressData`、`目标进度`、`currentAmount` | §2.2 进度存储 |
| `奖励`、`发奖`、`QuestRewardRequest` | §2.3 奖励通道 |
| `任务板`、`QuestBoardManager`、`任务面板`、`QuestLogPanel` | §2.4 分层 |
| `QuestLogSlot`、`槽位`、`任务日志` | §2.5 视图 |
| `目标不更新`、`进度不刷新`、`重复发奖` | §4 已知缺陷 |

## 2. 当前实现

### 2.1 状态机与面板（`QuestManager`）

`QuestManager : YSingleton<QuestManager>, ICanvasManager`（`Assets/Scripts/Gameplay/Quest/QuestManager.cs:6`）是任务域唯一管理者，同时持有 UI 引用、事件资产与进度数据（`:8-40`）。

状态枚举 `MyEnums.QuestState { Idle, Accepted, Decline, IsToComplete, Completed }`（`Assets/Scripts/Contracts/MyEnums.cs:47-54`，即 0/1/2/3/4）。管理器的字段初值为 `Idle`（`QuestManager.cs:42`）。

写入口是 `QuestStateChanged(QuestSO, QuestState)`（`:242-282`），流程固定为：①未注册的任务直接 return（`:244`）→ ②先关掉 accept/decline/complete 三个 CanvasGroup（`:246-248`）→ ③写入 `questProgress[quest].questState`（`:250-252`）→ ④按状态点亮按钮组（`:255-278`）→ ⑤`RefreshObjectiveProgress(quest)`（`:280`）→ ⑥`questLogPanel.DisPlayObjectives()`（`:281`）。

各状态的面板表现（`:255-278`）：

| 状态 | 点亮的 CanvasGroup |
|---|---|
| `Idle` | accept |
| `Accepted` | decline + complete |
| `Decline` | accept + decline |
| `IsToComplete` | decline + complete |
| `Completed` | 无（另触发 `SetQuestSlotToDoneState` + `RaiseRewardEvent`，`:274-278`） |

外部写入路径有三条：
- 选项按钮：`QuestOptionsButton.OnOptionButtonClicked()` → `QuestOptionsEventSO.OnQuestOptionsEventRaised(questStateToShift)`（`Assets/Scripts/Pipeline/UI/Buttons/QuestOptionsButton.cs:9-12`）→ `QuestManager.OnQuestOptionChose`（`:121-132`）。切到 `Completed` 前会先验 `IsQuestObjDone(currentQuest)`，否则只打日志 `"Quest Not Done"`（`:123-130`）；其它状态一律直接写入。
- 打开任务：`OpenQuest(quest)` → `SetCurrentQuest` + 按当前进度重放一次 `QuestStateChanged`（`:55-60`）。
- 任务板装载：`OnReFreshQuestState` 中把"目标已完成且状态为 `Accepted`"的任务自动提升为 `IsToComplete`（`:170-176`）。

**Decline 的回退语义**：`Decline` 只是"回到可再次接取"的状态——它重新点亮 accept + decline（`:264-268`），代码中不存在从 `Decline` 自动回到 `Idle` 的路径，也不存在 `Idle ↔ Decline` 之外的隐式迁移。

`Decline` / `IsToComplete` / `Completed` 的具体取值由场景里的三个 `QuestOptionsButton` 实例决定：`questStateToShift` 分别被覆盖为 `1`(Accepted)、`2`(Decline)、`4`(Completed)（`Assets/Scenes/GameScene/PersistentScene.unity:34170`、`:12117`、`:8288`；预制体默认值是 0，见 `Assets/Prefabs/UI/Buttons/QuestOptionsButton.prefab:296`）。没有任何按钮写入 `0`(Idle) 或 `3`(IsToComplete)。

面板开关与焦点：`ToggleCanvasEvent => toggleQuestEvent`（`:18`）、`SceneLoadedEvent => sceneLoadedEvent`（`:19`）；`OnToggleQuest` **刻意忽略 true 态**（`:104-113`，开启只认任务板的 `openQuestEventSO`），false 态则 `CloseQuestBoard()`；`OnFocus` 在面板已开时刷新排序（`:115-119`）；切场景 `CloseQuestBoard()`（`:99-102`）；`ToggleCanvas` 走 `ICanvasManager` 默认实现（`:392-396`）。注意 `SetCanvaState`（`:398-403`）是私有同名方法，只改 CanvasGroup 三件套、**不上报 UIManager**，用于 accept/decline/complete/details/prompt 这些子面板。

`CloseQuestBoard()` 会置 `currentBoardLoadQuests = null` 且 `currentQuest = null`（`:142-147`）；任务板用 `IsDisplayingQuestBoard(quests)` 做引用相等判定，避免关掉别人的面板（`:149-152`）。

### 2.2 进度存储（`QuestProgressData`）

进度**不落在 SO 上**。`QuestProgressData` 是 `QuestManager` 的**私有嵌套类**（`QuestManager.cs:64-76`），只有两个成员：`questState` 与 `Dictionary<QuestObjective,int> questObjectives`（`:74-75`）；构造函数按目标列表把每个目标初值置 0（`:66-72`）。所有实例存在 `Dictionary<QuestSO, QuestProgressData> questProgress`（`:45`），惰性注册、不覆盖已有进度（`:195-204`）。

`QuestSO.QuestObjective.currentAmount` 字段存在于资产上（`Assets/Scripts/Pipeline/SO/QuestSO.cs:32`），但全工程无任何代码读写它——代码只读写 `questProgress`（`:287-299`、`:329-335`）。该字段性质在 `QuestSO.cs:64-67`、`:85-86` 的注释中被明确写为"遗留字段"。

进度重算（唯一实现）：`UpdateObjectiveProgress`（`:285-300`）按目标类型取值——`targetItem != null` 时取 `ItemHistoryManager.Instance.GetItemQuantity(targetItem)`（`:290-293`）；否则当 `targetCharacter != null && ConversationHistoryManager.Instance.HasChatedWith(targetCharacter)` 时直接记满 `requiredAmount`（`:294-297`）。`targetLocation` **没有任何分支**，位置类目标进度恒为 0。`RefreshObjectiveProgress`（`:303-312`）在任务已 `Completed` 时整体跳过（`:306`），否则逐目标调用；它只被 `QuestStateChanged` 调用（`:280`），**没有物品拾取/对话结束事件驱动它**。

读取接口：`GetCurrentObjAmount`（`:329-335`，缺键返回 0）、`GetProgressText`（`:314-327`，完成返回 `√`，否则 `"{当前}/{需求}"`）、`IsObjDone`（`:384-390`，`当前 < 需求` 即未完成）、`IsQuestObjDone`（`:366-381`，已完成状态直接 true）。

### 2.3 奖励通道（事件交付，不直接引用背包）

`RaiseRewardEvent`（`:179-187`）逐条读 `quest.rewards`，调用 `QuestRewardRequest.RaiseInventoryUpdateRequest(item, 0, quantity)`——`price` 位传 0，数量走第三个参数。`QuestRewardRequest` 是序列化的 `InventorySlotsStatsSO`（`:22-23`，事件签名见 `Assets/Scripts/Pipeline/SO/Events/InventorySlotsStatsSO.cs:8-19`）。

连接验证：`QuestRewardEvent.asset`（guid `42a47779…`）被**同时**赋给 `InventoryManager.QuestRewardRequest`（`Assets/Scenes/GameScene/PersistentScene.unity:524`，组件脚本 guid `5ae0da3a…`）与 `QuestManager.QuestRewardRequest`（同文件 `:18734`，组件脚本 guid `812a7559…`）。接收侧 `InventoryManager.HandleQuestReward` → `UpdateInventorySlots(item, amount)`（`Assets/Scripts/Gameplay/Inventory/InventoryManager.cs:55`、`:68-71`）；金币类物品在 `:99-107` 累加 `goldAmount` 并调 `ItemHistoryManager.RecordItem`。

`RaiseRewardEvent` 只在 `QuestStateChanged` 的 `Completed` 分支触发（`:274-278`），**没有任何"已发奖"幂等标记**。

任务资产实例：`PickAndChat`（2 个目标：集 3 个蘑菇[物品] / 与 Purple Bob 对话[角色]；3 条奖励，`Assets/GameSO/UI SO/QuestSO/PickAndChat.asset:18-37`）、`DefaultQuest`（`questObjectives: []`、`rewards: []`，`:18-19`）、`Shopping`。注意 `requiredAmount` 为 0 的空目标会被判为"已完成"（`IsObjDone` 逻辑），`DefaultQuest` 因无目标而 `IsQuestObjDone` 恒真。

### 2.4 分层：任务板（范围触发 + 装载）与任务面板（状态 + 画布）

**任务板** `QuestBoardManager : MonoBehaviour`（`Assets/Scripts/Gameplay/Quest/QuestBoardManager.cs:6`，挂在 `Assets/Prefabs/Grid/QuestBoard.prefab:47`，带 `CircleCollider2D` `:55`）：

- 持有 `List<QuestSO> questsOnBoard`（`:9`，注释自述"如果要区分任务实例，则需要深拷贝类包装"）。
- 范围触发：`OnTriggerEnter2D/Exit2D` 按 `"Player"` tag 置 `isInRange`（`:36-50`）。
- 装载顺序：收到 `toggleQuestEvent(true)` 且 `isInRange` 时，**先** `loadQuestEventSO.OnLoadQuestEventRaised(questsOnBoard)` 初始化，**再** `openQuestEvent.OnEventRaised()` 打开面板（`:28-35`）。
- 离场收尾：`OnTriggerExit2D` 与 `OnDisable` 都调 `CloseQuestBoardIfShowing()`，只在 `QuestManager.IsDisplayingQuestBoard(questsOnBoard)` 为真时关闭（`:52-59`）。
- 每块板的清单在场景实例上覆盖：预制体自身是空数组（`Assets/Prefabs/Grid/QuestBoard.prefab:50`），`Scene1.unity:8988-8998` 覆盖为 `DefaultQuest` + `PickAndChat`；`Scene1`/`Scene2` 各有 3 个板实例带覆盖（`Scene1.unity:8988/10075/10855`、`Scene2.unity:943/1975/2734`）。

**任务面板** `QuestLogPanel : MonoBehaviour`（`Assets/Scripts/Gameplay/Quest/QuestLogPanel.cs:7`，注释"UI更新有关逻辑"）：

- 订阅 `openQuestEvent`，`ShowQuestOffer()` 取 `GetFirstIncompletedQuest()`（`:28-34`，`QuestManager.cs:226-235` 遍历 `currentBoardLoadQuests` 找第一个非 `Completed`）后走 `HandleQuestClicked`。
- `HandleQuestClicked(quest)`：记 `currentQuest`、调 `QuestManager.OpenQuest(quest)`、写标题/描述、`DisplayRewards()`（`:36-44`）。
- `DisPlayObjectives()`（`:46-68`）逐槽取 `GetCurrentObjAmount` + `GetProgressText`，超出数量的槽位 `SetActive(false)`；`DisplayRewards()`（`:70-86`）用 `reward.rewardItem.icon` + 数量。

`QuestManager` 的 `questLogPanel`、`questLogSlots`、accept/decline/complete 三组 CanvasGroup 与主画布都在 `PersistentScene` 同一组件上接线（`PersistentScene.unity:18727-18747`，其中 `questLogSlots` 6 个槽 `:18738-18744`）。

### 2.5 槽位视图

`QuestLogSlot : MonoBehaviour`（`Assets/Scripts/Gameplay/Quest/QuestLogSlot.cs:8`）：`Awake` 缓存 `CanvasGroup` 并复位（`:20-24`），`SlotCanvas` 对外暴露（`:18`）；`SetQuest` 写名字与 `"Lv."+lv`、复位后激活（`:34-42`）；`SetQuestActive(false)` 会清 `currentQuest`（`:43-52`）；`ResetSlotState` 把 alpha/interactable/blocksRaycasts 全置真（`:54-66`）；点击转给面板 `HandleQuestClicked`（`:68-71`）。`QuestManager.InitiateQuestSlots` 先全量 `SetQuestActive(false)`，再按数量 `SetQuest`，最后给已完成任务套 `SetQuestSlotToDoneState`（`:206-224`）。`OnValidate` 会在编辑器里 `SetActive(false)`（`:26-32`）。

`SetQuestSlotToDoneState`（`:351-364`）把 `currentQuest == quest` 的槽位设为 `alpha=0.2 / interactable=false / blocksRaycasts=false`——这是目前唯一的"已完成不可再点"保护。

`QuestObjectiveSlot.RefreshObjectives` 写说明与进度文本，完成时整体转灰（`Assets/Scripts/Gameplay/Quest/QuestObjectiveSlot.cs:8-16`）；`QuestRewardsSlot.DisplayReward` 写图标与数量（`.../QuestRewardsSlot.cs:9-13`）。

## 3. 约定与硬边界

1. **进度只活在 `questProgress` 字典里**（`QuestManager.cs:45`、`:64-76`），`QuestObjective.currentAmount` 是死字段。任何"改 SO 上的 currentAmount 就能改进度"的假设都不成立。
2. **进度的重算时机只有 `QuestStateChanged`**（`:280`）。拾取物品、结束对话都不会触发刷新；面板关闭期间进度是陈旧值，只有下次打开/切换状态时才重算。
3. **奖励只有事件通道**：`QuestManager` 不持有背包引用，改奖励发放必须改 `InventorySlotsStatsSO` 的订阅侧（`InventoryManager.cs:55`）。若两个管理器指向不同资产实例，事件静默不达（当前场景已验证同资产，见 §2.3）。
4. **`Completed` 无幂等保护**：任何重复进入 `Completed` 分支的调用都会再发一次奖励（`:274-278`）。当前只靠 UI 拦（完成后 3 个按钮组全灭 `:246-248`，槽位 `interactable=false` `:359-361`）。
5. **进入 `Completed` 的唯一数据前提是 `IsQuestObjDone`**（`:123-130`），但 `IsQuestObjDone` 对"状态已是 `Completed`"直接返回 true（`:371-372`），所以它不能当作"未发过奖"的判据。
6. **任务板必须先装载再开面板**（`QuestBoardManager.cs:32-33`）。少了 `OnLoadQuestEventRaised`，`currentBoardLoadQuests` 为 null，`GetFirstIncompletedQuest` 会抛 `NullReferenceException`（`QuestManager.cs:228`）。
7. **`IsDisplayingQuestBoard` 是引用相等**（`:151`），`questsOnBoard` 是各板实例自己的 List 对象——这也是"哪块板开的、哪块板负责关"的判定依据。
8. **目标进度只认物品史与对话史**（`:285-300`）：`targetLocation` 无实现；角色类目标一旦对话过就直接记满，与击杀/护送等语义无关。

## 4. 已知缺陷与风险

1. **重复发奖无数据层保护**：`QuestStateChanged(quest, Completed)` 每次都会 `RaiseRewardEvent`（`:274-278`）；`OpenQuest` 也会重放当前状态（`:55-60`），一旦"已完成任务仍可被打开"（例如槽位 CanvasGroup 被 `ResetSlotState` 复位），奖励会重复发放。
2. **`questLogPanel.DisPlayObjectives()` 无空值保护**（`:281`），`SetCanvaState` 对 accept/decline/complete/details/prompt 五组也无保护（`:246-248`、`:190-193`）；未接线即 NRE。面板内 `DisPlayObjectives` 同样不判 `currentQuest == null`（`QuestLogPanel.cs:50-54`），而它在 `OnReFreshQuestState` 的自动提升分支（`:172-175`）里就可能被调用到——面板实例尚未点过任何任务时是崩溃路径。
3. **面板与管理器各存一份 `currentQuest`**：`CloseQuestBoard` 只清管理器那份（`:146`），`QuestLogPanel.currentQuest` 不清（`QuestLogPanel.cs:16`、`:38`），关闭再打开时面板可能短暂显示上一个任务的数据。
4. **`GetQuestStateFromProgress`/`IsAllQuestsCompleted` 用字典索引器**（`:239`、`:342`）：传入未注册的 `QuestSO` 会抛 `KeyNotFoundException`（`QuestStateChanged` 自己有 `ContainsKey` 守卫 `:244`，但这两个读取接口没有）。
5. **`currentAmount` 遗留字段**：资产里仍在序列化（如 `PickAndChat.asset:24`、`:30`），代码零读写（`QuestSO.cs:32` 为唯一定义处），易被误认为进度来源——`QuestSO.cs:85-86` 已把它标为遗留。
6. **任务进度未接入存档**：`questProgress` 是纯内存字典，`Assets/Scripts/Gameplay/Save/` 下无任务域引用（全库 grep `questProgress` 仅命中 `QuestManager.cs`）。读档后任务状态与进度丢失。
7. **`QuestManager` 单类承担四种职责**（面板显隐、状态机、发奖、任务数据），与旧文档 `Docs/My_ARPG_MVCS项目现状.md:107`、`:114`、`:116` 描述的拆分方案（`QuestProgressModel`/`QuestRuntimeModel`/`QuestService`/`QuestLogView`）相比，方案**未实施**，当前仍是单类。
8. `QuestBoardManager.cs:4` 引入 `using Unity.VisualScripting;` 但未使用；`QuestLogPanel.cs:4` 同样（无害，仅噪声）。

## 5. 未核验事项

- 假设：`QuestStateChanged` 在 `Completed` 时的重复发奖在现网资产里不可达（靠槽位 `interactable=false` 拦住，未运行 Unity 验证点击路径）。
- 假设：`DetailsCanvaGroup` / `promptCanvaGroup` 的"无任务时显示白板提示"是预期表现（`SetNoQuestsState`，`:189-193`），无设计文档背书。
- 假设：旧文档提到的"任务面板只读回归：打开任务面板不改变任务进度"（`Docs/My_ARPG_MVCS项目现状.md:194`）仍是当前达成的目标——代码上打开面板会重算进度并重放状态，未运行验证是否有可观察差异。
- 假设：`targetLocation` 类目标当前没有任何线上配置（未逐个检查 QuestSO 资产的 `targetLocation` 字段）。
- 假设：六个 `questLogSlots` 与三个选项按钮的实际交互（点击哪个槽位、按钮组显隐）符合 §2.1 描述（只核对了 Inspector 接线数据，未运行 Unity 验证）。
- 假设：`Scene1`/`Scene2` 中 6 块任务板的 `questsOnBoard` 覆盖都是有意配置（仅核对了数组值与 guid 映射，未验证关卡设计意图）。
