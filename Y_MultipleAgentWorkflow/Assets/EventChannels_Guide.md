# 事件通道资产权威指南：SO 事件通道的资产侧契约与用法

文档 ID：`ASSETS-EVENTCHANNELS-GUIDE`
状态：`Active`
最后更新：`2026-11-01`
权威范围：本文件只对事件通道这一资产的清单、类型契约、订阅与发布惯例、命名与接线检查清单负责。事件总线机制与 MVC 及服务层的边界属 Architecture 域，场景、存档、任务等业务语义属各自 Gameplay 域。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `事件通道` / `EventSO` / `事件SO` | §2.1 清单、§2.2 类型契约 |
| `新增事件` / `新建通道` | §2.6 新增标准动作、§3.3 检查清单 |
| `改名事件` / `重命名事件资产` | §3.3 检查清单 |
| `VoidEventSO` / `OnEventRaised` | §2.1 清单、§2.2 类型契约 |
| `ToggleCanvasEventSO` / `canvasToToggle` | §2.5 内部契约、§3.2 canvasToToggle 必须唯一 |
| `SceneLoadedEvent` / `SceneLoadEvent` | §2.1 清单、§2.4 发布点 |
| `订阅` / `OnEnable +=` / `OnDisable -=` | §2.4 订阅与发布惯例、§3.1 订阅必须成对 |
| `Raise` / `OnEventRaised` / `发布` | §2.2 发布方法命名、§2.4 发布惯例 |
| `事件不生效` / `接线断了` / `Inspector 没拖` | §3.3 检查清单 |
| `事件资产在哪` / `Events 目录` | §2.3 事件资产的组织方式 |
| `InventorySlotsStatsSO` | §2.1 清单、§2.3 组织方式 |

## 2. 当前实现

### 2.1 清单：13 个通道类，29 个通道资产

13 个通道类集中在 Pipeline 层的 SO 事件目录：

| # | 类 | 对外事件 | 发布方法 | 资产数 |
|---|---|---|---|---|
| 1 | `VoidEventSO` | `Action VoidEvent` | `OnEventRaised()` | 6 |
| 2 | `ToggleCanvasEventSO` | `Action<bool> toggleCanvasEvent`、`Action focusEvent` | `RaiseToggleCanvasEvent(bool)`、`RaiseFocusEvent()` | 10 |
| 3 | `InventorySlotsStatsSO` | `Action<ItemSO,int,int> InventoryUpdateRequestEvent` | `RaiseInventoryUpdateRequest(...)` | 3 |
| 4 | `SceneLoadEventSO` | `Action<List<GameSceneSO>,Vector3,bool> LoadRequestEvent` | `RaiseLoadRequestEvent(...)` | 1 |
| 5 | `SceneLoadedEventSO` | `Action<GameSceneSO> SceneLoadedEvent` | `RaiseSceneLoadedEvent(...)` | 1 |
| 6 | `DataSaveEventSO` | `Action<SaveType> DataSaveEvent` | `RaiseDataSaveEvent(...)` | 1 |
| 7 | `LoadQuestEventSO` | `Action<List<QuestSO>> LoadQuestEvent` | `OnLoadQuestEventRaised(...)` | 1 |
| 8 | `LootEventSO` | `Action<ItemSO,int,Loot> LootEvent` | `OnEventRaised(...)` | 1 |
| 9 | `QuestOptionsEventSO` | `Action<QuestState> questOptionsEvent` | `OnQuestOptionsEventRaised(...)` | 1 |
| 10 | `ShopKeeperEventSO` | `Action<ShopKeeper> ShopKeeperEntered / Exited` | `RaiseShopKeeperEntered/Exited(...)` | 1 |
| 11 | `PlayerDamagedEventSO` | `Action<int,Transform,float,float> PlayerDamaged` | `OnPlayerDamaged(...)` | 1 |
| 12 | `EnemyDefeatedEventSO` | `Action<int,Transform> EnemyDefeated` | `OnEnemyDefeated(...)` | 1 |
| 13 | `CanvasOpenedEventSO` | `Action<CanvasToToggle> CanvasOpened` | `RaiseCanvasOpened(...)` | 1 |
| | **合计** | | | **29** |

13 个类里有 3 个是多实例：`ToggleCanvasEventSO` 占 10 个资产、`VoidEventSO` 占 6 个、`InventorySlotsStatsSO` 占 3 个，其余 10 个类各 1 个。

### 2.2 类型契约

**13 个类每一个都直接继承 `ScriptableObject`，没有公共基类、没有公共接口**，`VoidEventSO`、`SceneLoadedEventSO`、`ShopKeeperEventSO`、`PlayerDamagedEventSO` 都写作 `: ScriptableObject`；`GuidSO` 是另一条无关的继承线，事件通道不使用它。

