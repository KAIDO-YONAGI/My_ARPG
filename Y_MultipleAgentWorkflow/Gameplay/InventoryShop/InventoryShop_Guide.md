# 背包与商店权威指南：Gameplay.InventoryShop

文档 ID：`GP-INVENTORYSHOP-GUIDE`
状态：`Active`
最后更新：`2026-11-02`
权威范围：本域负责背包槽位数据与堆叠、拾取与丢弃、物品使用、金币与商店买卖这条链路；玩家数值域的内部规则归 `Gameplay.PlayerStats`，掉落物存档格式的内部规则归存档域，任务与对话如何发起奖励归各自域。

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
| `ShopKeeper` / `商店不打开` | §2.5、§2.6 |

## 2. 当前实现

### 2.1 背包数据结构与堆叠规则

- `InventoryManager` 的 `State` 持有金币与快捷栏、背包两组 `InventorySlotState` 列表，各自独立追踪脏标记。视图通过稳定的 `CurrentState` 对象读取金币、槽位数量和按索引取得的槽位值；列表本身不公开，也不为刷新复制集合或创建快照。快捷栏和背包的标记、清标记方法均由 `State` 提供。入包先遍历快捷栏再遍历背包，各组顺序由视图的序列化槽位数组决定。
- `InventorySlot` 保存物品与数量的显示副本，并以 `IsBackpackSlot`、`SlotIndex` 标记槽位地址。快捷栏在首次初始化时配置地址，背包每次打开时为当前实例配置地址。
- 堆叠上限来自 `ItemSO.stackableSize`，`AddToSlots` 按顺序填充空槽或同类物品槽位，返回剩余数量；`HasSpaceForItem` 检查两组列表的剩余空间。
- `RemoveFromSlot` 在数量归零时清空物品引用并标记对应列表。视图按索引查询对应槽位，再通过 `RestoreState` 更新控件；首次初始化接收视图初始配置，缓存重建沿用管理器中的数据。
- 背包内容与金币都不入档：`SaveData` 顶层只有 `lootsStatsDic`、`sceneIDAndPlayerPos`、`playerStatsData` 三个字段，`InventoryManager` 未实现 `ISaveable`。组件挂在常驻场景上，因此一次运行内跨游戏场景保留，读档后回到本局的运行值。

### 2.2 拾取、丢弃、使用三条入口

- 拾取：`Loot.OnTriggerEnter2D` 判定 `Player` 标签与 `canBePick`，播 `Pickup` 动画并广播 `lootEvent.OnEventRaised(item, quantity, this)`，由 `InventoryManager.OnItemLootedHandler` 接手并调 `UpdateInventorySlots`。
- 丢弃：右键槽位触发 `InventoryManager.DropByClick`，管理器按槽位地址取物品，先 `DropLoot` 再 `RemoveFromSlot` 扣减。掉落物通过 `ObjectPool<Loot>` 取得，池耗尽时实例化兜底，随后移到当前场景。池的预热、扩容与实例化兜底均使用 `InventoryManager.lootPrefab` 引用的资产；常驻场景中的该引用指向 `Gold.prefab`。
- 使用：左键槽位且商店未打开时调 `InventoryManager.UseItem`，按槽位地址取物品，执行效果后扣减状态并更新物品史。
- 拾取余量：`UpdateInventorySlots` 放完所有槽位后仍有 `quantity > 0` 时，用 `DropLoot` 把余量掉在玩家脚下；来源是同一个 `Loot` 对象时走原地重掉分支，位移到玩家位置、`Initialize`、重新激活并延时恢复可拾取。
- 池复用复原：`Pickup` 片段驱动子物体 `Sprite` 的透明度、排序层级、缩放与位置。`Loot.Initialize` 先启用 Animator、清除拾取参数，调用 `Animator.Play` 切回 `Idle` 并通过 `Animator.Update(0f)` 完成求值，然后停用 Animator，最后按 `CaptureVisualBaseline` 记录的静止状态写回显示属性并启用 SpriteRenderer。当前 `Idle` 使用空的 Blend Tree，静止外观由脚本管理，避免后续动画求值再次覆盖复原值；下次拾取时重新启用 Animator 并从头播放 `Pickup`。实际掉落模板是常驻场景 `InventoryManager.lootPrefab` 引用的 `Gold.prefab`，其 Animator 已开启禁用时写回默认值。

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
- 缓存接收商店开关事件，`ShopCanvasView.CanOpen` 检查店主。打开后管理器装载商品状态并标脏，视图逐槽显示当前分类的商品，多余槽位隐藏。
- 买入：`ShopSlot.OnBuyButtonClick` 调 `ShopManager.TryBuyItem`，后者发布正价格与正数量请求。
- 卖出：左键物品槽位且商店打开时，先 `SetSlotBeenClicked` 保存地址，再 `ShopManager.SellItem` 按当前分类的物品引用匹配报价，发布负价格与负数量请求。
- 结算集中在 `InventoryManager.HandleShopping`：`goldAmount` 小于 `price` 时直接返回；买入在 `HasSpaceForItem` 通过后先 `UpdateGold(price)` 再 `UpdateInventorySlots(item, amount)`；卖出先 `UpdateGold(price)` 再 `UpdateInventorySlots(item, amount)`。
- 金币的写入点是 `UpdateGold` 与 `UpdateInventorySlots` 的 `isGold` 分支，两处只修改管理器内部的金币值并置脏；`HotbarCanvasView` 独立读取 `State.GoldAmount` 刷新文本，快捷栏槽位只在快捷栏脏时按索引刷新。
- 货币来源：`goldAmount` 初值为 0，增加路径是拾取或奖励 `isGold` 物品时的 `goldAmount += quantity`，以及出售时经 `UpdateGold` 以负价格反向增加；金币没有其它增加入口。
- 商店不消耗库存：`TryBuyItem` 只发事件，商店侧没有数量扣减与售罄处理。

