# Gameplay.InventoryShop Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 背包与商店 权威文档

- 任务：为 `Gameplay.InventoryShop` 建立当前实现的事实性权威文档，并按项目中文模板重写 Router。
- 写入的文件：
  - `Y_MultipleAgentWorkflow\Gameplay\InventoryShop\InventoryShop_Guide.md`（新建，ID `GP-INVENTORYSHOP-GUIDE`，状态 Active）
  - `Y_MultipleAgentWorkflow\Gameplay\InventoryShop\Router.md`（按中文模板重写，ID `BUS-GAMEPLAY-INVENTORYSHOP`）
  - `Y_MultipleAgentWorkflow\Gameplay\InventoryShop\DeveloperLog.md`（本条目，追加）
- 依据的证据路径（全部实读）：
  - 代码：`Assets/Scripts/Gameplay/Inventory/{InventoryManager,InventorySlot,UseItem,Loot,BackpackCanvasManager}.cs`、`Assets/Scripts/Gameplay/Shop/{ShopManager,ShopSlot,ShopInfoPanel,ShopCategoryToggles,ShopPortraitCamera}.cs`、`Assets/Scripts/Pipeline/SO/ItemSO.cs`、`Assets/Scripts/Pipeline/SO/Events/{InventorySlotsStatsSO,LootEventSO}.cs`、`Assets/Scripts/Gameplay/Dialog/HistoryManager/ItemHistoryManager.cs`、`Assets/Scripts/Gameplay/Units/ShopKeeper/ShopKeeper.cs`、`Assets/Scripts/Contracts/{IShopInteractable,ICanvasManager,YSingleton}.cs`、`Assets/Scripts/Pipeline/ObjectPool.cs`、`Assets/Scripts/Gameplay/Save/SaveData.cs`、`Assets/Scripts/Gameplay/Player/Services/StatsService.cs`、`Assets/Scripts/Gameplay/Player/Models/PlayerStatsModel.cs`、`Assets/Scripts/Gameplay/Quest/QuestManager.cs`
  - 配置资产：`Assets/GameSO/ItemSO/{Gold,EXP,Mushroom,Meat,Wood,Bow}.asset`
  - 场景接线：`Assets/Scenes/GameScene/PersistentScene.unity`（InventoryManager/ShopManager/BackpackCanvasManager 的脚本 guid 引用）
  - 旧文档线索：`Docs/My_ARPG_MVCS项目现状.md`（109、120-124 行）、`Docs/游戏指南.txt`（33-35 行）
- 已核验项（静态读文件确认）：
  - 背包无独立数据类，槽位 = 场景 `InventorySlot`，`Start` 里先 hotbar 后 backpack 拼接，堆叠上限取 `ItemSO.stackableSize`。
  - `UseItem` 对数值域共 **8 处**调用点（`UpdateMaxHealth`/`UpdateHealth`/`UpdateSpeed`/`UpdateDamage` 各 2 次），非 6 处。
  - `ItemHistoryManager.RecordItem` 在 `InventoryManager` 有 **4 处**写入点（99-107、120-125、133-138、225 行）。
  - 买卖走单一 `InventorySlotsStatsSO` 通道，出售用负价格负数量，结算集中在 `HandleShopping`；金币唯一写入口 `UpdateGold`。
  - 背包与金币不入档（`SaveData.cs:9-11` 无相关字段，`InventoryManager` 未实现 `ISaveable`）。
- 未核验项：见 Guide §5（事件资产是否同一份资产、PersistentScene 是否整局不卸载、槽位是否全激活、`shopSlots` 长度是否够用、`stackableSize = 0` 是否为设计意图等 7 条，均标注为「未运行 Unity 验证」）。
- 发现的缺陷：出售 `isEXP` 物品的三重后果、出售 `isGold` 物品倒扣金币且不删物、`stackableSize = 0`（Bow）拾取即原地弹回、`PopulateShopItems` 不清空多余槽位导致出售可能按过期价格成交、任务奖励溢出到地面、`useItem`/`lootPrefab` 无空守卫。
- 结构说明：原 Router 的「下级导航」子表只有占位行 `| None | None |`（无真实子类），重写时按项目中文模板 `Workflow\Templates\BusinessRouter.template.md:16-18` 改写为 `| 无 | 无 |`，未删除任何真实子类行。
- 维护计数：`0/5`（未变更，本次仅建立文档）。