- 事件字段一律是 `public event Action<...>`，未标注 `[SerializeField]`，因此委托链不序列化，资产文件里只有 `m_Script` 与类内配置字段，看不到任何订阅者。
- 泛型参数只引用 `ItemSO`、`QuestSO`、`GameSceneSO`、`Loot`、`ShopKeeper` 与 `Contracts` 里的枚举（`SaveType`、`QuestState`、`CanvasToToggle`）；通道类不引用任何 Manager、Controller 或 View。
- 发布方法命名分两类：`ToggleCanvasEventSO`、`ShopKeeperEventSO`、`SceneLoadEventSO` 等用 `Raise*`；`VoidEventSO.OnEventRaised`、`LootEventSO.OnEventRaised`、`LoadQuestEventSO.OnLoadQuestEventRaised`、`PlayerDamagedEventSO.OnPlayerDamaged` 用 `On*Raised` 或 `On*`。新增通道时照抄同族命名。
- 每个资产实例是独立通道：同一个类建两个资产就是两条互不相干的广播线，`VoidEventSO` 的 6 个资产各管一件事即此用法。

### 2.3 事件资产的组织方式

- 单一用途的通道直接放 Events 目录根：`DataSavedEvent`、`EnemyDefeatedEvent`、`LoadQuestEvent`、`LootEvent`、`PlayerDamagedEvent`、`QuestOptionsEvent`、`SceneLoadEvent`、`SceneLoadedEvent`、`ShopKeeperEvent`，各 1 个，共 9 个。
- 一个类有多个实例时按类归子目录：`ToggleCanvasEvents` 下 10 个 `Toggle<后缀>Event`，`VoidEvents` 下 6 个语义命名资产，`InventorySlotsStatsEvents` 下 3 个。
- 命名不跟随类名：`ToggleESCEvent`、`RetryRequestEvent`、`DataSavedEvent`；其中 `DataSavedEvent` 的资产名是 `DataSaved`，类与方法是 `DataSave` 与 `RaiseDataSaveEvent`。
- `VoidEventSO` 资产的语义全靠命名，源码注释把它定为硬规则：资产名必须含语义前缀，禁止再创建无语义的资产名。

### 2.4 订阅与发布惯例

**订阅必须成对。** 场景组件通常在 `OnEnable` 订阅、`OnDisable` 退订；纯 C# Controller 在构造与 `Dispose` 中成对处理。缓存界面的开关与焦点事件由 `UIViewCache.Connect/Disconnect` 承接，并随 `UICacheManager` 启停；View 停用时开关入口仍然有效。`OpenOnToggleEvent` 控制是否直接响应打开，对话与任务先由业务入口准备数据再打开；缓存始终接收关闭请求。

代表写法，取字段、`+=` 与同名 `-=` 一次可见：

```csharp
// SaveDataManager 的成对订阅
private void OnEnable()  { sceneLoadEventSO.LoadRequestEvent += OnAutoSave; ... }
private void OnDisable() { sceneLoadEventSO.LoadRequestEvent -= OnAutoSave; ... }
```

**发布侧**只调通道方法，不关心谁在听。代表发布点：

| 通道 | 发布点 |
|---|---|
| `SceneLoadEvent` | `SceneChanger` |
| `SceneLoadedEvent` | `SceneChanger` |
| ToggleCanvas 10 个 | `UIManager`，方法为 `RaiseToggleCanvasEvent`、`RaiseFocusEvent`，切场景时复位全部关闭 |
| `DataSavedEvent` | `SaveDataManager` |
| `LootEvent` | `Loot` |
| `InventorySlotsStats` | `ShopManager`、`QuestManager` |
| `ShopKeeperEvent` | `ShopKeeper` |
| `PlayerDamagedEvent` | `EnemyCombat` |
| `EnemyDefeatedEvent` | `EnemyHealth` |
| `LoadQuestEvent` 与 `OpenQuestBoardEvent(Void)` | `QuestBoardManager` |
| `QuestOptionsEvent` | `QuestOptionsButton` |
| `RetryRequestEvent(Void)` | 发布 `RetryButton`，订阅 `PlayerDamageController` |
| `Slash` 与 `ShootActionFinishedEvent(Void)` | `PlayerCombat`、`PlayerBow`、`ShiftEquipment` |

### 2.5 ToggleCanvasEventSO 的内部契约

`ToggleCanvasEventSO` 是 13 个类里唯一带配置字段的，字段是 `public CanvasToToggle canvasToToggle`。它同时持有两条事件：`toggleCanvasEvent` 负责显隐，`focusEvent` 只调排序优先级，源码注释说明二者分离，用独立事件传达置顶与降级。

消费侧靠枚举匹配配对：

```csharp
// UIManager 的枚举匹配查找
private ToggleCanvasEventSO FindToggleEvent(CanvasToToggle canvas) {
    var cachedEvent = viewCache?.GetToggleEvent(canvas);
    if (cachedEvent != null) return cachedEvent;
    foreach (var eventSO in toggleCanvasEvents)
        if (eventSO != null && eventSO.canvasToToggle == canvas) return eventSO;   // 取第一个匹配
    return null;
}
```

