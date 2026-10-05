# 组合与初始化时序 Guide

文档 ID：`ARCH-COMPOSITION-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责「场景实例如何被组合成全局服务图」与「这些服务在 Unity 生命周期里的初始化先后、竞态兜底方式」。各业务域自身的数据流归 Gameplay 域与 SceneFlow 域，程序集拆分规划归 AssemblyPlan 域（状态 Proposal）。
上游来源：
- 契约层：`YSingleton`、`SaveableService`、`SaveRegistry`、`ISaveable`、`ICanvasManager`、`MyEnums`
- 管线层：`TimeManager`、`SceneChanger`、`SceneDataForSave`、`CameraPixelSnap`、`PersistentSceneRegistry`、`InitialLoad`
- 画布层：`UIManager`、`CanvasFocusStack`、`IntegratedUICanvasManager`
- 业务层：`StatsService`、`SkillTreeManager`
- 存档层：`SaveSystem`、`SaveDataManager`、`SaveLoadCanvasManager`
- 玩家与按钮层：`PlayerDamageController`、`RetryButton`、`ESCButton`
- 项目现状文档《My_ARPG_MVCS项目现状》仅作线索，与代码冲突时以代码为准

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

`YSingleton<T>` 是 CRTP 基类：`Awake` 先做重复实例判定，命中则停用组件、`Destroy(this)` 后直接返回；合法实例写入 `_instance` 并调用钩子 `OnSingletonInitialized()`；`OnDestroy` 只在 `_instance == this` 时复位。`Instance` 只读、无惰性创建，因此「拿不到实例」是设计上成立的运行状态，调用方按 `Instance` 可为 null 处理。

当前 18 个直接派生类都沿用基类 `Awake`，`base.Awake()` 链路完整。

### 2.2 单例清单与预算

`YSingleton<T>` 的派生共 1 个抽象基类加 19 个具体单例：18 个直接继承，`StatsService` 经 `SaveableService` 间接继承。

| 具体单例 | 职责 |
|---|---|
| `TimeManager` | `Time.timeScale` 与暂停引用计数 |
| `SceneDataForSave` | 场景 SO 列表，供读档定位场景 |
| `SceneChanger` | 场景组加载/卸载与过渡 |
| `ObjectsMapManager` | 位置登记表 |
| `AStarPathFinder` | A* 寻路 |
| `AStarNodeManager` | 网格节点构建 |
| `UIManager` | 画布焦点、输入、层级 |
| `IntegratedUICanvasManager` | 集成面板 `ICanvasManager` |
| `InventoryManager` | 背包与快捷栏 |
| `DialogManager` | 对话推进 |
| `ConversationHistoryManager` | 已对话角色与对话记录集合 |
| `ItemHistoryManager` | 物品拾取累计 |
| `VisitedHistoryManager` | 空类，无任何成员 |
| `QuestManager` | 任务 |
| `ShopManager` | 商店 |
| `SaveSystem` | 存档文件读写 |
| `SaveDataManager` | 存档对象组装与分发 |
| `PlayerLocator` | 玩家坐标查询 |
| `StatsService` | 玩家数值，经 `SaveableService` 继承 |

预算口径以项目现状文档《My_ARPG_MVCS项目现状》认定的「符合单例预算」名单为准，该名单含 `StatsService`、`SaveDataManager`、`SaveSystem`、`UIManager`、`SceneChanger`、`TimeManager` 六个。其余 13 个具体单例在此名单之外，属于超预算存量，新增单例前先对照上表。

存档参与者分两类。`SaveableService` 派生类当前只有 `StatsService` 一个，占固定槽位，`GetDataID()` 返回 null。直接实现 `ISaveable` 的场景物体当前只有 `Loot` 一个，按 GUID 参与。

### 2.3 初始化时序的三套手写解法

#### 2.3.1 统一钩子：`OnSingletonInitialized`

实例注册成功后的初始化统一写在 `OnSingletonInitialized` 钩子里，各类都省去自写 `Awake`。重写点共 6 个：`StatsService` 建模型并调 `base` 注册，`SceneChanger` 设初始位置，`UIManager` 枚举建 `inputState` 并接线焦点栈回调，另有 `IntegratedUICanvasManager`、`AStarNodeManager`、`DialogManager`。

#### 2.3.2 可重试订阅：`SkillTreeManager` 式

`SkillTreeManager` 是不挂单例的 `MonoBehaviour`，需要订阅 `StatsService` 的 `LevelUp`。`OnEnable` 可能早于 `StatsService.Awake`，订阅因此写成可重试的：`TrySubscribeLevelUp()` 以 `Instance == null` 为提前返回条件、用 `listeningToLevelUp` 防重复订阅，并在 `OnEnable` 与 `Start` 各调一次；退订侧同样先判 null 再解绑。注释写明其前提：`Start` 排在所有 `Awake` 之后。

同类「判空即跳过」的兜底还有 `SaveLoadCanvasManager`，其启动期 `OnEnable` 早于 `SaveSystem.Awake`；`ESCButton`、`Loot`、`PathFollower` 同属这一类。

#### 2.3.3 两个窗口标志位

- `SceneChanger.isLoading`：请求、淡入、卸载、加载、完成的整个窗口内置 true，`OnLoadRequestEvent` 开头据此拒绝新请求，`OnLoadCompleted` 末尾复位。广播段排在标志位之前，`RequestSceneLoad` 先 `RaiseLoadRequestEvent` 再进执行段，因此 `isLoading` 拦不住广播段内发起的重入请求。
- `SaveSystem.IsLoadingSaveRequest`：读档入口设置的短暂窗口标志，包住 `SceneChanger.Instance.RequestSceneLoad(...)` 这一次同步调用，消费方是 `SaveDataManager.OnAutoSave`，语义为「读档触发的切换不写自动存档」。它覆盖的是广播段这一次同步调用，异步加载完成阶段在其之外。

#### 2.3.4 重试复活的编排落点

`SceneChanger` 不订阅任何事件，生命周期入口只有 `OnSingletonInitialized` 与 `Start`；切换的唯一入口是 `RequestSceneLoad`，先广播后执行。

重试链路的订阅与编排落在 `PlayerDamageController`：

- 订阅：序列化字段 `VoidEventSO retryEventSO` 在 `OnEnable` 订 `VoidEvent`、`OnDisable` 成对退订，订阅时不判空。事件资产是重试请求事件资产，由 `RetryButton` 调用 `retryEventSO.OnEventRaised()` 触发。
- 编排：`OnRetryRequest()` 先 `StatsService.Instance.Respawn()`，再 `SceneChanger.Instance.RequestSceneLoad(SceneChanger.Instance.GetCurrentScenes(), Vector3.zero, true)`；位置传零向量以复用场景组预设出生点，场景组取当前组副本。
- 顺序固定，理由写在方法注释里：`SaveDataManager.OnAutoSave` 在 `RequestSceneLoad` 的广播段同步抓存档快照，场景加载完成后又回灌该快照；回血排在快照抓取之后，等于把死亡态血量写进存档并覆盖回运行时。
- 回灌侧的配套兜底：`SaveDataManager.OnAutoLoad` 遍历 `SaveRegistry.All` 时用 `if (saveable.GetDataID() == null) continue;` 跳过 `SaveableService` 派生的固定槽位服务，避免广播段抓的旧快照覆盖回运行时、静默撤销加载窗口内的状态变更。

这条链路复用既有机制：静态注册表、显式入口、广播顺序。`RetrySceneSO` 重试场景资产仍在工程里，当前全工程零引用。

### 2.4 全工程唯一的 DefaultExecutionOrder

`[DefaultExecutionOrder(10000)]` 只在 `CameraPixelSnap` 出现一次，工程未配置任何脚本执行顺序。其语义是渲染管线收尾：`Awake` 缓存 Camera 并按平台设帧率，`OnPreCull` 把画面吸附到像素网格且只改 `worldToCameraMatrix`，`OnPostRender` 与 `OnDisable` 复位。注释写明动机是避免直接改 Transform 与 Cinemachine 互相覆盖。10000 表达的是「比其他组件更晚进入渲染回调」，业务初始化时序不依赖它。

### 2.5 SaveRegistry：存档链路上显式设计掉时序依赖的登记处

设计要点四条：静态类先于一切场景实例存在；注册与注销在任何生命周期阶段调用都安全，`Awake` 顺序不影响注册结果；`SubsystemRegistration` 阶段清空静态列表，Domain Reload 开关两种配置行为一致；另有 `Clear()` 供同进程重开新局的局间复位。参与者侧由接口默认方法提供注册能力，由基类在钩子里自动登记、在 `OnDestroy` 注销。

存档链路之外只有一处同款用法：`PersistentSceneRegistry` 采用同一套 `SubsystemRegistration` 复位惯用法，由 `InitialLoad.Awake` 注册。

### 2.6 UI 画布焦点栈契约

`ICanvasManager` 的四个方法 `ToggleCanvas`、`SetCanvaInactive`、`SetCanvaState`、`RefreshCanvaOrder` 都是接口默认实现。实现者共 10 个：`ESCMenuManager`、`IntegratedUICanvasManager`、`GameOverCanvasManager`、`SkillTreeCanvasManager`、`SaveLoadCanvasManager`、`ShopManager`、`DialogManager`、`BackpackCanvasManager`、`QuestManager`、`StatsPanelView`；调用惯例统一写成 `((ICanvasManager)this).方法(...)`。

反向依赖有两处，都在默认实现里直读单例：`SetCanvaState` 末行调静态入口 `UIManager.Report(...)`，该入口集中判空，未注册时静默跳过；`RefreshCanvaOrder` 直读 `UIManager.Instance`，`Instance` 为 null 时静默退化为 `UIManager.DefaultOrder`。`UIManager` 为此保留常量转发与 `GetCanvasOrder` 转发，接口引用保持不变。

`CanvasFocusStack` 是把上述状态机从 `UIManager` 拆出的纯 C# 类，与 `MonoBehaviour` 生命周期解耦，也独立于 `ToggleCanvasEventSO`，全部外部动作经两个回调委托 `OnCanvasToggleRequested` 与 `OnFocusRefreshRequested`，宿主在 `OnSingletonInitialized` 里接线。`CanvasFocusStackTests` 因此在 EditMode 里可以直接 `new` 出实例断言，覆盖开闭顺序、order 计算、ESC 行为与互斥快照。

## 3. 约定与硬边界

1. **单例初始化钩子内不做跨单例调用。** 钩子在各实例自己的 `Awake` 内执行，`Awake` 顺序无保证；在钩子里读另一个 `Instance` 会随场景层级顺序随机变成空引用，基类没有就绪屏障。跨单例协作走 §2.3.2 的重试订阅或 §2.3.3 的窗口标志；广播段内的先后约束见 §2.3.4。
2. **派生类重写 `Awake` 时必须调 `base.Awake()`。** 漏调会让 `_instance` 永不赋值、`Instance` 全局为 null，同时重复实例销毁逻辑失效，同场景两份实例各自运行；后果是静默失效，无异常、无日志。
3. **重写 `SaveableService.OnSingletonInitialized` 时必须调 `base`。** 注册发生在 base 里，漏调则该服务不进 `SaveRegistry`，存档不报错但永远少一段数据。
4. **`SaveRegistry.All` 遍历前必须拷贝，且不得在遍历中注册或注销。** 现存调用点统一写作 `SaveRegistry.All.ToList()`。
5. **`CanvasToToggle.Default` 不参与任何流程。** 它无索引，语义为默认界面，同时是 ESC 判断依据；`UIManager.ReportCanvasState` 与 `CanvasFocusStack.ReportState` 都直接 return。新枚举成员必须加在 `Default` 之前，加在后面会静默丢失画布能力。
6. **画布要能被 UIManager 主动关闭，其 `ToggleCanvasEventSO` 必须登记进 `UIManager.toggleCanvasEvents`。** 未登记的画布只能上报状态与刷新层级，`IsClosableCanvas` 返回 false，ESC 与互斥关闭都不会作用于它；此时若向它发关闭请求，焦点栈与真实显隐会错位。
7. **`EditorSettings` 的 Domain Reload 状态与静态状态必须成对。** 持 static 可变状态的新类，要么加 `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` 复位，现例为 `SaveRegistry` 与 `PersistentSceneRegistry`，要么保证订阅方成对退订；否则关闭 Domain Reload 时状态跨 Play 存活，导致坏档或脏数据。

## 4. 已知缺陷与风险

1. `SaveRegistry.Clear()` 目前没有调用点：同进程重开新局时静态注册表保留旧局内容，旧局残留的 `ISaveable` 会继续被写入新局存档。项目现状文档《My_ARPG_MVCS项目现状》同样记为待办。
2. `SaveSystem.IsLoadingSaveRequest` 缺 try/finally 保护：`RequestSceneLoad` 抛出的异常被外层 catch 吞掉，标志位停在 true，此后所有 Menu 到 Location 的切换都跳过自动存档写入，且无任何日志。
3. `SceneChanger.isLoading` 同样缺 finally：协程 `UnloadCurrentScenes` 中途异常会让标志位永久为 true，之后所有场景切换请求被静默拒绝。
4. `ICanvasManager.RefreshCanvaOrder` 在 `UIManager.Instance == null` 时静默取 `DefaultOrder`：启动期打开的画布层级会错，且无报错，是跨单例时序最隐蔽的一处。
5. `VisitedHistoryManager` 是空壳单例，占一个单例名额与一个场景组件位。
6. 单例预算已超：项目现状文档《My_ARPG_MVCS项目现状》认定的合规集合为 6 个，实际 19 个具体单例，其中对话历史三件套 `ConversationHistoryManager`、`ItemHistoryManager`、`VisitedHistoryManager` 与寻路三件套 `ObjectsMapManager`、`AStarPathFinder`、`AStarNodeManager` 是明显可合并、可下沉的簇。
7. `RequestSceneLoad` 的广播先于 `isLoading` 置位：任何订阅者在广播段内再发起一次 `RequestSceneLoad` 都会启动第二条卸载/加载协程，标志位拦不住。

## 5. 未核验事项

1. 假设：单例组件挂在常驻场景节点上，跨场景不随内容场景卸载。本域只做了脚本静态核对，挂载位置与启用状态待编辑器实测。
2. 假设：`Awake` 顺序在实机上确实无保证，因此 §2.3.2 的重试订阅是必要项。从常驻场景直接进 Play 的对照实验待编辑器实测。
3. 假设：`CameraPixelSnap` 的 `worldToCameraMatrix` 改写与 Cinemachine 的实际共存结果如注释所述。运行期画面对比待编辑器实测。
4. 已核验：编辑器的 Enter Play Mode Options 未启用，Domain Reload 保持默认开启。`SaveRegistry` 的 `ResetStatics` 与 `PersistentSceneRegistry` 的 `Reset` 因此是「关闭 Domain Reload 后仍正确」的前置保险，关闭 Domain Reload 后的实际行为待编辑器实测；§3.7 的纪律不因该实测值改变。
5. 假设：10 个 `ICanvasManager` 实现者的 Inspector 引用 `ToggleCanvasEventSO`、`SceneLoadedEventSO`、`CanvasGroup` 已正确接线，`toggleCanvasEvents` 列表覆盖所有需要被主动关闭的画布。场景资产侧的接线核对待编辑器实测。
6. 假设：`StatsService` 是 `SaveableService` 唯一派生类这一结论在全工程成立，依据是脚本目录的全量文本检索；通过其他程序集或动态类型引入的可能待实测。
