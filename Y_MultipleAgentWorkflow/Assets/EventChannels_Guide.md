# 事件通道资产权威指南（SO 事件通道的资产侧契约与用法）

文档 ID：`ASSETS-EVENTCHANNELS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本文件只对**事件通道（`Assets/Scripts/Pipeline/SO/Events/*.cs` + `Assets/GameSO/Events/**`）这一资产的清单、类型契约、订阅/发布惯例、命名与接线检查清单**负责。事件总线机制本身是否成立、事件与 MVC/服务层的边界属 Architecture 域；场景/存档/任务等业务语义属各自 Gameplay 域。本文件不重复它们的职责判定。
上游来源：
- 代码：`Assets/Scripts/Pipeline/SO/Events/*.cs`（12 个类）、全部订阅点与发布点（见 2.4 清单）
- 资产：`Assets/GameSO/Events/**`（28 个 `.asset`）
- 关联代码：`Assets/Scripts/Pipeline/UI/SystemCanvasManagers/UIManager.cs`、`Assets/Scripts/Contracts/ICanvasManager.cs`、`Assets/Scripts/Contracts/MyEnums.cs`
- 旧文档（仅作线索）：`Temp/doc-discovery/asset-data-pipeline.json`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `事件通道` / `EventSO` / `事件SO` | 第 2 节 |
| `新增事件` / `新建通道` | 2.6 与 3.4 检查清单 |
| `改名事件` / `重命名事件资产` | 3.4 检查清单（**必读**） |
| `VoidEventSO` / `OnEventRaised` | 2.1、2.2 |
| `ToggleCanvasEventSO` / `canvasToToggle` | 2.3 与 3.2 |
| `SceneLoadedEvent` / `SceneLoadEvent` | 2.2、2.5 |
| `订阅` / `OnEnable +=` / `OnDisable -=` | 2.4 与 3.1 |
| `Raise` / `OnEventRaised` / `发布` | 2.4 与 3.1 |
| `事件不生效` / `接线断了` / `Inspector 没拖` | 3.4 检查清单 |
| `事件资产在哪` / `Events 目录` | 2.3 |
| `InventorySlotsStatsSO` | 2.1、2.3 与第 4 节缺陷 4 |

## 2. 当前实现

### 2.1 清单：12 个通道类，28 个通道资产

通道类全部位于 `Assets/Scripts/Pipeline/SO/Events/`，**12 个**：

| # | 类 | 对外事件 | 发布方法 | 资产数 |
|---|---|---|---|---|
| 1 | `VoidEventSO` | `Action VoidEvent` | `OnEventRaised()` | 6 |
| 2 | `ToggleCanvasEventSO` | `Action<bool> toggleCanvasEvent`、`Action focusEvent` | `RaiseToggleCanvasEvent(bool)`、`RaiseFocusEvent()` | 10 |
| 3 | `InventorySlotsStatsSO` | `Action<ItemSO,int,int> InventoryUpdateRequestEvent` | `RaiseInventoryUpdateRequest(...)` | 3 |
| 4 | `SceneLoadEventSO` | `Action<List<GameSceneSO>,Vector3,bool> LoadRequestEvent` | `RaiseLoadRequestEvent(...)` | 1 |
| 5 | `SceneLoadedEventSO` | `Action<GameSceneSO> SceneLoadedEvent` | `RaiseSceneLoadedEvent(...)` | 1 |
| 6 | `DataSaveEventSO` | `Action<MyEnums.SaveType> DataSaveEvent` | `RaiseDataSaveEvent(...)` | 1 |
| 7 | `LoadQuestEventSO` | `Action<List<QuestSO>> LoadQuestEvent` | `OnLoadQuestEventRaised(...)` | 1 |
| 8 | `LootEventSO` | `Action<ItemSO,int,Loot> LootEvent` | `OnEventRaised(...)` | 1 |
| 9 | `QuestOptionsEventSO` | `Action<MyEnums.QuestState> questOptionsEvent` | `OnQuestOptionsEventRaised(...)` | 1 |
| 10 | `ShopKeeperEventSO` | `Action<ShopKeeper> ShopKeeperEntered / Exited` | `RaiseShopKeeperEntered/Exited(...)` | 1 |
| 11 | `PlayerDamagedEventSO` | `Action<int,Transform,float,float> PlayerDamaged` | `OnPlayerDamaged(...)` | 1 |
| 12 | `EnemyDefeatedEventSO` | `Action<int,Transform> EnemyDefeated` | `OnEnemyDefeated(...)` | 1 |
| | **合计** | | | **28** |

统计方法（可复现）：对 `Assets/GameSO/Events/**/*.asset` 逐个读 `m_Script` 的 guid，与 12 个 `.cs.meta` 的 guid 对照分组。改类数、改资产数后请重跑此法，不要沿用本文档的数字。

**不要**把本数记成 11 或 24：12 个类里有 3 个是多实例——`ToggleCanvasEventSO` 占 10 个资产、`VoidEventSO` 占 6 个、`InventorySlotsStatsSO` 占 3 个，合计 19；其余 **9 个类各 1 个**（19 + 9 = 28）。

### 2.2 类型契约（无公共基类）

**12 个类每一个都直接继承 `ScriptableObject`，没有公共基类、没有公共接口。** 例如 `VoidEventSO.cs:8`、`SceneLoadedEventSO.cs:5`、`ShopKeeperEventSO.cs:5`、`PlayerDamagedEventSO.cs:10` 全部写作 `: ScriptableObject`；`Assets/Scripts/Pipeline/SO/GuidSO.cs` 是另一条无关的继承线，事件通道不使用它。

- 事件字段一律是 `public event Action<...>`（非 `[SerializeField]`）⇒ **委托链不序列化**：资产文件里只有 `m_Script` 与类内配置字段，看不到任何订阅者。
- 泛型参数只引用 `ItemSO`/`QuestSO`/`GameSceneSO`/`Loot`/`ShopKeeper` 及 `MyEnums` 枚举；**没有任何一个通道类引用 Manager/Controller/View**，即通道类与具体订阅方在源码层面零耦合。
- 发布方法命名**不统一**：有 `Raise*`（`ToggleCanvasEventSO`、`ShopKeeperEventSO`、`SceneLoadEventSO` 等）、也有 `On*Raised`/`On*`（`VoidEventSO.OnEventRaised`、`LootEventSO.OnEventRaised`、`LoadQuestEventSO.OnLoadQuestEventRaised`、`PlayerDamagedEventSO.OnPlayerDamaged`）。新增通道时**照抄同族命名**。
- 每个资产实例是**独立通道**：同一个类建两个资产就是两条互不相干的广播线（`VoidEventSO` 6 个资产各管一件事即是此用法）。

### 2.3 事件资产的组织方式

```
Assets/GameSO/Events/
├── DataSavedEvent.asset  EnemyDefeatedEvent.asset  LoadQuestEvent.asset  LootEvent.asset
├── PlayerDamagedEvent.asset  QuestOptionsEvent.asset  SceneLoadEvent.asset
├── SceneLoadedEvent.asset  ShopKeeperEvent.asset                    (各 1 个，共 9)
├── ToggleCanvasEvents/     Toggle<后缀>Event.asset          × 10
├── VoidEvents/             <语义>Event.asset                 × 6
└── InventorySlotsStatsEvents/                               × 3
```

- 单一用途的通道直接放 `Events/` 根；一个类有多个实例时按类归子目录（`ToggleCanvasEvents`、`VoidEvents`、`InventorySlotsStatsEvents`）。
- 命名**不跟随类名**：`ToggleESCEvent.asset`、`RetryRequestEvent.asset`、`DataSavedEvent.asset`（注意资产是 `DataSaved`，类与方法是 `DataSave`/`RaiseDataSaveEvent`）。
- `VoidEventSO` 资产的语义全靠命名，源码注释把它定为硬规则："资产名必须含语义前缀…禁止再创建无语义的资产名"（`VoidEventSO.cs:4-5`）。
- `InventorySlotsStatsSO` 这一类名字与"事件"无关，容易被误当成配置资产（见第 4 节缺陷 4）。

### 2.4 订阅与发布惯例

**统一订阅惯例：`OnEnable` 里 `+=`，`OnDisable` 里 `-=`。** 本次全量核验（对 `Assets/Scripts` 下 `.cs` 匹配订阅表达式）：

- 订阅表达式 **49** 处，分布在 **22** 个文件中；发布调用点 **23** 处，分布在 **15** 个文件中。订阅点最密集的是 `QuestManager.cs:80-85`（6 处）；其余订阅方包括 `ShopManager`、`StatsPanelView`、`SaveLoadCanvasManager`、`IntegratedUICanvasManager`、`BackpackCanvasManager`、`SkillTreeCanvasManager`、`ESCMenuManager`、`GameOverCanvasManager`、`InventoryManager`、`QuestBoardManager`、`PlayerDamageController`、`PlayerMovement`、`ShopPortraitCamera`、`QuestLogPanel`、`DialogManager`、`SaveDataManager`、`SaveSystem`、`MenuSceneCanvasHider`、`NPCDialogTrigger`、`UIManager`、`ExperienceController`（后者是唯一的例外：不在 `OnEnable`/`OnDisable` 订阅，而是构造函数 `+=`、`Dispose` `-=`，`ExperienceController.cs:25,31`）。

代表写法（三件事一次可见：取字段、`+=`、同名 `-=`）：

```csharp
// Assets/Scripts/Gameplay/Save/SaveDataManager.cs:28-36
private void OnEnable()  { sceneLoadEventSO.LoadRequestEvent += OnAutoSave; ... }
private void OnDisable() { sceneLoadEventSO.LoadRequestEvent -= OnAutoSave; ... }
```

**发布侧**只调通道方法，不关心谁在听（23 处，代表）：

| 通道 | 发布点 |
|---|---|
| `SceneLoadEvent` | `Assets/Scripts/Pipeline/Scene/SceneChanger.cs:88` |
| `SceneLoadedEvent` | `Assets/Scripts/Pipeline/Scene/SceneChanger.cs:284` |
| ToggleCanvas ×10 | `UIManager.cs:343`（`RaiseToggleCanvasEvent`）、`:348`（`RaiseFocusEvent`）、`:366`（切场景复位全部关） |
| `DataSavedEvent` | `SaveDataManager.cs:114` |
| `LootEvent` | `Loot.cs:170` |
| `InventorySlotsStats` | `ShopManager.cs:135,145`、`QuestManager.cs:185` |
| `ShopKeeperEvent` | `ShopKeeper.cs:28,38,46` |
| `PlayerDamagedEvent` | `EnemyCombat.cs:23` |
| `EnemyDefeatedEvent` | `EnemyHealth.cs:32` |
| `LoadQuestEvent`/`OpenQuestBoardEvent(Void)` | `QuestBoardManager.cs:32,33` |
| `QuestOptionsEvent` | `QuestOptionsButton.cs:11` |
| `RetryRequestEvent(Void)` | 发布 `RetryButton.cs:15`；订阅 `PlayerDamageController.cs:22`（重试复活编排，已不在 `SceneChanger`） |
| `Slash/ShootActionFinishedEvent(Void)` | `PlayerCombat.cs:39`、`PlayerBow.cs:130`、`ShiftEquipment.cs:44-45` |

### 2.5 ToggleCanvasEventSO 的内部契约（唯一有配置字段的通道）

`ToggleCanvasEventSO` 是 12 个类里唯一带配置字段的：`public MyEnums.CanvasToToggle canvasToToggle`（`ToggleCanvasEventSO.cs:16`）。它同时持有**两条**事件——`toggleCanvasEvent`（显隐）与 `focusEvent`（只调排序优先级），源码注释说明二者刻意分离，"避免复用 open 语义来传达置顶/降级"（`ToggleCanvasEventSO.cs:10-14`）。

消费侧靠枚举匹配，而非靠引用配对：

```csharp
// Assets/Scripts/Pipeline/UI/SystemCanvasManagers/UIManager.cs:185-194
private ToggleCanvasEventSO FindToggleEvent(CanvasToToggle canvas) {
    foreach (var eventSO in toggleCanvasEvents)
        if (eventSO != null && eventSO.canvasToToggle == canvas) return eventSO;   // 取第一个匹配
    return null;
}
```

`UIManager.toggleCanvasEvents`（`UIManager.cs:17-18`）是这份"枚举 → 资产"的登记表；`IsClosableCanvas` 用它判定面板能否被关（`UIManager.cs:196-201`），未登记的 `CanvasToToggle` 不会被 UIManager 主动关闭。

现状核验：10 个 toggle 资产映射到 **10 个互不重复**的 `CanvasToToggle` 值，`MyEnums.CanvasToToggle`（`MyEnums.cs:56-72`）共 11 个成员，唯一无资产的是 `Default`。

| 资产 | `canvasToToggle` |
|---|---|
| `ToggleESCEvent.asset` | 字段**缺失** ⇒ 反序列化为 `0` = `ESC` |
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

1. 若要新类：在 `Assets/Scripts/Pipeline/SO/Events/` 建类，`[CreateAssetMenu(menuName = "Events/<类名>")]`，包一层 `public event Action<...>` + 一个发布方法，**照抄同族命名风格**（2.2）。
2. 在 `Assets/GameSO/Events/`（或多个实例时建子目录）右键 `Create > Events > ...` 建资产，**立即改名**（默认文件名是类名，`VoidEventSO` 尤其必须补语义前缀）。
3. 发布方 `[SerializeField]` 拖该资产；订阅方在 `OnEnable`/`OnDisable` 成对 `+=`/`-=`；**两侧都要在 Inspector 拖到同一个资产**（3.4）。
4. 若是画布开关：给资产设 `canvasToToggle`，并把资产加进 `PersistentScene` 中 `UIManager` 的 `toggleCanvasEvents` 列表，否则关键盘唤起/ESC 关闭/切场景复位都不会生效（`UIManager.cs:196-201`）。

## 3. 约定与硬边界

### 3.1 订阅必须成对、且必须在 OnEnable/OnDisable

`ScriptableObject` 资产的生命周期比场景对象长：若只 `+=` 不 `-=`，对象销毁后委托仍指向已销毁实例，下次广播会抛 `MissingReferenceException`，且重复 `OnEnable`（对象复用、场景重载）会累积多次订阅 ⇒ **一次广播触发多次回调**。现有 22 个订阅方全部成对（2.4；`ExperienceController` 在构造函数订阅、在 `Dispose` 注销，同样成对）。

反面教材（同一文件内不一致，属既有事实）：`Assets/Scripts/Gameplay/Skills/SkillSlot.cs:18-19` 用的是 `public static event` 而非 SO 通道，`SkillManager.cs:9-16` 也成对订阅——静态事件跨场景存活，是另一套机制，**不要**照其写法新增 SO 通道订阅。

### 3.2 `canvasToToggle` 是隐式身份，必须唯一

`FindToggleEvent` 取**列表中第一个**匹配项（`UIManager.cs:188-191`）。若两个 toggle 资产填了同一个 `CanvasToToggle`，后者永远查不到 ⇒ 该面板的焦点/开关广播静默丢失。当前 10 个资产值互不重复（2.5）。新增画布时**先加枚举成员**（`MyEnums.cs:56-72`，注释要求在 `Default` 之前插入），再建资产填值，最后登记进 `toggleCanvasEvents`。

### 3.3 事件的"关系不可见"是本机制的固有短板

- 通道类源码里**只有**泛型参数类型，没有订阅方类型（2.2 核验：12 个类中零处引用 Manager/Controller/View）。
- 发布方与订阅方在 C# 层**互不引用**：双方各自持有同一个资产引用，靠 Inspector 同一个资产实例相遇。因此"谁在听这条广播"**无法从代码读出**，只能靠 grep 订阅点或翻场景/预制体。
- 事件资产里**也不记录**订阅者（委托链不序列化，2.2）。⇒ 一个通道是否真的有人听、听的人是否还在场景里，静态检查无从得知。
- 已有事故形态：本域统计到 **3 个通道资产零外部引用**——`VoidEvents/LoadData`、`VoidEvents/SaveData`、`InventorySlotsStatsEvents/SlotsUpdateRequest`，发布/订阅两侧都没接线却不报任何错。

### 3.4 新增/改名事件通道的检查清单（改名会静默失效全部接线）

**改名或移动任一事件资产前，先按此表核对该通道的全部落点。** 关键事实：资产引用是按 **GUID** 存的，在 Unity 内用重命名/移动**不会**断引用；但只要绕过 Unity（在文件系统改名、重新 `Create` 一个新资产替换旧的、或改了资产的 GUID），所有引用会**全部静默失效**——订阅方 `OnEnable` 里 `event += null` 或抛空引用，且编译期无任何提示。

| # | 检查项 | 排查方式 |
|---|---|---|
| 1 | 通道类本身（事件名、发布方法签名） | `Assets/Scripts/Pipeline/SO/Events/<类>.cs` |
| 2 | 发布方：`[SerializeField]` 拖了该资产 | `grep` 该资产 GUID 于 `*.prefab`/`*.unity`/`*.asset` |
| 3 | 订阅方：`OnEnable +=` / `OnDisable -=` | 每个订阅方文件内的成对改动（2.4 的 22 个文件清单） |
| 4 | 场景/预制体里的资产引用 | `Assets/Scenes/**`、`Assets/Prefabs/**`（按 GUID 计数，改名后引用数不应为 0） |
| 5 | 若为 toggle 通道：`canvasToToggle` 值唯一 + 已登记进 `UIManager.toggleCanvasEvents` | `MyEnums.cs:56-72`、`UIManager.cs:17-18,185-194` |
| 6 | 若为 `PersistentScene` 内管理器持有：确认它是常驻场景对象 | `Assets/Scenes/GameScene/PersistentScene.unity` |
| 7 | 改完后按 GUID 复算引用数，确认与改前一致 | 对 `Assets/**/*.{unity,prefab,asset}` 语料做 GUID 匹配计数 |

改类名/字段名（而非资产名）是更危险的一类：`[SerializeField] private SceneLoadedEventSO sceneLoadedEvent;` 字段改名会让 Inspector 引用**静默变空**（Unity 按字段名反序列化），编译能过、运行时无广播。

## 4. 已知缺陷与风险

1. **关系不可见（3.3）。** 订阅者既不写在通道类里也不写在资产里，唯一事实来源是分散在 22 个文件的 `OnEnable`/`OnDisable`。任何"这条广播还有没有人听"的判断都只能靠 grep + 翻场景。这是本机制的固有属性，不是可修的 bug。
2. **改名/重建资产的静默失效（3.4）。** 无编译期保护，无接入校验脚本；新增通道也没有生成器（手工 `CreateAssetMenu` + 手工改名 + 手工拖引用，见 `Assets_Guide.md` 2.7）。
3. **3 个零引用通道资产**（`VoidEvents/LoadData`、`VoidEvents/SaveData`、`InventorySlotsStatsEvents/SlotsUpdateRequest`）。它们存在且命名像"在用"，但发布/订阅两侧都无落点。`SlotsUpdateRequest` 与在用的 `ShopUpdateRequest`/`QuestRewardEvent` 同属 `InventorySlotsStatsSO`，最易被误认为在用。
4. **`InventorySlotsStatsSO` 这个类名不含"事件"。** 它实际是通道（`InventoryUpdateRequestEvent`，`InventorySlotsStatsSO.cs:8`），却与"物品栏槽位统计配置"的语感混淆；`Assets/GameSO/Events/InventorySlotsStatsEvents/` 下 3 个资产里只有 2 个在用（见上）。
5. **`ToggleESCEvent.asset` 的 `canvasToToggle` 字段是缺失的**（靠默认值 0 落到 `ESC`）。当前行为正确，但这类"靠枚举 0 兜底"的资产在枚举重排或字段被显式写入 0（`Default`）时会静默错位——`MyEnums.cs:70-71` 的注释正警告 `Default` 语义特殊且"不应该使用 default 的任何索引"。
6. **发布方法命名不统一**（`Raise*` / `On*Raised` / `On*`，2.2），新通道易照错模板，增加了 grep 成本。
## 5. 未核验事项

- 假设：49 处订阅与 23 处发布是当前全量（对 `Assets/Scripts/**/*.cs` 按字段类型 + `+=`/发布方法名匹配，未用 AST，未含 `Tests`；未运行 Unity 验证）。
- 假设：3 个零引用通道资产在运行时确实不可达（静态 GUID 计数，未运行 Unity）。
- 假设：`ToggleESCEvent` 因字段缺失而落到 `ESC(=0)` 是**有意**的默认值写法，而非误删字段（未在 Inspector 中打开确认，仅读 `.asset` YAML）。
- 假设：`OnEnable`/`OnDisable` 的成对订阅在域重载后仍能正确重建、不产生重复订阅（未运行 Unity）。
