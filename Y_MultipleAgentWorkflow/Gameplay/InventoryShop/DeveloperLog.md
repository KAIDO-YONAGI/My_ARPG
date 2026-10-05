# Gameplay.InventoryShop Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 背包与商店 权威文档

- 任务：为 `Gameplay.InventoryShop` 建立当前实现的事实性权威文档，并按项目中文模板重写 Router。
- 写入的文件：
  - `InventoryShop_Guide.md`（新建，ID `GP-INVENTORYSHOP-GUIDE`，状态 Active）
  - `Router.md`（按中文模板重写，ID `BUS-GAMEPLAY-INVENTORYSHOP`）
  - `DeveloperLog.md`（本条目，追加）
- 依据的证据路径（全部实读）：
  - 代码：`InventoryManager`、`InventorySlot`、`UseItem`、`Loot`、`BackpackCanvasManager`、`ShopManager`、`ShopSlot`、`ShopInfoPanel`、`ShopCategoryToggles`、`ShopPortraitCamera`、`ItemSO`、`InventorySlotsStatsSO`、`LootEventSO`、`ItemHistoryManager`、`ShopKeeper`、`IShopInteractable`、`ICanvasManager`、`YSingleton`、`ObjectPool`、`SaveData`、`StatsService`、`PlayerStatsModel`、`QuestManager`
  - 配置资产：ItemSO 目录下的 Gold、EXP、Mushroom、Meat、Wood、Bow 六份物品配置。
  - 场景接线：PersistentScene 场景中 InventoryManager、ShopManager、BackpackCanvasManager 的脚本引用。
- 已核验项（静态读文件确认）：
  - 背包无独立数据类，槽位 = 场景 `InventorySlot`，`Start` 里先 hotbar 后 backpack 拼接，堆叠上限取 `ItemSO.stackableSize`。
  - `UseItem` 对数值域共 **8 处**调用点（`UpdateMaxHealth`/`UpdateHealth`/`UpdateSpeed`/`UpdateDamage` 各 2 次）。
  - `ItemHistoryManager.RecordItem` 在 `InventoryManager` 有 **4 处**写入点（99-107、120-125、133-138、225 行）。
  - 买卖走单一 `InventorySlotsStatsSO` 通道，出售用负价格负数量，结算集中在 `HandleShopping`；金币唯一写入口 `UpdateGold`。
  - 背包与金币不入档：SaveData 没有相关字段，InventoryManager 未实现 ISaveable。
- 结构说明：原 Router 的「下级导航」子表只有占位行 `| None | None |`（无真实子类），重写时按项目中文模板 `..\..\Workflow\Templates\BusinessRouter.template.md` 改写为 `| 无 | 无 |`，未删除任何真实子类行。
- 维护计数：`0/5`（未变更，本次仅建立文档）。