`UICacheManager.views` 与 `UIManager.toggleCanvasEvents` 共同提供枚举到事件的登记。缓存注册表优先，`toggleCanvasEvents` 随后；`IsClosableCanvas` 使用同一查找。属性面板与技能、商店、对话、任务、背包六块界面登记在 `views`；其余画布登记在 `toggleCanvasEvents`。

10 个 toggle 资产映射到 **10 个互不重复**的 `CanvasToToggle` 值。`CanvasToToggle` 共 11 个成员，唯一无资产的是 `Default`。

| 资产 | `canvasToToggle` |
|---|---|
| `ToggleESCEvent` | 字段缺失，反序列化为 `0` = `ESC` |
| `ToggleGameOverEvent` | 1 `GameOver` |
| `ToggleStatsEvent` | 2 `Stats` |
| `ToggleSkillsEvent` | 3 `Skills` |
| `ToggleDialogEvent` | 4 `Dialog` |
| `ToggleQuestEvent` | 5 `Quest` |
| `ToggleShopEvent` | 6 `Shop` |
| `ToggleIntegratedEvent` | 7 `Integrated` |
| `ToggleBackpackEvent` | 8 `Backpack` |
| `ToggleSaveLoadEvent` | 9 `SaveLoad` |

### 2.6 新增一个事件通道的标准动作

1. 新建类时把它放在事件通道目录，`[CreateAssetMenu(menuName = "Events/<类名>")]`，包一层 `public event Action<...>` 加一个发布方法，照抄同族命名风格，见 §2.2。
2. 在 Events 目录或多实例子目录右键 `Create > Events > ...` 建资产，随即改名；默认文件名是类名，`VoidEventSO` 必须补语义前缀。
3. 发布方用 `[SerializeField]` 拖该资产；订阅方在 `OnEnable` 与 `OnDisable` 成对 `+=` 与 `-=`；两侧都拖到同一个资产，见 §3.3。
4. 画布开关通道要给资产设 `canvasToToggle`。场景画布将事件登记进 `UIManager.toggleCanvasEvents`；缓存界面把带该事件的 `Contracts.CachedCanvasView` 预制体登记进 `UICacheManager.views`。

## 3. 约定与硬边界

### 3.1 订阅必须随对象或打开状态成对管理

`ScriptableObject` 资产的生命周期比场景对象长。只订阅不退订会保留已销毁实例，重复订阅会让一次广播触发多次回调。场景组件使用 `OnEnable/OnDisable`，Controller 使用构造与 `Dispose`，缓存 View 使用 `OnOpen/OnClose`，缓存服务使用 `Connect/Disconnect`。

另一套机制是静态事件：`SkillSlot` 用的是 `public static event`，由 `SkillTreeManager` 成对订阅，静态事件跨场景存活。新增 SO 通道订阅时照抄 SO 通道的成对写法。

### 3.2 canvasToToggle 是隐式身份，必须唯一

同一个 ID 只对应一份事件资产和显示实例。旧列表取第一个匹配项，缓存注册表拒绝重复 ID；两处同时出现时缓存定义优先。当前 10 个资产值互不重复。新增画布先在 `Default` 之前增加枚举成员，再创建事件资产，最后登记到事件列表或预制体列表。

### 3.3 新增与改名事件通道的检查清单

**改名或移动任一事件资产前，先按此表核对该通道的全部落点。** 资产引用按 **GUID** 存储，在 Unity 内重命名或移动不会断引用；绕过 Unity 改名、重新 Create 一个资产替换旧的、或改了资产的 GUID，全部引用会失效，订阅方在 `OnEnable` 里拿到 null 或抛空引用，编译期无任何提示。

| # | 检查项 | 排查方式 |
|---|---|---|
| 1 | 通道类本身：事件名、发布方法签名 | 通道类源码 |
| 2 | 发布方：`[SerializeField]` 拖了该资产 | 按该资产 GUID 检索场景、预制体与资产 |
| 3 | 订阅与退订成对 | 按 §2.4 核对组件、Controller、缓存 View 和服务的生命周期 |
| 4 | 场景与预制体里的资产引用 | 按 GUID 计数，改名后引用数不应为 0 |
| 5 | toggle 通道：`canvasToToggle` 值唯一，且已登记事件或缓存预制体 | `CanvasToToggle` 与 `UIManager` |
| 6 | 常驻场景内管理器持有的通道 | 确认它是常驻场景 `PersistentScene` 里的对象 |
| 7 | 改完后按 GUID 复算引用数，确认与改前一致 | 对全部场景、预制体与资产做 GUID 匹配计数 |

字段改名会让 Inspector 引用变空：Unity 按字段名反序列化，编译能过、运行时无广播。
