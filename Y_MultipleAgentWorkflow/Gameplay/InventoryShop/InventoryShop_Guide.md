# 背包与商店（Gameplay.InventoryShop）权威指南

文档 ID：`GP-INVENTORYSHOP-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只对「背包槽位数据/堆叠、拾取与丢弃、物品使用、金币与商店买卖」这条链路负责；玩家数值域的内部规则归 `Gameplay.PlayerStats`，掉落物存档格式的内部规则归存档域，任务/对话如何发起奖励归各自域。
上游来源：
- `Assets/Scripts/Gameplay/Inventory/{InventoryManager,InventorySlot,UseItem,Loot,BackpackCanvasManager}.cs`
- `Assets/Scripts/Gameplay/Shop/{ShopManager,ShopSlot,ShopInfoPanel,ShopCategoryToggles,ShopPortraitCamera}.cs`
- `Assets/Scripts/Pipeline/SO/ItemSO.cs`、`Assets/Scripts/Pipeline/SO/Events/{InventorySlotsStatsSO,LootEventSO}.cs`
- `Assets/Scripts/Gameplay/Dialog/HistoryManager/ItemHistoryManager.cs`、`Assets/Scripts/Gameplay/Units/ShopKeeper/ShopKeeper.cs`
- 旧文档线索：`Docs/My_ARPG_MVCS项目现状.md` §2（109、122 行）、`Docs/游戏指南.txt`（33-35 行）

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

- 背包没有独立的数据类。槽位就是场景里的 `InventorySlot` 组件，`InventoryManager.Start` 用两次 `GetComponentsInChildren` 把 `hotbarParent` 与 `backpackParent` 的子槽位拼成一个运行时 `List<InventorySlot>`（`Assets/Scripts/Gameplay/Inventory/InventoryManager.cs:41-42`）。**顺序决定优先级**：先快捷栏、后背包，填充永远从快捷栏开始（:129-146）。
- 槽位私有状态只有两个字段：`itemSO` 与 `quantity`，对外只读暴露 `ItemSO`、`Quantity`、`IsEmpty`（`InventorySlot.cs:7-15`）。
- 堆叠上限来自 `ItemSO.stackableSize`（`Assets/Scripts/Pipeline/SO/ItemSO.cs:16`）。放入逻辑在 `AddItem`：同物品时上限是 `stackableSize - quantity`，空槽时上限是 `stackableSize`，返回实际放入数量（`InventorySlot.cs:51-64`）。`SpaceRemaining` 与 `AddItem` 使用同一套上限（:88-93），购买前的容量检查走它（`InventoryManager.cs:153-161`）。
- 移除逻辑在 `RemoveItem`：数量归零时把 `quantity` 归 0 并清空 `itemSO`，维持「空槽 `itemSO` 必为 null」的不变量（`InventorySlot.cs:70-83`）。`UpdateUI` 也会在 `quantity <= 0` 时兜底清空 `itemSO`（:95-100）。
- 背包内容与金币都不入档：`SaveData` 顶层只有 `lootsStatsDic`、`sceneIDAndPlayerPos`、`playerStatsData` 三个字段（`Assets/Scripts/Gameplay/Save/SaveData.cs:9-11`），`InventoryManager` 未实现 `ISaveable`（`InventoryManager.cs:8`）。组件挂在 `PersistentScene.unity`（该场景引用其脚本 guid `5ae0da3ae8a33ac4f8b902811d4ff529`），因此一次运行内跨游戏场景保留，读档不恢复。

### 2.2 拾取、丢弃、使用三条入口

- 拾取：`Loot.OnTriggerEnter2D` 判定 `Player` 标签且 `canBePick`，播 `Pickup` 动画并广播 `lootEvent.OnEventRaised(item, quantity, this)`（`Loot.cs:164-173`）→ `InventoryManager.OnItemLootedHandler`（:73-76）→ `UpdateInventorySlots`。
- 丢弃：右键槽位 → `InventoryManager.DropByClick`，先 `DropLoot(ItemSO, 1)` 再 `slot.RemoveItem(1)`（`InventoryManager.cs:210-215`）。`DropLoot` 走 `ObjectPool<Loot>`；池满（`ObjectPool.Get` 返回 null，`Assets/Scripts/Pipeline/ObjectPool.cs:55-76`）时一次性实例化兜底并手工补一次 `OnPoolGet`（`InventoryManager.cs:178-195`）。掉落物必须经 `SceneManager.MoveGameObjectToScene` 挂到当前场景根，因此 `Loot.OnPoolGet` 里先 `transform.SetParent(null)`（`Loot.cs:53-60`）。
- 使用：左键槽位且商店未打开时调 `InventoryManager.UseItem`（`InventorySlot.cs:23-38`）→ `useItem.ApplyItemEffects` → `slot.RemoveItem(1)` → 写物品史 `-1`（`InventoryManager.cs:218-227`）。
- 拾取剩余量处理：`UpdateInventorySlots` 放完所有槽位后仍有 `quantity > 0`，就 `DropLoot` 把余量掉在玩家脚下（`InventoryManager.cs:148-149`）；若来源是同一个 `Loot` 对象，走「原地重掉」分支：位移到玩家位置、`Initialize`、重新激活并延时恢复可拾取（:164-172、:197-205）。

### 2.3 物品使用对数值域的写命令

`UseItem.ApplyItemEffects` 与延时器 `EffectTimer` 一共**8 个调用点**，覆盖 4 个写命令各两次：`UpdateMaxHealth`、`UpdateHealth`、`UpdateSpeed`、`UpdateDamage`（`Assets/Scripts/Gameplay/Inventory/UseItem.cs:11-18`、:28-39）。所有写入都经 `StatsService`，数值规则（钳制、事件）仍在 `PlayerStatsModel`（`Assets/Scripts/Gameplay/Player/Services/StatsService.cs:40-46`；`Models/PlayerStatsModel.cs:97-126`）。

另外两个跨域写点：拾取 `isEXP` 物品调 `StatsService.AddExperience(quantity)`（`InventoryManager.cs:108-112`），任务奖励的金币/经验也复用同一分支（:68-71）。

### 2.4 物品史的写入点

`ItemHistoryManager` 是内存字典 `Dictionary<ItemSO,int>`，另有 `HasPickedOverAmount`/`GetItemQuantity` 给对话与任务读取，归零不删除条目（`Assets/Scripts/Gameplay/Dialog/HistoryManager/ItemHistoryManager.cs:9-36`）。写命令 `RecordItem` 在 `InventoryManager` 里共有 **4 处**：

| 位置 | 场景 | 代码 |
|---|---|---|
| 拾取/奖励到账的金币 | `+quantity` | `InventoryManager.cs:99-107` |
| 出售扣减 | `-removed`（实际移除量） | `InventoryManager.cs:120-125` |
| 拾取/购买入包 | `+placed`（每槽依实际放入量写一次） | `InventoryManager.cs:133-138` |
| 使用物品 | `-1` | `InventoryManager.cs:225` |

### 2.5 商店：买入、卖出与货币来源

- 商品数据由 `ShopKeeper` 持有，分三类列表 `shopItems`/`shopWeapon`/`shopArmor`，元素是 `ShopItems{item, price}`（`Assets/Scripts/Gameplay/Units/ShopKeeper/ShopKeeper.cs:12-18`；`ShopManager.cs:166-171`）。
- 玩家进出范围 → `ShopKeeperEventSO` → `ShopManager.OnKeeperEntered/Exited` 记录 `activeShopKeeper`；离开时若商店开着就 `CloseShop`（`ShopManager.cs:60-71`；`ShopKeeper.cs:24-47`）。
- 开关 → `toggleShopCanvasEvent.toggleCanvasEvent` → `OnShopToggle`，打开时用 `activeShopKeeper` 的三份列表 `OpenShop`，随后 `PopulateShopItems` 逐槽 `Initialize(item, price)`，多余槽位 `SetActive(false)`（`ShopManager.cs:73-108`、:116-131）。
- 买入：`ShopSlot.OnBuyButtonClick` → `IShopInteractable.TryBuyItem` → `InventoryUpdateRequest.RaiseInventoryUpdateRequest(item, price, 1)`（`ShopSlot.cs:52-58`；`ShopManager.cs:133-136`；`Assets/Scripts/Pipeline/SO/Events/InventorySlotsStatsSO.cs:16-19`）。
- 卖出：左键背包槽位且 `ShopManager.Instance.IsShopOpen` 为真 → 先 `SetSlotBeenClicked(this)` 再 `ShopManager.Instance.SellItem(itemSO)`（`InventorySlot.cs:27-33`）。`SellItem` 在当前 `shopSlots` 里找同名 `ItemSO`，命中后用**负价格、负数量**发事件（`ShopManager.cs:137-149`）。
- 结算集中在 `InventoryManager.HandleShopping`（`InventoryManager.cs:77-93`）：`goldAmount < price` 直接返回；买（`amount > 0`）先查 `HasSpaceForItem` 再 `UpdateGold(price)` + `UpdateInventorySlots(item, amount)`；卖（`amount < 0`）先 `UpdateGold(price)` 再 `UpdateInventorySlots(item, amount)`。金币有**两个**写入点：`UpdateGold`（`goldAmount -= price` 并刷新 `goldAmountText`，:228-232）与 `UpdateInventorySlots` 的 `isGold` 分支（`goldAmount += quantity` 并刷新文本，:99-107）。
- 货币来源：`goldAmount` 初值为 0（无序列化初始值，:25），只由两条路增加——拾取/奖励 `isGold` 物品时 `goldAmount += quantity`（:99-107），以及出售时经 `UpdateGold(负价)` 反向增加（:88-91）。没有其它发钱入口。
- 商店不消耗库存：`TryBuyItem` 只有发事件，没有数量扣减或售罄逻辑（`ShopManager.cs:133-136`）。

### 2.6 订阅惯例与画布

- 事件通道都是 `ScriptableObject` + C# `event`，订阅一律成对写在 `OnEnable`/`OnDisable`：`InventoryManager` 订 3 条（`InventoryManager.cs:51-66`），`ShopManager` 订 4 条（`ShopManager.cs:37-53`），`BackpackCanvasManager` 订 3 条（`BackpackCanvasManager.cs:13-25`）。场景加载事件统一用来复位画布（`ShopManager.cs:55-58`、`BackpackCanvasManager.cs:27-30`）。
- 面板显隐走 `ICanvasManager` 的默认实现，并把状态上报 `UIManager`（`Assets/Scripts/Contracts/ICanvasManager.cs:37-100`）。`ShopManager` 与 `BackpackCanvasManager` 都实现该接口，并把 `ToggleCanvasEvent`、`SceneLoadedEvent` 暴露出去供 `UIManager` 注册（:5-16）。
- 面板内部控件不用单例：`ShopSlot`、`ShopCategoryToggles` 用 `[SerializeField] Component shopRef` + `as IShopInteractable` 拿父级商店（接口无法序列化），空引用时 `Debug.LogError`（`ShopSlot.cs:29-34`；`ShopCategoryToggles.cs:16-21`）。唯一的例外是 `InventorySlot` 直接走 `ShopManager.Instance`，`IShopInteractable.cs:6` 的注释把这条记为「暂留单例访问」。
- 商店头像相机订阅同一 `ShopKeeperEventSO`，无 keeper 时按 `hideWhenNoShopKeeper` 关闭 Camera，`LateUpdate` 跟目标偏移（`ShopPortraitCamera.cs:34-52`）。
- 悬停信息面板只读 `ItemSO` 的展示字段与五个数值字段，负数/零不显示（`ShopInfoPanel.cs:23-51`）。

### 2.7 ItemSO 作为共同配置类型

`ItemSO` 是背包与商店唯一共同消费的配置类型（`Assets/Scripts/Pipeline/SO/ItemSO.cs:7-25`）：`itemName`/`itemDescription`/`icon` 负责展示，`isGold`/`isEXP` 决定是否走背包槽位（`InventoryManager.cs:99-112`），`stackableSize` 决定堆叠（`InventorySlot.cs:56`），`currentHealth`/`maxHealth`/`speed`/`damage`/`duration` 决定使用效果（`UseItem.cs:11-20`）。它同时被商店（`ShopManager.cs:169`）、任务（`Assets/Scripts/Pipeline/SO/QuestSO.cs:19,26`）、对话（`Assets/Scripts/Pipeline/SO/DialogSO.cs:36`）引用，四个域共享同一份资产。

现有资产（`Assets/GameSO/ItemSO/`）：`Gold`（`isGold: 1`，`stackableSize: 0`，:18-20）、`EXP`（`isEXP: 1`，`stackableSize: 10000000`，:19-20）、`Mushroom`（`stackableSize: 5`，`currentHealth/maxHealth/speed: 2`、`duration: 2`，:19-24）、`Meat`（`stackableSize: 3`，:19-24）、`Wood`（`stackableSize: 10`，:19）、`Bow`（`stackableSize: 0`，:19）。

## 3. 约定与硬边界

1. **`stackableSize` 必须 ≥ 1**，否则该物品永远进不了背包：`AddItem` 与 `SpaceRemaining` 都以它算容量，`0` 时 `HasSpaceForItem` 恒为 false（`InventoryManager.cs:153-161`）。现存反例 `Bow.asset:19`（=0），见 §4。
2. **空槽 `itemSO` 必须是 null**：`IsEmpty`、`SpaceRemaining` 的分支都依赖它；手改场景槽位时把数量留 0 又留着 `itemSO` 会在 `UpdateUI` 里被清空（`InventorySlot.cs:95-100`），不要绕过。
3. **出售必须先 `SetSlotBeenClicked`**：`UpdateInventorySlots` 的负数分支只认这个字段，未设置时只打一条 `Debug.Log("No slot been Marked")` 然后**静默不删物品**（`InventoryManager.cs:114-125`）。直接从别处调 `SellItem` 会留下语义缺口。
4. **金币只有 `UpdateGold` 与 `isGold` 分支两个写入点，两处都必须同步 `goldAmountText`**，文本是硬引用，为空即抛 `NullReferenceException`（`InventoryManager.cs:99-107`、:228-232）。
5. **`isGold` / `isEXP` 分支先于数量正负判断**：这两类物品走的是「加钱/加经验并 return」，不参与槽位增删（`InventoryManager.cs:99-112`）。任何新的负数量交易都会先撞上这两个分支。
6. **交易失败静默**：金币不足或背包无空间时 `HandleShopping` 直接返回，没有 UI 反馈、没有事件（`InventoryManager.cs:79-87`）。
7. **背包/金币不入档**：`SaveData` 三个字段里没有背包或金币（`SaveData.cs:9-11`），域内唯一实现 `ISaveable` 的对象是 `Loot`（`Loot.cs:7`）。读档后背包内容与金币回到本局运行值。
8. **槽位顺序即优先级**：`Start` 里 hotbar 先于 backpack 拼接（`InventoryManager.cs:41-42`），改动层级会改变入包顺序与「背包满」的表现。

## 4. 已知缺陷与风险

1. **出售 `isEXP` 道具：加钱、留物、经验被拦。** 走在 `UpdateInventorySlots` 的 `isEXP` 分支（`InventoryManager.cs:108-112`）并在扣金币之后，因此出售金币照样进账；`return` 跳过了通用出售分支，道具留在背包里；`PlayerStatsModel.AddExp` 忽略非正数只留警告（`PlayerStatsModel.cs:135-141`）。旧文档 D1 与代码一致（`Docs/My_ARPG_MVCS项目现状.md:120-124`）。
2. **出售 `isGold` 道具会倒扣金币且不删物。** 同一路径下 `goldAmount += quantity`（`quantity = -1`）→ 金币减 1 并 `return`（`InventoryManager.cs:99-107`）。与缺陷 1 同源、后果更反直觉。
3. **`stackableSize = 0` 的物品拾取即「原地弹回」。** `Bow.asset:19` 为 0：`AddItem` 返回 0 → 余量走 `DropLoot(item, quantity, lootObj)` 的原地重掉分支，把 loot 位移到玩家脚下并在 0.3×动画时长后恢复 `canBePick`（`InventoryManager.cs:140-149`、:164-172、:197-205），玩家站在物品上会反复触发拾取动画。
4. **`PopulateShopItems` 不清空多余槽位的数据。** 只 `SetActive(false)`，`ShopSlot.item`/`price` 保留上一个分类的旧值（`ShopManager.cs:126-130`；`ShopSlot.cs:43-50`）。`SellItem` 遍历的是整个 `shopSlots` 数组（含失活槽位），可能命中旧槽位并**按过期价格**成交（`ShopManager.cs:140-148`）。
5. **任务奖励溢出到地面。** `HandleQuestReward` 忽略 `price` 直接 `UpdateInventorySlots(item, amount)`（`InventoryManager.cs:68-71`；任务侧 `QuestManager.cs:179-187`），背包装不下时余量被 `DropLoot` 掉在玩家脚下，奖励变成可丢失的地面物。
6. **`UseItem`、`Loot` 池等序列化引用无空守卫**：`useItem` 为空时使用物品直接 NRE（`InventoryManager.cs:15,222`），`lootPrefab` 为空时 `ObjectPool` 构造即 NRE（:49）。
7. **`InventoryManager` 同时承担背包数据、金币、拾取、商店校验、掉落池、物品使用六类职责**，旧文档已把它列为「还没分层」的功能域（`Docs/My_ARPG_MVCS项目现状.md:109`）。
8. **本域没有回归网**：`Assets/Tests/Editor/` 下 5 个用例文件（`ObjectPoolTests`、`PlayerStatsSOTests`、`CanvasFocusStackTests`、`PlayerStatsModelTests`、`AStarOpenHeapTests`）没有一个覆盖 `InventoryManager`/`InventorySlot`/`UseItem`/`ShopManager`。

## 5. 未核验事项

- 假设：`InventoryManager.ShoppingRequest` 与 `ShopManager.InventoryUpdateRequest` 在 `PersistentScene` 中指向同一个 `InventorySlotsStatsSO` 资产，`QuestRewardRequest` 与 `QuestManager` 的引用同理（未运行 Unity 验证）。
- 假设：`InventoryManager`、`ShopManager`、`BackpackCanvasManager` 的 `PersistentScene` 实例在整局游戏内不卸载，因此背包内容与金币跨游戏场景保留（未运行 Unity 验证；仅确认脚本 guid 被 `PersistentScene.unity` 引用）。
- 假设：`hotbarParent`/`backpackParent` 下所有 `InventorySlot` 都处于激活状态（`GetComponentsInChildren` 默认跳过失活对象，未运行 Unity 验证）。
- 假设：`ShopSlot.shopRef`、`ShopCategoryToggles.shopRef` 在场景/预制体中已接线，因此不会打出 `Awake` 里的 `LogError`（未运行 Unity 验证）。
- 假设：`ShopManager.shopSlots` 数组长度足够覆盖三类商品列表的最大长度，否则 `PopulateShopItems` 静默截断（未运行 Unity 验证）。
- 假设：`ItemSO.stackableSize = 0` 的 `Bow` 是设计意图（不可拾取的展示物）而非配置遗漏（未运行 Unity 验证）。
- 假设：商店出售价格按商店槽位价格成交是当前设计意图（未运行 Unity 验证）。
