# 组合与初始化时序 Guide

文档 ID：`ARCH-COMPOSITION-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责「场景实例如何被组合成全局服务图」与「这些服务在 Unity 生命周期里的初始化先后、竞态兜底方式」。不负责各业务域自身的数据流（见 Gameplay\* 与 SceneFlow 域），也不负责程序集拆分规划（见 AssemblyPlan，状态 Proposal）。
上游来源：
- `Assets/Scripts/Contracts/YSingleton.cs`、`SaveableService.cs`、`SaveRegistry.cs`、`ISaveable.cs`、`ICanvasManager.cs`、`MyEnums.cs`
- `Assets/Scripts/Pipeline/TimeManager.cs`、`Pipeline/Scene/{SceneChanger,SceneDataForSave,CameraPixelSnap,PersistentSceneRegistry,InitialLoad}.cs`
- `Assets/Scripts/Pipeline/UI/SystemCanvasManagers/{UIManager,CanvasFocusStack,IntegratedUICanvasManager}.cs`
- `Assets/Scripts/Gameplay/Player/Services/StatsService.cs`、`Gameplay/Skills/SkillTreeManager.cs`
- `Assets/Scripts/Gameplay/Save/{SaveSystem,SaveDataManager,SaveLoadCanvasManager}.cs`
- `Assets/Scripts/Gameplay/Player/Controllers/PlayerDamageController.cs`、`Pipeline/UI/Buttons/{RetryButton,ESCButton}.cs`
- `Docs/My_ARPG_MVCS项目现状.md`（仅作线索，与代码冲突时以代码为准）

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `单例`、`YSingleton`、`Instance` | §2.1、§2.2 |
| `单例预算`、`单例数量`、`又加了一个单例` | §2.2 |
| `初始化顺序`、`Awake 顺序`、`Instance 为 null` | §2.3 |
| `OnSingletonInitialized` | §2.3.1 |
| `可重试订阅`、`TrySubscribe`、`订阅空引用` | §2.3.2 |
| `isLoading`、`IsLoadingSaveRequest`、`标志位/窗口` | §2.3.3 |
| `执行顺序`、`DefaultExecutionOrder`、`10000` | §2.4 |
| `SaveRegistry`、`注册表`、`Domain Reload`、`局间复位` | §2.5 |
| `ICanvasManager`、`焦点栈`、`sortingOrder`、`UIManager.Instance` | §2.6 |
| `重试`、`RetryRequestEvent`、`Respawn`、`重载当前场景组` | §2.3.4 |

## 2. 当前实现

### 2.1 单例基类与初始化钩子

`YSingleton<T>` 是 CRTP 基类：`Awake` 中先做重复实例判定（停用 + `Destroy(this)` 后直接 return），合法实例才写 `_instance` 并调用钩子 `OnSingletonInitialized()`，`OnDestroy` 只在 `_instance == this` 时复位（`Assets/Scripts/Contracts/YSingleton.cs:9`、`:15-25`、`:28`、`:30-34`）。`Instance` 只读、无惰性创建（`:13`），因此「拿不到实例」在设计上就是可能状态，调用方必须假定 `Instance` 可为 null。

当前 18 个直接派生类均未重写 `Awake`（全工程 `Awake` 定义点核对后无一落在 `YSingleton` 派生类里），所以 `base.Awake()` 链路目前无人踩断。

### 2.2 单例清单与预算

全工程 `Assets/**` 内 `YSingleton<T>` 派生共 **1 个抽象基类 + 19 个具体单例**（18 个直接继承 + `StatsService` 经 `SaveableService` 间接继承）。核对方式：`YSingleton<` 声明点 19 处，其中 1 处是 `SaveableService<TSelf>` 自身。

| 具体单例 | 声明点 | 职责 |
|---|---|---|
| `TimeManager` | `Pipeline/TimeManager.cs:3` | `Time.timeScale` 与暂停引用计数 |
| `SceneDataForSave` | `Pipeline/Scene/SceneDataForSave.cs:4` | 场景 SO 列表（读档定位场景用） |
| `SceneChanger` | `Pipeline/Scene/SceneChanger.cs:13` | 场景组加载/卸载与过渡 |
| `ObjectsMapManager` | `Pipeline/Pathfinding/ObjectsMapManager.cs:13` | 位置登记表 |
| `AStarPathFinder` | `Pipeline/Pathfinding/AStarPathFinder.cs:11` | A* 寻路 |
| `AStarNodeManager` | `Pipeline/Pathfinding/AStarNodeManager.cs:7` | 网格节点构建 |
| `UIManager` | `Pipeline/UI/SystemCanvasManagers/UIManager.cs:7` | 画布焦点、输入、层级 |
| `IntegratedUICanvasManager` | `Pipeline/UI/SystemCanvasManagers/IntegratedUICanvasManager.cs:6` | 集成面板 `ICanvasManager` |
| `InventoryManager` | `Gameplay/Inventory/InventoryManager.cs:8` | 背包与快捷栏 |
| `DialogManager` | `Gameplay/Dialog/DialogManager.cs:5` | 对话推进 |
| `ConversationHistoryManager` | `Gameplay/Dialog/HistoryManager/ConversationHistoryManager.cs:6` | 已对话角色/对话记录集合 |
| `ItemHistoryManager` | `Gameplay/Dialog/HistoryManager/ItemHistoryManager.cs:6` | 物品拾取累计 |
| `VisitedHistoryManager` | `Gameplay/Dialog/HistoryManager/VisitedHistoryManager.cs:5` | **空类，无任何成员** |
| `QuestManager` | `Gameplay/Quest/QuestManager.cs:6` | 任务 |
| `ShopManager` | `Gameplay/Shop/ShopManager.cs:4` | 商店 |
| `SaveSystem` | `Gameplay/Save/SaveSystem.cs:29` | 存档文件读写 |
| `SaveDataManager` | `Gameplay/Save/SaveDataManager.cs:12` | 存档对象组装与分发 |
| `PlayerLocator` | `Gameplay/Player/PlayerLocator.cs:9` | 玩家坐标查询 |
| `StatsService` | `Gameplay/Player/Services/StatsService.cs:16` | 玩家数值（经 `SaveableService`） |

预算口径来自 `Docs/My_ARPG_MVCS项目现状.md:222`：被显式认定为「符合单例预算」的是 `StatsService`、`SaveDataManager`、`SaveSystem`、`UIManager`、`SceneChanger`、`TimeManager` 六个。其余 13 个具体单例不在这份名单内，属于超预算存量，新增单例前先对照此表。

存档参与者只有两类：`SaveableService` 派生（当前仅 `StatsService`，固定槽位、`GetDataID()` 返回 null，`Contracts/SaveableService.cs:25`）与直接实现 `ISaveable` 的场景物体（当前仅 `Loot`，按 GUID 参与，`Gameplay/Inventory/Loot.cs:7`）。

### 2.3 初始化时序的三套手写解法

#### 2.3.1 统一钩子：`OnSingletonInitialized`

实例注册成功后的初始化一律写在这个钩子里，而不是自己写 `Awake`：`StatsService.cs:26-30`（建模型 + `base` 注册）、`SceneChanger.cs:75-78`（设初始位置）、`UIManager.cs:38-49`（枚举建 `inputState` + 接线焦点栈回调）、`IntegratedUICanvasManager.cs:27-37`、`AStarNodeManager.cs:10-14`、`DialogManager.cs:25-29`。共 6 个重写点。

#### 2.3.2 可重试订阅：`SkillTreeManager` 式

`SkillTreeManager` 是不挂单例的 `MonoBehaviour`，需要订阅 `StatsService` 上的 `LevelUp`。`OnEnable` 可能早于 `StatsService.Awake`，所以订阅被写成可重试的：`TrySubscribeLevelUp()` 以 `Instance == null` 为提前返回条件、用 `listeningToLevelUp` 防重复订阅，并在 `OnEnable` 与 `Start` 各调一次（`Gameplay/Skills/SkillTreeManager.cs:14-19`、`:30-37`、`:64-66`）；退订侧同样先判 null 再解绑（`:39-46`）。注释明确了假设：`Start` 一定在所有 `Awake` 之后（`:30`）。

同类「判空即跳过」的兜底还有：`SaveLoadCanvasManager.cs:111`（启动期 `OnEnable` 早于 `SaveSystem.Awake`）、`Pipeline/UI/Buttons/ESCButton.cs:21-24`、`Loot.cs:128`、`PathFollower.cs:62`、`:94`。

#### 2.3.3 两个窗口标志位

- `SceneChanger.isLoading`：请求→淡入→卸载→加载→完成的整个窗口内置 true，`OnLoadRequestEvent` 开头据此拒绝新请求（`Pipeline/Scene/SceneChanger.cs:46`、`:142-148`），在 `OnLoadCompleted` 末尾才复位（`:287`）。注意广播段在标志位之前：`RequestSceneLoad` 先 `RaiseLoadRequestEvent` 再进执行段（`:86-90`），所以 `isLoading` 拦不住广播段内发起的重入请求。
- `SaveSystem.IsLoadingSaveRequest`：读档入口设置的短暂窗口标志，包住 `SceneChanger.Instance.RequestSceneLoad(...)` 这一次同步调用（`Gameplay/Save/SaveSystem.cs:31`、`:133-137`），消费方是 `SaveDataManager.OnAutoSave`（`Gameplay/Save/SaveDataManager.cs:88-91`），语义是「读档触发的切换不写自动存档」。它只覆盖广播段（同步），不覆盖异步加载完成。

#### 2.3.4 重试复活的编排落点（本轮迁移）

`SceneChanger` 现在**完全不订阅事件**：全文只有 `OnSingletonInitialized`（`:75-78`）与 `Start`（`:96-99`），没有 `OnEnable`/`OnDisable`/`OnDestroy`，旧的 `retryEventSO` 订阅与切换执行段里的 `StatsService.Instance.Respawn()` 已一并迁出。切换的唯一入口仍是 `RequestSceneLoad`，先广播后执行（`:86-90`）。

重试这条链路的订阅与编排现在都在 `Gameplay/Player/Controllers/PlayerDamageController.cs`：

- **订阅**：`[SerializeField] VoidEventSO retryEventSO`（`:16`），`OnEnable` 订 `VoidEvent`、`OnDisable` 成对退订、不判空（`:19-29`）。资产是 `Assets/GameSO/Events/VoidEvents/RetryRequestEvent.asset`，由 `Pipeline/UI/Buttons/RetryButton.cs:15` 的 `retryEventSO.OnEventRaised()` 触发。
- **编排**：`OnRetryRequest()` 先 `StatsService.Instance.Respawn()`，再 `SceneChanger.Instance.RequestSceneLoad(SceneChanger.Instance.GetCurrentScenes(), Vector3.zero, true)`（`:54-58`）；位置传零向量以复用场景组预设出生点，场景组取当前组副本而非原列表。
- **顺序不可交换**，理由写在方法注释里（`:48-53`）：`SaveDataManager.OnAutoSave` 在 `RequestSceneLoad` 的广播段同步抓存档快照，场景加载完成后又回灌该快照；回血若晚于快照抓取，就会把死亡态血量写进存档并覆盖回运行时。
- **回灌侧的配套兜底**：`SaveDataManager.OnAutoLoad` 遍历 `SaveRegistry.All` 时用 `if (saveable.GetDataID() == null) continue;` 跳过 `SaveableService` 派生的固定槽位服务（`Gameplay/Save/SaveDataManager.cs:120-134`），避免广播段抓的旧快照覆盖回运行时、静默撤销加载窗口内的状态变更。

这条链路**不是第四套手写解法**，而是既有机制（静态注册表 + 显式入口 + 广播顺序）的消费方。另外：`Assets/GameSO/GameSceneSO/OtherScenes/RetrySceneSO.asset` 仍在工程里，但按 guid 全量检索，`*.unity`/`*.prefab`/`*.asset` 中已**零引用**——它已不再被 `SceneChanger` 使用。

### 2.4 全工程唯一的 DefaultExecutionOrder

`[DefaultExecutionOrder(10000)]` 只在 `Pipeline/Scene/CameraPixelSnap.cs:9` 出现一次；`ProjectSettings/` 下不存在 `MonoManager.asset`，也没有 `m_ScriptExecutionOrder` 条目，即工程未配置任何脚本执行顺序。其真实语义是渲染管线收尾：在 `Awake` 缓存 Camera 并按平台设帧率（`:20-26`），在 `OnPreCull` 里把画面吸附到像素网格后**只改 `worldToCameraMatrix`**（`:28-54`），`OnPostRender`/`OnDisable` 复位（`:56-73`）。注释写明动机：避免直接改 Transform 与 Cinemachine 互相覆盖（`:5-7`）。所以 10000 表达的是「比其他组件更晚进入渲染回调」，**不是**业务初始化时序依赖。

### 2.5 SaveRegistry：唯一被显式设计掉时序依赖的存档登记处

设计要点四条：静态类先于一切场景实例存在（`Contracts/SaveRegistry.cs:5`）；注册/注销在任何生命周期阶段调用都安全，`Awake` 顺序不影响注册结果（`:6`）；`SubsystemRegistration` 阶段清空静态列表，保证 Domain Reload 开关两种配置行为一致（`:18-19`）；另有 `Clear()` 供同进程重开新局的局间复位（`:29-30`）。参与者侧由接口默认方法提供注册能力（`Contracts/ISaveable.cs:6-14`），由基类在钩子里自动登记、在 `OnDestroy` 注销（`Contracts/SaveableService.cs:14-17`、`:19-23`）。

限定语：`SaveRegistry` 是存档链路上唯一这样做的地方，**不是全工程唯一**——`PersistentSceneRegistry` 用同一套 `SubsystemRegistration` 复位惯用法（`Pipeline/Scene/PersistentSceneRegistry.cs:39-43`），由 `InitialLoad.Awake` 注册（`Pipeline/InitialLoad.cs:11-15`）。

### 2.6 UI 画布焦点栈契约

`ICanvasManager` 的四个方法是接口默认实现：`ToggleCanvas`（`Contracts/ICanvasManager.cs:37-45`）、`SetCanvaInactive`（`:53-58`）、`SetCanvaState`（`:66-76`）、`RefreshCanvaOrder`（`:85-100`）。实现者共 10 个（`ESCMenuManager`、`IntegratedUICanvasManager`、`GameOverCanvasManager`、`SkillTreeCanvasManager`、`SaveLoadCanvasManager`、`ShopManager`、`DialogManager`、`BackpackCanvasManager`、`QuestManager`、`StatsPanelView`），调用惯例统一为 `((ICanvasManager)this).方法(...)`。

反向依赖有两处，都在默认实现里直读单例：`SetCanvaState` 末行调静态入口 `UIManager.Report(...)`（`:75`，该入口在 `UIManager.cs:116-126` 内集中判空，未注册时静默跳过）；`RefreshCanvaOrder` 直读 `UIManager.Instance`，**Instance 为 null 时不算错也不报错**，退化为 `UIManager.DefaultOrder`（`:90-93`）。`UIManager` 为此保留了常量转发与 `GetCanvasOrder` 转发，避免改动接口引用（`UIManager.cs:9-11`、`:159-162`）。

`CanvasFocusStack` 是把上述状态机从 `UIManager` 拆出的纯 C# 类：不继承 `MonoBehaviour`、不引用 `ToggleCanvasEventSO`，全部外部动作经两个回调委托 `OnCanvasToggleRequested` / `OnFocusRefreshRequested`（`Pipeline/UI/SystemCanvasManagers/CanvasFocusStack.cs:11`、`:26-29`、`:224-227`），宿主在 `OnSingletonInitialized` 里接线（`UIManager.cs:47-48`）。这让它在 EditMode 里可被 `new` 出来直接断言（`Assets/Tests/Editor/CanvasFocusStackTests.cs:20-24`），覆盖开闭顺序、order 计算、ESC 行为与互斥快照。

## 3. 约定与硬边界

1. **单例初始化钩子内不做跨单例调用。** 钩子在各实例自己的 `Awake` 内执行，`Awake` 顺序无保证；在钩子里读另一个 `Instance` 会随场景层级顺序随机变成空引用（`YSingleton.cs:15-25` 无任何就绪屏障）。跨单例协作必须走 §2.3.2 的重试订阅或 §2.3.3 的窗口标志；广播段内的先后约束（如 §2.3.4 的重试复活）另见该节。
2. **派生类重写 `Awake` 必须调 `base.Awake()`。** 否则 `_instance` 永不赋值，`Instance` 全局为 null；同时重复实例销毁逻辑失效，同场景两份实例会各自运行。后果是静默失效（无异常、无日志）。
3. **重写 `SaveableService.OnSingletonInitialized` 必须调 `base`。** 注册发生在 base 里（`SaveableService.cs:6`、`:16`），漏调则该服务不进 `SaveRegistry`，存档不报错但永远少一段数据。
4. **`SaveRegistry.All` 遍历前必须拷贝，且不得在遍历中注册/注销。** 契约写在 `SaveRegistry.cs:15`；现存调用点全部写作 `SaveRegistry.All.ToList()`（`SaveDataManager.cs:64`、`:81`、`:122`、`:145`）。
5. **`CanvasToToggle.Default` 不参与任何流程。** `MyEnums.cs:69-71` 明确其无索引、语义为默认界面，且是 ESC 判断依据；`UIManager.ReportCanvasState`（`UIManager.cs:136-139`）与 `CanvasFocusStack.ReportState`（`CanvasFocusStack.cs:90-93`）都直接 return。新枚举成员必须加在 `Default` **之前**，加在后面会静默丢失画布能力。
6. **画布要能被 UIManager 主动关闭，其 `ToggleCanvasEventSO` 必须登记进 `UIManager.toggleCanvasEvents`。** 未登记的画布只能上报状态与刷新层级，`IsClosableCanvas` 返回 false（`UIManager.cs:16`、`:196-201`），ESC 与互斥关闭都不会作用于它；此时若向它发关闭请求，焦点栈与真实显隐会错位（`:169` 注释即为此防）。
7. **`EditorSettings` 的 Domain Reload 状态与静态状态必须成对。** 持 static 可变状态的新类，要么加 `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` 复位（现例：`SaveRegistry.cs:18`、`PersistentSceneRegistry.cs:39`），要么保证订阅方成对退订；否则关闭 Domain Reload 时状态跨 Play 存活导致坏档或脏数据。

## 4. 已知缺陷与风险

1. `SaveRegistry.Clear()`（`SaveRegistry.cs:30`）全工程零调用点：同进程重开新局时静态注册表不会被清空，旧局残留的 `ISaveable` 会继续被写入新局存档。`Docs/My_ARPG_MVCS项目现状.md:171` 自己也记为待办。
2. `SaveSystem.IsLoadingSaveRequest` 无 try/finally 保护（`SaveSystem.cs:133-137`）：若 `RequestSceneLoad` 抛异常，异常被外层 catch 吞掉（`:148`）而标志位停在 true，此后所有 Menu→Location 切换都不再写自动存档，且无任何日志。
3. `SceneChanger.isLoading` 同样无 finally：协程 `UnloadCurrentScenes` 中途异常会让标志位永久为 true，之后所有场景切换请求静默被拒（`SceneChanger.cs:142-146`、`:287`）。
4. `ICanvasManager.RefreshCanvaOrder` 在 `UIManager.Instance == null` 时静默取 `DefaultOrder`（`ICanvasManager.cs:90-93`）：启动期打开的画布层级会错但不报错，属于跨单例时序最隐蔽的一处。
5. `VisitedHistoryManager` 是 7 行空壳单例（`VisitedHistoryManager.cs:5-7`），白占一个单例名额与一个场景组件位。
6. 单例预算已超：`Docs/My_ARPG_MVCS项目现状.md:222` 认定的合规集合只有 6 个，实际 19 个具体单例，其中对话历史三件套（`ConversationHistoryManager`/`ItemHistoryManager`/`VisitedHistoryManager`）与寻路三件套（`ObjectsMapManager`/`AStarPathFinder`/`AStarNodeManager`）是明显可合并/可下沉的簇。
7. `RequestSceneLoad` 的广播先于 `isLoading` 置位（`SceneChanger.cs:86-90`、`:148`）：任何订阅者在广播段内再发起一次 `RequestSceneLoad` 都会启动第二条卸载/加载协程，标志位拦不住。

## 5. 未核验事项

1. 假设：单例组件都挂在常驻（Persistent）场景节点上，跨场景不随内容场景卸载——本域只做了 `.cs` 静态核对，未打开 `.unity`/`.prefab` 资产逐个确认挂载位置与启用状态（未运行 Unity 验证）。
2. 假设：`Awake` 顺序在实机上确实无保证，因此 §2.3.2 的重试订阅是必要而非冗余——未构造「从 PersistentScene 直接进 Play」的对照实验（该路径会放大单例竞态的说法原见于 `Docs/My_ARPG_重构优化清单_未解决.md:122`；该文件已由用户在 2026-10-05 删除，内容见 `git HEAD:Docs/My_ARPG_重构优化清单_未解决.md`）。
3. 假设：`CameraPixelSnap` 的 `worldToCameraMatrix` 改写与 Cinemachine 的实际共存结果如注释所述——只读了代码，未在运行时对比画面（未运行 Unity 验证）。
4. 已核验（原假设成立）：`ProjectSettings/EditorSettings.asset:25-26` 为 `m_EnterPlayModeOptionsEnabled: 0`、`m_EnterPlayModeOptions: 3`，即 Enter Play Mode Options 未启用、Domain Reload 保持默认开启。因此 `SaveRegistry.ResetStatics` 与 `PersistentSceneRegistry.Reset` 当前并非「正在生效的兜底」，而是「一旦关闭 Domain Reload 仍正确」的前置保险；§3.7 的纪律不因该实测值改变。真正未验证的是关闭 Domain Reload 后的实际行为（未运行 Unity 验证）。
5. 假设：10 个 `ICanvasManager` 实现者的 Inspector 引用（`ToggleCanvasEventSO`/`SceneLoadedEventSO`/`CanvasGroup`）都已正确接线，`toggleCanvasEvents` 列表覆盖了所有需要被主动关闭的画布——未打开场景资产核对（未运行 Unity 验证）。
6. 假设：`StatsService` 是 `SaveableService` 的唯一派生类这一结论在全工程成立——依据是 `Assets/**` 全量文本检索，未考虑通过其他程序集或动态类型引入的可能（`Library/` 属包缓存，不计入）。
