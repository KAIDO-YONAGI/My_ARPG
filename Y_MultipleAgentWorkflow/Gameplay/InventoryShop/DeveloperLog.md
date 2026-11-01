# Gameplay.InventoryShop Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-01 商店无效槽位与打开门槛

`ShopCanvas.prefab` 移除误挂在 5 个属性文本上的 `ShopSlot` 组件，商品槽数组保留 8 个配置有效的嵌套槽位。复核 §2.5 与 §2.6，商店仍在 `CanOpen` 通过并进入 `OnOpen` 后通知管理器装载商品，无需改动业务 Guide。槽位引用与 YAML 文件 ID 静态检查通过，`git diff --check` 通过；EditMode 未运行，因为当前工程已有 Unity Editor 打开，项目验证规则禁止并行启动批处理 Editor。维护计数 `4/5 -> 5/5`，Guide 复核无变更后清零为 `0/5`。

## 2026-11-01 背包与商店视图直接查询 State

背包与快捷栏按索引读取金币和槽位，商店按索引读取商品与价格；脏标记操作由对应 `State` 提供。刷新不复制集合或创建快照。维护计数更新为 `3/5 -> 4/5`。

## 2026-11-01 背包与商店改用只读快照

`InventoryManager` 将金币、快捷栏和背包分开提供版本快照，集合使用只读副本；`ShopManager` 向画布提供商品快照。视图完成对应区域绘制后按版本确认，金币变化不会刷新快捷栏槽位。维护计数更新为 `2/5 -> 3/5`。

## 2026-10-31 商店与背包界面迁入缓存预制体

- `ShopManager` 不再实现 `ICanvasManager`，改由 `ShopCanvasView`（`CanOpenFromView` 判店主）承接界面；`BackpackCanvasManager` 仍保留 `ICanvasManager` 用于背包画布。
- `ShopSlot` 与 `ShopCategoryToggles` 去掉 `shopRef` 与 `IShopInteractable` 转换，数据由 `ShopCanvasView` 注入。
- `InventoryManager` 暴露 `State`、`TryGetSlotState`、`InitializeHotbarState`；`BackpackCanvasView` 承载背包，常驻快捷栏拆出 `HotbarCanvasView`。

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
  - `ItemHistoryManager.RecordItem` 在 `InventoryManager` 有 4 处写入点。
  - 买卖走单一 `InventorySlotsStatsSO` 通道，出售用负价格负数量，结算集中在 `HandleShopping`；金币唯一写入口 `UpdateGold`。
  - 背包与金币不入档：SaveData 没有相关字段，InventoryManager 未实现 ISaveable。
- 结构说明：原 Router 的「下级导航」子表只有占位行 `| None | None |`（无真实子类），重写时按项目中文模板 `..\..\Workflow\Templates\BusinessRouter.template.md` 改写为 `| 无 | 无 |`，未删除任何真实子类行。
- 维护计数：`0/5`（未变更，本次仅建立文档）。
