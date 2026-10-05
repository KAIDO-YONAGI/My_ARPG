# Gameplay.Quest Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

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