### 2.6 订阅惯例与画布

- 事件通道都是 `ScriptableObject` 加 C# `event`，订阅成对写在 `OnEnable` 与 `OnDisable`；场景加载事件统一用来复位画布。缓存界面的开关与焦点事件由 `UIViewInstanceCache.Connect/Disconnect` 统一订阅。
- 背包与商店分别由 `BackpackCanvasView`、`ShopCanvasView` 承担，预制体登记在 `UICacheManager.views`。缓存统一管理实例、开关和焦点，业务管理器持有运行状态。
- 面板内部控件由 `ShopCanvasView` 注入数据：`ShopSlot` 与 `ShopCategoryToggles` 不持 `shopRef`，也不做 `IShopInteractable` 转换；`ShopManager.State` 通过商品数量、按索引查询商品与价格以及脏标记方法供视图读取，不创建商品快照。
- `InventorySlot` 直接走 `ShopManager.Instance`。
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

### 3.1 `stackableSize` 必须 ≥ 1

取 0 的物品进不了背包：`AddItem` 与 `SpaceRemaining` 都以它算容量，此时 `HasSpaceForItem` 恒为 false。

### 3.2 空槽 `itemSO` 必须是 null

`IsEmpty` 与 `SpaceRemaining` 的分支都依赖它；手改场景槽位时把数量留 0 又留着 `itemSO`，会在 `UpdateUI` 里被清空。

### 3.3 出售必须先 `SetSlotBeenClicked`

`UpdateInventorySlots` 的负数分支只认这个字段，未设置时只打一条 `Debug.Log("No slot been Marked")`，随后保留物品。

### 3.4 金币与槽位分别标脏

管理器写入金币后由常驻 `HotbarCanvasView` 读取 `State.GoldAmount` 并更新文本；金币变化只触发金币显示，快捷栏与背包按各自的 State 脏标记和索引查询刷新。

### 3.5 `isGold` 与 `isEXP` 分支先于数量正负判断

这两类物品走加钱或加经验并 `return` 的路径，不参与槽位增删。

### 3.6 交易失败静默

金币不足或背包无空间时 `HandleShopping` 直接返回，没有 UI 反馈，也没有事件。

### 3.7 背包与金币不入档

`SaveData` 的字段不含它们，域内唯一实现 `ISaveable` 的对象是 `Loot`；读档后回到本局运行值。

### 3.8 槽位顺序即优先级

管理器先填快捷栏再填背包；各组内的优先级由视图序列化槽位数组的顺序决定。

### 3.9 池复用的掉落物必须经 `Initialize` 复原显示状态

掉落入口在取得池对象、设置位置并移入目标场景后调用 `Initialize`。该方法设置本次物品与数量，先完成动画复位并停用 Animator，再恢复显示属性与贴图并登记存档。再次拾取时才重新启用 Animator。预热通过实例化后立即停用对象入队完成，首次借出与池空后扩容分别经过重新激活和直接使用新实例两条路径。
