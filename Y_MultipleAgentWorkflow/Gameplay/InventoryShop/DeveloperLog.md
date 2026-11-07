# Gameplay.InventoryShop Developer Log

## 维护规则

按项目约定记录带日期的任务事实、验证证据与维护计数，现行结论同步到对应 Guide。了解现状优先读取 Guide；需要任务历史时，只定位并读取最近一天或两天的相关条目，禁止整份日志载入上下文。只有任务需要追溯历史时才按日期、标题或关键词定向读取更早条目。读取范围不改变存储保留，删除具体历史条目须经用户明确确认。纯文档维护不增加业务计数。

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-07 背包与商店使用独立版本读取

- `InventoryManager.State` 为金币、快捷栏、背包提供独立版本，商品状态提供 `ItemsVersion`。视图各自保存 `VersionReader`，绘制后记录捕获版本，打开时使本地记录失效。
- 初始化接收 `IReadOnlyList<InventorySlotState>`，视图仅在管理器首次初始化时提取值数组；点击传递 `SlotAddress`。管理器的初始化、使用、出售选择和丢弃入口均移除 UI 槽位参数。
- 验证：`UICacheRegressionTests` 覆盖两个金币视图独立刷新、金币变化保持槽位、背包同版本重开、初始值复制与一次初始化。现有 Unity 2022.3.62f3 实例内的 EditMode 结果为 80/80 通过，其中项目用例 79 项；XML 为 `Logs/ui-version-editmode.xml`。
- 维护计数：`3/5 → 4/5`。

## 2026-11-02 公共掉落池沿用独立 Loot 预制体

- 结构：沿用 `Assets/Prefabs/Inventory/Loot.prefab`，其根对象包含 Animator、触发器、Loot 与存档标记，Sprite 子物体负责显示；实际物品由 `Initialize` 注入。
- 接线：常驻场景 `InventoryManager.lootPrefab` 指向 `Loot.prefab`，池预热、扩容和池满时直接实例化共用该引用。具体物品预制体继续承担场景摆放职责。
- 模板：整理根对象和 Sprite 子物体的局部位置，清理残留默认数量，沿用空物品与空图标配置、现有组件引用及动画默认值恢复配置。
- 证据：全工程资产检索确认公共 `lootPrefab` 配置位于常驻场景；Unity 在编辑模式导入模板并保存常驻场景，返回实际模板路径为 `Assets/Prefabs/Inventory/Loot.prefab`，物品和图标为空、保存结果为成功。按用户要求未启动游戏或运行测试。
- 文档：Guide 同步公共模板职责和引用关系，维护计数 `2/5 -> 3/5`。

## 2026-11-02 修正实际掉落模板与动画复位顺序

现场检查用户指出的 `(8.5488, 2.1695, 0)` 掉落物：贴图为 `Mushroom03`，透明度为 0、子物体缩放为 `(0.7, 1, 1)`；Animator 已在空 Blend Tree 的 `Idle`，`isPicked` 为 false，记录的静止基线仍为透明度 1、缩放 `(1, 1, 1)`。残留外观与 `Pickup.anim` 末帧一致。常驻场景实际引用 `Gold.prefab`，此前修改的 `Loot.prefab` 是另一份资产，未覆盖真实池模板。

- 代码：`Loot.ResetVisualState` 先启用 Animator、清除拾取参数、播放并立即求值 `Idle`，再停用 Animator，最后恢复 SpriteRenderer 启用状态、颜色、排序、缩放与位置；拾取入口重新启用 Animator 并从头播放 `Pickup`。
- 资产：实际模板 `Assets/Prefabs/Inventory/Gold.prefab` 开启 `m_WriteDefaultValuesOnDisable`。
- 验证边界：刷新后 Unity 已完成编译，`isCompiling` 与 `isUpdating` 均为 false，控制台错误为 0。已进入运行模式准备跨帧验证，但用户明确要求停止测试，因此未执行预热、扩容及回收复用的跨帧验证，也未以旧合成用例作为修复证明；退出本次启动的运行模式。
- 文档：Guide 同步真实模板与当前复位顺序；保留上次修复记录供追溯，维护计数 `1/5 -> 2/5`。

## 2026-11-02 掉落物池复用的显示状态复原

现象：从快捷栏或背包丢出的物品落地后看不见，拾取、堆叠、存档都正常。原因是 `Pickup.anim` 末帧把子物体 `Sprite` 的 `m_Color.a` 写到 0、排序层级临时抬到 50、缩放压到 0.7，而 `Loot.prefab` 的 Animator 关闭了禁用时写回默认值，这些值随对象留在池里；`Loot.Initialize` 只写贴图，全仓库没有一处写 `sr.color`，池取出的实例就是全透明精灵。

- 代码：`Loot` 记录预制体静止显示状态（`Awake` 与首次 `Initialize`），取用时写回颜色、排序层级、子物体缩放与位置，并把状态机切回 `Idle`。
- 资产：`Loot.prefab` 的 Animator 开启禁用时写回默认值，归还后再兜一层复原。
- 用例：新增 `Assets/Tests/Editor/LootPoolingTests.cs`，模拟片段写下的透明与压扁后再次 `Initialize`，断言四项显示状态复原。
- 验证：Unity 在 17:10:43 重建 `Assembly-CSharp.dll` 与 `Assembly-CSharp-Editor.dll`，编辑器日志无 `error CS`，两个程序集分别含 `ResetVisualState` 与 `LootPoolingTests`。EditMode 全量未跑：批处理 Editor 在本会话沙箱内启动即退出（退出码 140063，不产出 log 与结果 XML）。
- 文档：Guide 新增 §2.2 池复用复原与 §3.9 硬边界；维护计数 `0/5 -> 1/5`。

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
