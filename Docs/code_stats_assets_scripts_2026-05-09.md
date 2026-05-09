# My_ARPG 代码量统计（2026-05-09）

## 统计口径
- 范围：`Assets/Scripts/**/*.cs`
- 模块定义：`Assets/Scripts` 下一级目录视为模块；位于 `Assets/Scripts` 根目录的脚本归为 `Root`
- 代码量口径：按文件物理行数（LOC）统计
- 未纳入模块总量：`Assets/TextMesh Pro/Examples & Extras/Scripts` 下第三方示例脚本，共 `34` 文件、`4939` 行

## 总览
- 项目主代码合计：`100` 文件、`6785` 行
- 若把第三方 TMP 示例脚本一并计入：`134` 文件、`11724` 行

| 模块 | 文件数 | 行数 | 占比 |
| --- | ---: | ---: | ---: |
| UI | 38 | 2960 | 43.6% |
| Units | 9 | 754 | 11.1% |
| A Star | 5 | 664 | 9.8% |
| Player | 9 | 629 | 9.3% |
| Inventory | 4 | 486 | 7.2% |
| SaveAndLoad | 6 | 448 | 6.6% |
| ScriptableObjects | 20 | 367 | 5.4% |
| Scene | 5 | 335 | 4.9% |
| Root | 2 | 90 | 1.3% |
| Grid | 2 | 52 | 0.8% |

## 模块明细

### UI（38 文件 / 2960 行）

| 行数 | 文件 |
| ---: | --- |
| 377 | `Assets/Scripts/UI/CanvasManagers/QuestManager.cs` |
| 333 | `Assets/Scripts/UI/DialogScripts/DialogManager.cs` |
| 308 | `Assets/Scripts/UI/CanvasManagers/UIManager.cs` |
| 197 | `Assets/Scripts/UI/CanvasManagers/SaveCanvasPanelManager.cs` |
| 161 | `Assets/Scripts/UI/CanvasManagers/ShopManager.cs` |
| 156 | `Assets/Scripts/UI/CanvasManagers/IntegretedUICanvasManager.cs` |
| 96 | `Assets/Scripts/UI/QuestLog/QuestLogUI.cs` |
| 91 | `Assets/Scripts/UI/SkillsTree/SkillSlot.cs` |
| 84 | `Assets/Scripts/UI/Shop/SubShopToggler.cs` |
| 80 | `Assets/Scripts/UI/ScrollbarFix.cs` |
| 68 | `Assets/Scripts/UI/QuestLog/QuestLogSlot.cs` |
| 68 | `Assets/Scripts/UI/Shop/ShopInfo.cs` |
| 63 | `Assets/Scripts/UI/CanvasManagers/ExpManager.cs` |
| 61 | `Assets/Scripts/UI/CanvasManagers/StatsCanvasManager.cs` |
| 60 | `Assets/Scripts/UI/QuestLog/QuestBoardManager.cs` |
| 60 | `Assets/Scripts/UI/Shop/ShopSlot.cs` |
| 59 | `Assets/Scripts/UI/SkillsTree/SkillTreeManager.cs` |
| 52 | `Assets/Scripts/UI/Shop/ShopPortraitCameraController.cs` |
| 51 | `Assets/Scripts/UI/CanvasManagers/ICanvasManager.cs` |
| 49 | `Assets/Scripts/UI/Joystick.cs` |
| 44 | `Assets/Scripts/UI/DialogScripts/HistoryManager/ItemHistoryManager.cs` |
| 38 | `Assets/Scripts/UI/UIDrag.cs` |
| 37 | `Assets/Scripts/UI/CanvasManagers/ESCMenuManager.cs` |
| 36 | `Assets/Scripts/UI/DialogScripts/HistoryManager/ConversationHistoryManager.cs` |
| 36 | `Assets/Scripts/UI/SkillsTree/SkillManager.cs` |
| 31 | `Assets/Scripts/UI/Buttons/ButtonSceneToggler.cs` |
| 30 | `Assets/Scripts/UI/Buttons/ESCButton.cs` |
| 30 | `Assets/Scripts/UI/CanvasManagers/BackpackCanvasManager.cs` |
| 28 | `Assets/Scripts/UI/CanvasManagers/ToggleSkillTree.cs` |
| 28 | `Assets/Scripts/UI/Menu/OpenTxtWithSystem.cs` |
| 25 | `Assets/Scripts/UI/Buttons/ContinueButton.cs` |
| 25 | `Assets/Scripts/UI/Buttons/OpenSaveLoadCanvasButton.cs` |
| 25 | `Assets/Scripts/UI/CanvasManagers/HealthCanvasManager.cs` |
| 17 | `Assets/Scripts/UI/QuestLog/QuestObjectiveSlot.cs` |
| 15 | `Assets/Scripts/UI/Menu/ExitManager.cs` |
| 14 | `Assets/Scripts/UI/DialogScripts/HistoryManager/VisitedHistoryManager.cs` |
| 14 | `Assets/Scripts/UI/QuestLog/QuestRewardsSlot.cs` |
| 13 | `Assets/Scripts/UI/Buttons/QuestOptionsButton.cs` |

### Units（9 文件 / 754 行）

| 行数 | 文件 |
| ---: | --- |
| 174 | `Assets/Scripts/Units/NPC/NPCPatrol.cs` |
| 171 | `Assets/Scripts/Units/Enemy/EnemyMovement.cs` |
| 120 | `Assets/Scripts/Units/NPC/NPCWander.cs` |
| 105 | `Assets/Scripts/Units/NPC/NPCChat.cs` |
| 56 | `Assets/Scripts/Units/ShopKeeper/ShopKeeper.cs` |
| 42 | `Assets/Scripts/Units/NPC/NPCStateController.cs` |
| 31 | `Assets/Scripts/Units/Enemy/EnemyHealth.cs` |
| 29 | `Assets/Scripts/Units/Enemy/EnemyKnockBack.cs` |
| 26 | `Assets/Scripts/Units/Enemy/EnemyCombat.cs` |

