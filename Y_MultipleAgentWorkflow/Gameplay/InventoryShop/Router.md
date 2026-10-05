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
- `path:Assets\Scripts\Gameplay\Inventory`
- `path:Assets\Scripts\Gameplay\Shop`

## 能力边界

**Active 能力**
- `InventoryShop_Guide.md`（ID `GP-INVENTORYSHOP-GUIDE`）是本域当前实现的权威描述：背包槽位与堆叠、拾取/丢弃/使用、金币与商店买卖、`ItemSO` 共同配置约定。
- 本域真实实现分散在 `Assets/Scripts/Gameplay/Inventory`、`Assets/Scripts/Gameplay/Shop`、`Assets/Scripts/Pipeline/SO/ItemSO.cs`、`Assets/Scripts/Pipeline/SO/Events/{InventorySlotsStatsSO,LootEventSO}.cs`、`Assets/Scripts/Gameplay/Dialog/HistoryManager/ItemHistoryManager.cs`、`Assets/Scripts/Gameplay/Units/ShopKeeper/ShopKeeper.cs`。

**Proposal**
- 无。旧文档 `Docs/My_ARPG_MVCS项目现状.md:109` 提出的 `InventoryModel`/`InventoryService`/`ItemEffectService`/`ShopModel`/`ShopService` 拆分方向只是旧文档设想，代码中不存在对应类型，本 Router 不把它记为已确认能力。

**需要用户确认**
- 背包与商店是否按旧文档方向做分层重构（现状是单管理器形态）。
- 背包内容与金币不入档是否有意（`SaveData` 无相关字段）；若要入档需先定 DTO 边界。
- 出售 `isEXP`/`isGold` 物品的语义：允许出售则需改分支，禁止出售则需明确拒绝反馈。
- `ItemSO.stackableSize = 0` 的 `Bow.asset` 是设计意图还是配置遗漏。
- 出售价格是否应继续取商店槽位价格（当前可能命中已失活槽位的旧值）。
