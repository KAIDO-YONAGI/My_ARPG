# Gameplay.InventoryShop Router

文档 ID：`BUS-GAMEPLAY-INVENTORYSHOP`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `背包` / `InventoryManager` / `InventorySlot` / `槽位` | `InventoryShop_Guide.md` |
| `堆叠` / `stackableSize` / `背包满了` | `InventoryShop_Guide.md` |
| `金币` / `goldAmount` / `Gold` / `货币` | `InventoryShop_Guide.md` |
| `商店` / `ShopManager` / `ShopKeeper` / `ShopSlot` | `InventoryShop_Guide.md` |
| `买入` / `卖出` / `TryBuyItem` / `SellItem` / `HandleShopping` | `InventoryShop_Guide.md` |
| `使用物品` / `UseItem` / `ItemSO` 效果 | `InventoryShop_Guide.md` |
| `物品史` / `ItemHistoryManager` / `RecordItem` | `InventoryShop_Guide.md` |
| `掉落` / `Loot` / `丢弃` / `DropByClick` / 掉落池 | `InventoryShop_Guide.md` |
| `BackpackCanvas` / `ToggleShopEvent` / `ToggleBackpackEvent` | `InventoryShop_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Gameplay.InventoryShop`
- `path:Y_MultipleAgentWorkflow\Gameplay\InventoryShop`
- 背包实现脚本 `InventoryManager`、`InventorySlot`、`UseItem`、`Loot`、`BackpackCanvasManager`
- 商店实现脚本 `ShopManager`、`ShopSlot`、`ShopInfoPanel`、`ShopCategoryToggles`、`ShopPortraitCamera`
- 配置与事件资产 `ItemSO`、`InventorySlotsStatsSO`、`LootEventSO`
- 场景与预制体接线 常驻场景、背包与商店画布

## 能力边界

**Active 能力**
- `InventoryShop_Guide.md` 是本域当前实现的权威描述，ID 为 `GP-INVENTORYSHOP-GUIDE`，覆盖背包槽位与堆叠、拾取与丢弃、物品使用、金币与商店买卖、`ItemSO` 共同配置约定。
- 本域实现类型：`InventoryManager`、`InventorySlot`、`UseItem`、`Loot`、`BackpackCanvasManager`、`ShopManager`、`ShopSlot`、`ShopInfoPanel`、`ShopCategoryToggles`、`ShopPortraitCamera`、`ItemHistoryManager`、`ShopKeeper`；配置与事件类型：`ItemSO`、`InventorySlotsStatsSO`、`LootEventSO`。
