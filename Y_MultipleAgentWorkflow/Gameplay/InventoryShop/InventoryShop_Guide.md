# 背包与商店权威指南：Gameplay.InventoryShop

文档 ID：`GP-INVENTORYSHOP-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本域负责背包槽位数据与堆叠、拾取与丢弃、物品使用、金币与商店买卖这条链路；玩家数值域的内部规则归 `Gameplay.PlayerStats`，掉落物存档格式的内部规则归存档域，任务与对话如何发起奖励归各自域。
上游来源：
- 背包实现：`InventoryManager`、`InventorySlot`、`UseItem`、`Loot`、`BackpackCanvasManager`
- 商店实现：`ShopManager`、`ShopSlot`、`ShopInfoPanel`、`ShopCategoryToggles`、`ShopPortraitCamera`
- 配置与事件：`ItemSO`、`InventorySlotsStatsSO`、`LootEventSO`
- 历史与单位：`ItemHistoryManager`、`ShopKeeper`
- 场景接线：常驻场景、背包与商店画布

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `InventoryManager` / `背包满了` | §2.1、§3 |
| `堆叠` / `stackableSize` / `堆叠上限` | §2.1、§3 |
| `金币` / `goldAmount` / `Gold` | §2.5、§3 |
| `买卖` / `SellItem` / `TryBuyItem` / `HandleShopping` | §2.5 |
| `使用物品` / `UseItem` / `吃蘑菇` | §2.3 |
| `物品史` / `ItemHistoryManager` / `RecordItem` | §2.4 |
| `掉落` / `Loot` / `丢弃` / `DropByClick` | §2.2 |
| `ItemSO` / `配置物品` | §2.7 |
| `ShopKeeper` / `商店不打开` | §2.6 |

## 2. 当前实现

### 2.1 背包数据结构与堆叠规则

- 背包没有独立的数据类。槽位就是场景里的 `InventorySlot` 组件，`InventoryManager.Start` 用两次 `GetComponentsInChildren` 把 `hotbarParent` 与 `backpackParent` 的子槽位拼成一个运行时 `List<InventorySlot>`。这份列表的顺序决定优先级：快捷栏在前、背包在后，填充总是从快捷栏开始。
- 槽位私有状态只有 `itemSO` 与 `quantity` 两个字段，对外只读暴露 `ItemSO`、`Quantity`、`IsEmpty`。
- 堆叠上限来自 `ItemSO.stackableSize`。`AddItem` 在同类物品上取上限减去现有数量，在空槽上取 `stackableSize`，返回实际放入数量；`SpaceRemaining` 用同一套上限，购买前的容量检查走 `HasSpaceForItem`。
- `RemoveItem` 在数量归零时把 `quantity` 归 0 并清空 `itemSO`，维持空槽 `itemSO` 为 null 的不变量；`UpdateUI` 在 `quantity <= 0` 时同样清空 `itemSO`。
- 背包内容与金币都不入档：`SaveData` 顶层只有 `lootsStatsDic`、`sceneIDAndPlayerPos`、`playerStatsData` 三个字段，`InventoryManager` 未实现 `ISaveable`。组件挂在常驻场景上，因此一次运行内跨游戏场景保留，读档后回到本局的运行值。

### 2.2 拾取、丢弃、使用三条入口

- 拾取：`Loot.OnTriggerEnter2D` 判定 `Player` 标签与 `canBePick`，播 `Pickup` 动画并广播 `lootEvent.OnEventRaised(item, quantity, this)`，由 `InventoryManager.OnItemLootedHandler` 接手并调 `UpdateInventorySlots`。
- 丢弃：右键槽位触发 `InventoryManager.DropByClick`，先 `DropLoot(ItemSO, 1)` 再 `slot.RemoveItem(1)`。`DropLoot` 走 `ObjectPool<Loot>`，池取不到对象时一次性实例化兜底并手工补一次 `OnPoolGet`。掉落物经 `SceneManager.MoveGameObjectToScene` 挂到当前场景根，`Loot.OnPoolGet` 先把父级置空。
- 使用：左键槽位且商店未打开时调 `InventoryManager.UseItem`，随后执行 `useItem.ApplyItemEffects`、`slot.RemoveItem(1)`，并写物品史 `-1`。
- 拾取余量：`UpdateInventorySlots` 放完所有槽位后仍有 `quantity > 0` 时，用 `DropLoot` 把余量掉在玩家脚下；来源是同一个 `Loot` 对象时走原地重掉分支，位移到玩家位置、`Initialize`、重新激活并延时恢复可拾取。

### 2.3 物品使用对数值域的写命令

`UseItem.ApplyItemEffects` 与延时器 `EffectTimer` 一共 8 个调用点，覆盖 `UpdateMaxHealth`、`UpdateHealth`、`UpdateSpeed`、`UpdateDamage` 四个写命令各两次。所有写入都经 `StatsService`，钳制与事件仍在 `PlayerStatsModel`。

另有两个跨域写点：拾取 `isEXP` 物品调 `StatsService.AddExperience(quantity)`，任务奖励的金币与经验复用同一分支。

### 2.4 物品史的写入点

`ItemHistoryManager` 是内存字典 `Dictionary<ItemSO,int>`，另有 `HasPickedOverAmount` 与 `GetItemQuantity` 给对话与任务读取，条目归零后保留。写命令 `RecordItem` 在 `InventoryManager` 里共有 4 处：

| 场景 | 记账 |
|---|---|
| 拾取或奖励到账的金币 | `+quantity` |
| 出售扣减 | `-removed`，取实际移除量 |
| 拾取或购买入包 | `+placed`，每槽按实际放入量写一次 |
| 使用物品 | `-1` |

### 2.5 商店：买入、卖出与货币来源

- 商品数据由 `ShopKeeper` 持有，分 `shopItems`、`shopWeapon`、`shopArmor` 三类列表，元素是 `ShopItems{item, price}`。
- 玩家进出范围经 `ShopKeeperEventSO` 到 `ShopManager.OnKeeperEntered` 与 `OnKeeperExited`，记录 `activeShopKeeper`；离开时商店若开着就 `CloseShop`。
- 开关经 `toggleShopCanvasEvent.toggleCanvasEvent` 到 `OnShopToggle`；打开时用 `activeShopKeeper` 的三份列表调 `OpenShop`，再由 `PopulateShopItems` 逐槽 `Initialize(item, price)`，多余槽位 `SetActive(false)`。
- 买入：`ShopSlot.OnBuyButtonClick` 调 `IShopInteractable.TryBuyItem`，后者发 `InventoryUpdateRequest.RaiseInventoryUpdateRequest(item, price, 1)`。
- 卖出：左键背包槽位且 `ShopManager.Instance.IsShopOpen` 为真时，先 `SetSlotBeenClicked(this)` 再 `ShopManager.Instance.SellItem(itemSO)`。`SellItem` 在当前 `shopSlots` 里找同名 `ItemSO`，命中后以负价格、负数量发事件。
- 结算集中在 `InventoryManager.HandleShopping`：`goldAmount` 小于 `price` 时直接返回；买入在 `HasSpaceForItem` 通过后先 `UpdateGold(price)` 再 `UpdateInventorySlots(item, amount)`；卖出先 `UpdateGold(price)` 再 `UpdateInventorySlots(item, amount)`。
- 金币的写入点是 `UpdateGold` 与 `UpdateInventorySlots` 的 `isGold` 分支：`UpdateGold` 做 `goldAmount -= price` 并刷新 `goldAmountText`，`isGold` 分支做 `goldAmount += quantity` 并刷新同一文本。两处都必须同步文本，文本是硬引用。
- 货币来源：`goldAmount` 初值为 0，增加路径是拾取或奖励 `isGold` 物品时的 `goldAmount += quantity`，以及出售时经 `UpdateGold` 以负价格反向增加；金币没有其它增加入口。
- 商店不消耗库存：`TryBuyItem` 只发事件，商店侧没有数量扣减与售罄处理。

### 2.6 订阅惯例与画布

- 事件通道都是 `ScriptableObject` 加 C# `event`，订阅成对写在 `OnEnable` 与 `OnDisable`：`InventoryManager` 订 3 条，`ShopManager` 订 4 条，`BackpackCanvasManager` 订 3 条；场景加载事件统一用来复位画布。
- 面板显隐走 `ICanvasManager` 的默认实现，并把状态上报 `UIManager`。`ShopManager` 与 `BackpackCanvasManager` 都实现该接口，并把 `ToggleCanvasEvent`、`SceneLoadedEvent` 暴露出去供 `UIManager` 注册。
- 面板内部控件取父级商店的方式是 `[SerializeField] Component shopRef` 加 `as IShopInteractable`，因为接口无法序列化；引用为空时打 `Debug.LogError`。`ShopSlot` 与 `ShopCategoryToggles` 都走这条路。
- `InventorySlot` 直接走 `ShopManager.Instance`，`IShopInteractable` 的注释把这条记为暂留单例访问。
- 商店头像相机订阅同一 `ShopKeeperEventSO`，无 keeper 时按 `hideWhenNoShopKeeper` 关闭 Camera，`LateUpdate` 跟目标偏移。
- 悬停信息面板只读 `ItemSO` 的展示字段与五个数值字段，负数与零不显示。

### 2.7 ItemSO 作为共同配置类型

`ItemSO` 是背包与商店唯一共同消费的配置类型：`itemName`、`itemDescription`、`icon` 负责展示，`isGold` 与 `isEXP` 决定是否走背包槽位，`stackableSize` 决定堆叠，`currentHealth`、`maxHealth`、`speed`、`damage`、`duration` 决定使用效果。它同时被商店、任务、对话引用，四个域共享同一份资产。

物品配置资产目录下的现有资产：

| 资产 | 关键字段 |
|---|---|
| `Gold` | `isGold` 为 1、`stackableSize` 为 0 |
| `EXP` | `isEXP` 为 1、`stackableSize` 为 10000000 |
| `Mushroom` | `stackableSize` 为 5、`currentHealth` 与 `maxHealth` 与 `speed` 为 2、`duration` 为 2 |
| `Meat` | `stackableSize` 为 3 |
| `Wood` | `stackableSize` 为 10 |
| `Bow` | `stackableSize` 为 0 |

## 3. 约定与硬边界

1. **`stackableSize` 必须 ≥ 1**，取 0 的物品永远进不了背包：`AddItem` 与 `SpaceRemaining` 都以它算容量，此时 `HasSpaceForItem` 恒为 false。`Bow` 是现存反例，见 §4。
2. **空槽 `itemSO` 必须是 null**：`IsEmpty` 与 `SpaceRemaining` 的分支都依赖它；手改场景槽位时把数量留 0 又留着 `itemSO`，会在 `UpdateUI` 里被清空。
3. **出售必须先 `SetSlotBeenClicked`**：`UpdateInventorySlots` 的负数分支只认这个字段，未设置时只打一条 `Debug.Log("No slot been Marked")`，随后静默保留物品。从别处直接调 `SellItem` 会留下这个语义缺口。
4. **金币的写入点是 `UpdateGold` 与 `isGold` 分支，两处都必须同步 `goldAmountText`**：文本是硬引用，为空即抛 `NullReferenceException`。
5. **`isGold` 与 `isEXP` 分支先于数量正负判断**：这两类物品走加钱或加经验并 `return` 的路径，不参与槽位增删，任何新的负数量交易都会先撞上它们。
6. **交易失败静默**：金币不足或背包无空间时 `HandleShopping` 直接返回，没有 UI 反馈，也没有事件。
7. **背包与金币不入档**：`SaveData` 的字段不含它们，域内唯一实现 `ISaveable` 的对象是 `Loot`；读档后回到本局运行值。
8. **槽位顺序即优先级**：`Start` 里 hotbar 先于 backpack 拼接，改动层级会改变入包顺序与背包满时的表现。

## 4. 已知缺陷与风险

1. **出售 `isEXP` 道具：加钱、留物、经验被拦。** 交易走 `UpdateInventorySlots` 的 `isEXP` 分支，位置在扣金币之后，因此出售照样进账；`return` 跳过通用出售分支，道具留在背包里；`PlayerStatsModel.AddExp` 忽略非正数，只留一条警告。
2. **出售 `isGold` 道具会倒扣金币。** 同一路径把 `quantity` 取 -1 加到 `goldAmount` 上，金币减 1 后 `return`，道具一并留在背包里，后果比缺陷 1 更反直觉。
3. **`stackableSize` 为 0 的物品拾取即原地弹回。** `Bow` 取 0，`AddItem` 返回 0，余量走 `DropLoot(item, quantity, lootObj)` 的原地重掉分支，把掉落物位移到玩家脚下并在 0.3 倍动画时长后恢复 `canBePick`，玩家站在物品上会反复触发拾取动画。
4. **`PopulateShopItems` 对多余槽位只 `SetActive(false)`。** `ShopSlot.item` 与 `price` 保留上一个分类的旧值，而 `SellItem` 遍历整个 `shopSlots` 数组，可能命中失活槽位并按过期价格成交。
5. **任务奖励溢出到地面。** `HandleQuestReward` 忽略 `price` 直接 `UpdateInventorySlots(item, amount)`，而奖励由 `QuestManager` 发出；背包装不下时余量被 `DropLoot` 掉在玩家脚下，奖励变成可丢失的地面物。
6. **序列化引用缺空守卫。** `useItem` 为空时使用物品直接抛 NRE，`lootPrefab` 为空时 `ObjectPool` 构造即抛 NRE。
7. **`InventoryManager` 同时承担背包数据、金币、拾取、商店校验、掉落池、物品使用六类职责。**
8. **本域没有回归网。** 编辑器用例 `ObjectPoolTests`、`PlayerStatsSOTests`、`CanvasFocusStackTests`、`PlayerStatsModelTests`、`AStarOpenHeapTests` 都不覆盖 `InventoryManager`、`InventorySlot`、`UseItem`、`ShopManager`。

## 5. 未核验事项

以下结论来自静态阅读，运行期行为待编辑器实测。

- `InventoryManager.ShoppingRequest` 与 `ShopManager.InventoryUpdateRequest` 在常驻场景中指向同一个 `InventorySlotsStatsSO` 资产，`QuestRewardRequest` 与 `QuestManager` 的引用同理。
- `InventoryManager`、`ShopManager`、`BackpackCanvasManager` 的常驻场景实例在整局游戏内不卸载，背包内容与金币因此跨游戏场景保留。脚本 guid 已被常驻场景引用，其余行为待实测。
- `hotbarParent` 与 `backpackParent` 下所有 `InventorySlot` 都处于激活状态，`GetComponentsInChildren` 会跳过失活对象。
- `ShopSlot.shopRef` 与 `ShopCategoryToggles.shopRef` 已在场景与预制体中接线，`Awake` 里的 `LogError` 因此不会触发。
- `ShopManager.shopSlots` 的数组长度覆盖三类商品列表的最大长度，`PopulateShopItems` 不会截断。
- `Bow` 的 `stackableSize` 取 0 是设计意图，代表不可拾取的展示物。
- 商店出售按商店槽位价格成交是当前设计意图。
