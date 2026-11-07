# Gameplay.Quest Developer Log

## 维护规则

按项目约定记录带日期的任务事实、验证证据与维护计数，现行结论同步到对应 Guide。了解现状优先读取 Guide；需要任务历史时，只定位并读取最近一天或两天的相关条目，禁止整份日志载入上下文。只有任务需要追溯历史时才按日期、标题或关键词定向读取更早条目。读取范围不改变存储保留，删除具体历史条目须经用户明确确认。纯文档维护不增加业务计数。

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-07 任务状态提供分组版本

列表、选择和当前状态分别提供版本，`QuestCanvasView` 以本地读取记录决定列表与详情绘制，绘制后保留捕获版本。回归验证选择保持列表版本、完成任务推进列表版本、重复关闭保持三个版本。现有 Unity 实例内 EditMode 全套 80/80 通过，项目用例 79 项，XML 为 `Logs/ui-version-editmode.xml`。维护计数 `0/5 → 1/5`。

## 2026-11-01 任务视图直接读取 State

`QuestCanvasView` 通过任务数量与索引查询读取列表、当前任务和状态，绘制后清理 `State` 脏标记；不创建刷新快照或复制任务列表。维护计数达到 `5/5`，复核实现并更新 Guide 后清零为 `0/5`。

## 2026-11-01 任务面板按字段读取快照

`QuestManager` 将任务列表、当前任务与当前状态保留在内部状态中；视图消费只读快照，并按绘制字段与版本确认刷新。维护计数更新为 `3/5 -> 4/5`。

## 2026-10-31 任务面板迁入缓存预制体

- `QuestManager` 不再实现 `ICanvasManager`，去掉 `questLogPanel`、`questLogSlots` 与三组 `CanvasGroup` 引用；`QuestStateChanged` 只写状态、发奖、重算并 `MarkDirty()`。
- 新增 `QuestCanvasView`：`CanOpen` 用 `CanOpenBoard` 拒绝空任务板，`RefreshIfDirty` 负责槽位填充、五个 `CanvasGroup` 显隐与 `QuestLogPanel` 刷新。
- `SetQuestSlotToDoneState` 收敛为 `QuestLogSlot.SetCompletedState()`。

## 2026-10-05 建立 Gameplay.Quest 权威文档

**写入的文件**
- `Quest_Guide.md`（新建，ID `GP-QUEST-GUIDE`，状态 Active）
- `Router.md`（重写为项目中文模板，维护计数 `0/5`）
- 本 DeveloperLog

**依据的证据路径**
- 代码：`QuestManager`、`QuestBoardManager`、`QuestLogPanel`、`QuestLogSlot`、`QuestObjectiveSlot`、`QuestRewardsSlot`、`QuestSO`、`LoadQuestEventSO`、`QuestOptionsEventSO`、`InventorySlotsStatsSO`、`QuestOptionsButton`、`MyEnums`
- 资产：QuestSO 目录下的 PickAndChat、DefaultQuest、Shopping 三份任务配置，以及 `QuestRewardEvent`、QuestBoard 预制体、QuestOptionsButton 预制体。
- 接线：PersistentScene 场景的 QuestManager 组件、奖励资产的双引用与三个状态按钮覆盖；Scene1 与 Scene2 各 6 块任务板的 questsOnBoard 覆盖。

**已核验**
- 状态机各分支与按钮组显隐（Idle/Accepted/Decline/IsToComplete/Completed）、`Decline` 只回到可再次接取、`IsToComplete` 仅由自动提升产生（无按钮写入）、`QuestProgressData` 为私有嵌套类且 `QuestObjective.currentAmount` 全工程零读写、奖励经同一 `InventorySlotsStatsSO` 资产由 QuestManager 发送、InventoryManager 接收（场景 guid 双向核对）、任务板"先装载后开面板"与离场关闭的引用相等判定、面板/槽位刷新路径。