### A Star（5 文件 / 664 行）

| 行数 | 文件 |
| ---: | --- |
| 218 | `Assets/Scripts/A Star/MovementController.cs` |
| 185 | `Assets/Scripts/A Star/AStarPathFinder.cs` |
| 183 | `Assets/Scripts/A Star/AStarNodeManager.cs` |
| 44 | `Assets/Scripts/A Star/PathFinderDetails.cs` |
| 34 | `Assets/Scripts/A Star/AStarNode.cs` |

### Player（9 文件 / 629 行）

| 行数 | 文件 |
| ---: | --- |
| 216 | `Assets/Scripts/Player/PlayerMovement.cs` |
| 103 | `Assets/Scripts/Player/PlayerBow.cs` |
| 83 | `Assets/Scripts/Player/StatsManager.cs` |
| 59 | `Assets/Scripts/Player/Arrow.cs` |
| 49 | `Assets/Scripts/Player/PlayerCombat.cs` |
| 40 | `Assets/Scripts/Player/TimeManager.cs` |
| 34 | `Assets/Scripts/Player/ShiftEquipment.cs` |
| 25 | `Assets/Scripts/Player/PlayerHealth.cs` |
| 20 | `Assets/Scripts/Player/PlayerController.cs` |

### Inventory（4 文件 / 486 行）

| 行数 | 文件 |
| ---: | --- |
| 254 | `Assets/Scripts/Inventory/Items/InventoryManager.cs` |
| 130 | `Assets/Scripts/Inventory/Items/Loot.cs` |
| 61 | `Assets/Scripts/Inventory/Items/InventorySlot.cs` |
| 41 | `Assets/Scripts/Inventory/Items/UseItem.cs` |

### SaveAndLoad（6 文件 / 448 行）

| 行数 | 文件 |
| ---: | --- |
| 212 | `Assets/Scripts/SaveAndLoad/SaveSystem.cs` |
| 126 | `Assets/Scripts/SaveAndLoad/DataManager.cs` |
| 57 | `Assets/Scripts/SaveAndLoad/Data.cs` |
| 23 | `Assets/Scripts/SaveAndLoad/DataDefinition.cs` |
| 16 | `Assets/Scripts/SaveAndLoad/ISaveable.cs` |
| 14 | `Assets/Scripts/SaveAndLoad/DynamicDataHandler.cs` |

### ScriptableObjects（20 文件 / 367 行）

| 行数 | 文件 |
| ---: | --- |
| 38 | `Assets/Scripts/ScriptableObjects/DialogSO.cs` |
| 33 | `Assets/Scripts/ScriptableObjects/QuestSO.cs` |
| 32 | `Assets/Scripts/ScriptableObjects/Events/InventorySlotsStatsSO.cs` |
| 25 | `Assets/Scripts/ScriptableObjects/ItemSO.cs` |
| 21 | `Assets/Scripts/ScriptableObjects/GuidSO.cs` |
| 19 | `Assets/Scripts/ScriptableObjects/Events/SceneLoadEventSO.cs` |
| 19 | `Assets/Scripts/ScriptableObjects/Events/ShopLoadEventSO.cs` |
| 19 | `Assets/Scripts/ScriptableObjects/LocationSO.cs` |
| 17 | `Assets/Scripts/ScriptableObjects/Events/QuestOptionsEventSO.cs` |
| 16 | `Assets/Scripts/ScriptableObjects/GameSceneSO.cs` |
| 15 | `Assets/Scripts/ScriptableObjects/Events/ToggleCanvasEventSO.cs` |
| 14 | `Assets/Scripts/ScriptableObjects/Events/VoidEventSO.cs` |
| 13 | `Assets/Scripts/ScriptableObjects/Events/DataSaveEventSO.cs` |
| 13 | `Assets/Scripts/ScriptableObjects/Events/LoadQuestEventSO.cs` |
| 13 | `Assets/Scripts/ScriptableObjects/Events/LootEventSO.cs` |
| 13 | `Assets/Scripts/ScriptableObjects/Events/OpenSaveLoadPanelEventSO.cs` |
| 13 | `Assets/Scripts/ScriptableObjects/SkillSO.cs` |
| 12 | `Assets/Scripts/ScriptableObjects/RefuseDialogSO.cs` |
| 11 | `Assets/Scripts/ScriptableObjects/CharacterSO.cs` |
| 11 | `Assets/Scripts/ScriptableObjects/LoacatoinSO.cs` |

### Scene（5 文件 / 335 行）

| 行数 | 文件 |
| ---: | --- |
| 205 | `Assets/Scripts/Scene/SceneChanger.cs` |
| 60 | `Assets/Scripts/Scene/ConfinerFinder.cs` |
| 36 | `Assets/Scripts/Scene/RetryManager.cs` |
| 19 | `Assets/Scripts/Scene/SceneDataForSave.cs` |
| 15 | `Assets/Scripts/Scene/Teleport.cs` |

### Root（2 文件 / 90 行）

| 行数 | 文件 |
| ---: | --- |
| 78 | `Assets/Scripts/MyEnums.cs` |
| 12 | `Assets/Scripts/InitialLoad.cs` |

### Grid（2 文件 / 52 行）

| 行数 | 文件 |
| ---: | --- |
| 26 | `Assets/Scripts/Grid/ElevationEntry.cs` |
| 26 | `Assets/Scripts/Grid/ElevationExit.cs` |
