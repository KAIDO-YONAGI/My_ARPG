# UI 基础设施设计与实施计划

> **For agentic workers:** 后续按 `executing-plans` 逐阶段执行，使用下文复选框记录进度。开始实施时重新读取项目路由、核对工作区并取得精确租约。本轮授权范围是编写计划，复选框不代表已经实现。

**Goal:** 为现有 uGUI 工程补充界面注册与闲置缓存、模态弹窗栈、循环列表/网格，并沿用项目现有 MVCS 数据流。

**Architecture:** 由现有 `UIManager` 组合普通 C# 的运行时、缓存与模态栈；普通画布继续使用 `CanvasFocusStack`。虚拟化容器只负责可见区与 cell 复用，玩法域通过 Controller 提供数据与命令，业务状态独立于可销毁的 View。

**Tech Stack:** Unity 2022.3、uGUI、ScrollRect、Canvas/CanvasGroup、TextMeshPro、C#；首版使用序列化的 Prefab 引用。

状态：`待实施的方案草稿`  
最后更新：`2026-10-31`  
文档性质：位于非权威参考区 `Docs/Plans/`，不加入 `Y_MultipleAgentWorkflow/` 的权威索引，不增加其维护计数。  
确认范围：用户已认可总体方向，并要求先在非权威位置保存计划；尚未授权根据本计划修改产品代码、场景或预制体。  
事实基准：2026-10-31 阅读到的当前工作区；执行前必须重读受影响文件，尤其是正在调整的命名空间与常驻场景接线。

## 1. 四个概念分别解决什么

### 1.1 注册、缓存与 LRU

三个对象需要分清：

| 对象 | 含义 | 存活时间 |
|---|---|---|
| 界面定义 | 界面 ID、Prefab、所属层、缓存策略等配置 | 配置资产存活期间 |
| 界面实例 | Prefab 实例化后的 GameObject、控件、Controller | 首次打开至淘汰 |
| 业务状态 | 玩家属性、背包内容、任务进度 | 由对应业务服务决定 |

目录中的定义在启动时注册，实例在首次打开时登记。以后打开先取缓存，找不到再创建；关闭时解除本次绑定、隐藏并进入闲置缓存；淘汰时销毁实例并注销实例记录，定义仍保留。

LRU（最近最少使用）只作用于已关闭的实例。维护一条由旧到新的闲置链表：取用时摘除节点，归还时放到尾部，超预算从头部淘汰。打开的普通界面、被上层遮住的弹窗、常驻 HUD 都不能作为闲置对象回收。

首版收益是减少重复 Instantiate/Destroy 与无效 UI 刷新。序列化 Prefab 引用会继续持有资产；销毁实例不保证它引用的贴图、字体等资产立即卸载，也不保证进程内存立即下降。这一版的缓存预算以闲置实例数量为准。

### 1.2 弹窗栈

普通窗口可以点击后提到前面；模态弹窗有明确的先后关系，例如“放弃任务确认”之上又打开“操作提示”。新弹窗压栈，只让栈顶接收点击、键盘和手柄导航。关闭栈顶后，恢复下层弹窗的交互与原来的选中控件。

专用 Root 负责渲染位置；栈负责顺序和关闭规则。仅把弹窗挂到最后一个子节点不能完成输入隔离，必须同时控制遮罩、Canvas 排序、CanvasGroup 与 EventSystem 的选择对象。

### 1.3 LoopList 与 LoopGrid

数据有 1,000 条，不代表要创建 1,000 个 UI 节点。视口显示 8 行时，仅维持可见行及上下少量预留行；滚出视口的 cell 解绑旧条目，再绑定即将进入视口的条目。

对象池减少重复创建；虚拟化进一步限制同时存活的 cell 数量。两者需要结合。列表和网格共用范围计算与 cell 回收，列表相当于每行 1 列的网格。

### 1.4 MVC、MVP、MVVM 与本项目的取舍

| 模式 | 主要协作方式 | 本项目的选择 |
|---|---|---|
| MVC | Controller 接收操作并更新 Model；View 可订阅或读取 Model | 已有命名继续保留，明确各自职责 |
| MVP | Presenter 读取 Model、响应 View 事件并主动调用显示接口 | 最贴合当前 uGUI 的显式事件与控件引用 |
| MVVM | View 通过绑定系统读取 ViewModel 的可观察属性与命令 | 数据绑定基础设施带来额外工作，首版不引入 |

这些名称不是严格统一的行业接口标准。本项目采用既有的 MVCS/MVP 风格，不为了命名重命名现有 Controller：

```text
View 的操作事件 -> Controller -> 业务 Service/Model 的命令
业务变更事件   -> Controller -> View 的显示方法
UIManager      -> 界面实例的打开/关闭/排序，与业务数据流分工
```

Model 负责状态与规则；Service 提供业务写入口；Controller 负责展示数据和界面流程；View 持有控件。缓存销毁 View 时，不能顺带销毁业务状态。

## 2. 已核对的工程现状

本节为取证摘要，后文新增类型均为拟议内容。

| 当前事实 | 证据文件（相对工程根） | 对落地的影响 |
|---|---|---|
| `UIManager` 管按键、外部请求、互斥面板、阻塞声明和切场关闭 | `Assets/Scripts/Pipeline/UI/SystemCanvasManagers/UIManager.cs` | 沿用入口，新增能力由它组合，不新增全局 UI 单例 |
| `CanvasFocusStack` 使用画布类型链表；重报开启会把画布移到末尾 | `Assets/Scripts/Pipeline/UI/SystemCanvasManagers/CanvasFocusStack.cs` | 是焦点排序容器，不是只能 Pop 的模态实例栈 |
| 显隐由 CanvasGroup 的 alpha/interactable/blocksRaycasts 控制 | `Assets/Scripts/Contracts/ICanvasManager.cs` | 当前“关闭”通常不会停用 GameObject，不能直接等同于缓存生命周期 |
| 属性面板已有 Model/Service/Controller/View | `Assets/Scripts/Gameplay/Player/Controllers/StatsPanelController.cs`、`Views/StatsPanelView.cs` | 适合作为首个可销毁界面试点 |
| 属性 Controller 构造时订阅，Dispose 时退订；View 在 Start 创建它 | 同上 | 缓存后反复显示不再依赖 Start，只调用一次的语义；需增加显式绑定与解除绑定 |
| 通用池按组件类型、Prefab、Queue 管理；`TotalCreated` 是累计创建计数 | `Assets/Scripts/Pipeline/ObjectPool.cs` | 不直接承担不同界面的 LRU；首版 cell 池由容器拥有，避免全局池的累计上限和场景销毁语义干扰 |
| 任务进度与画布、槽位引用共存在同一 Manager | `Assets/Scripts/Gameplay/Quest/QuestManager.cs` | 不能把包含该 Manager 的整个面板直接交给 LRU 销毁 |
| 任务槽位的“已完成”外观由当前槽位遍历设置 | `Assets/Scripts/Gameplay/Quest/QuestManager.cs`、`QuestLogSlot.cs` | 复用时必须由数据重新生成外观，不能只修改当前可见槽位 |
| 背包真实物品和数量存在 InventorySlot 组件中 | `Assets/Scripts/Gameplay/Inventory/InventorySlot.cs`、`InventoryManager.cs` | 背包迁移虚拟化前必须先建立独立于 cell 的槽位模型 |
| 商店填充固定 ShopSlot 数组，卖价在该数组内查找 | `Assets/Scripts/Gameplay/Shop/ShopManager.cs` | 先把价格查询改为商品数据查询，再把商品展示改为虚拟化 |
| 常驻场景已经容纳多个业务 Canvas；FadeCanvas 的序列化 order 为 1000 | `Assets/Scenes/GameScene/PersistentScene.unity` | Runtime、ModalRoot 和 CacheRoot 放在常驻场景，不误认为所有 UI 会随内容场景卸载 |
| 普通焦点最高 order 为 90，GameOverCanvas 的序列化 order 为 99 | `CanvasFocusStack.cs`、`PersistentScene.unity` | 需要独立的模态排序段，并定义 GameOver 强制抢占规则 |
| SceneChanger 的公开入口先广播加载请求，再执行加载逻辑 | `Assets/Scripts/Pipeline/Scene/SceneChanger.cs` | 在现有 UIManager 订阅入口清理 UI，不需要另建场景加载通道 |
| 当前运行时脚本已有 `Contracts`、`Pipeline.*`、`Gameplay.*` 命名空间 | 上述当前源码 | 按当前命名空间接入；旧 Guide 中全局命名空间的统计不作为本计划依据 |

本轮观察到场景、SceneChanger 与其他脚本存在并发修改。此文只记录所读快照，不覆盖这些改动；用户要求本轮仅写非权威文档，因此权威 Guide 的同步留到对应改造任务处理。

## 3. 方案边界与阶段关系

推荐渐进方案：在现有 UIManager 上组合缓存与模态能力，虚拟化作为独立组件交付。相比把所有功能继续塞进 UIManager，这样更容易控制职责；相比首版全面迁入第三方框架和 Addressables，这样迁移面更小。

计划分为三个可独立验收的子项目：

1. **界面运行时**：注册、实例生命周期、LRU、模态栈、Stats 与通用确认框样例。
2. **虚拟化容器**：固定尺寸的纵向 LoopList/LoopGrid，以及独立样例场景。
3. **业务迁移**：任务列表先接入；商店、背包满足数据独立条件后分别迁移。

依赖关系：阶段 1→2→3；阶段 4→5 可独立开发；阶段 6 使用前述接口迁移任务；阶段 7 规划商店与背包扩展。

首版支持直接引用 Prefab、普通页面单实例、相同类型的多个弹窗实例、固定行高列表、固定格尺寸网格、纵向滚动。横向滚动、可变高度测量、远程资源加载、热更和全项目统一数据绑定不计入首版。

## 4. 组件与文件布局（拟新增或修改）

下面路径均为相对工程根的未来落点。新增类跟随目录命名空间，不增加 asmdef。领域专用接口放在领域内；UI 框架中的接口留在 `Pipeline.UI` 对应子目录。

| 路径 | 动作 | 职责 |
|---|---|---|
| `Assets/Scripts/Pipeline/UI/Runtime/UIViewDefinition.cs` | 新增 | UIViewId、UILayer、UICachePolicy、界面配置条目 |
| `Assets/Scripts/Pipeline/UI/Runtime/UIViewCatalogSO.cs` | 新增 | 配置列表与启动校验，生成 ID 到定义的映射 |
| `Assets/Scripts/Pipeline/UI/Runtime/UIHandle.cs` | 新增 | 单个实例的身份；相同弹窗类型有不同 Handle |
| `Assets/Scripts/Pipeline/UI/Runtime/UIOpenArgs.cs` | 新增 | 打开参数基类、关闭原因与关闭结果类型 |
| `Assets/Scripts/Pipeline/UI/Runtime/UIView.cs` | 新增 | Initialize/Show/Hide/Dispose 的显式生命周期与本次绑定清理 |
| `Assets/Scripts/Pipeline/UI/Runtime/UIViewCache.cs` | 新增 | 闲置实例的分类取用、LRU、数量及超时淘汰 |
| `Assets/Scripts/Pipeline/UI/Runtime/UIRuntime.cs` | 新增 | Open/Close、实例登记、模态协作、重入防护与 Reset |
| `Assets/Scripts/Pipeline/UI/Modal/UIModalStack.cs` | 新增 | 纯 C# 实例栈与栈顶身份；不持有任务、背包类型 |
| `Assets/Scripts/Pipeline/UI/Modal/UIModalLayer.cs` | 新增 | 弹窗容器、遮罩、Canvas 排序和 EventSystem 焦点 |
| `Assets/Scripts/Pipeline/UI/Modal/ConfirmDialogView.cs` | 新增 | 通用确认框与 ConfirmDialogArgs，仅展示文字和返回结果 |
| `Assets/Scripts/Pipeline/UI/SystemCanvasManagers/UIManager.cs` | 修改 | 初始化 Runtime、分派输入、接入切场清理、转发新 API |
| `Assets/Scripts/Gameplay/Player/Views/StatsPanelView.cs` | 修改 | 接入 UIView 生命周期，打开绑定、关闭退订 |
| `Assets/Scripts/Gameplay/Player/Controllers/StatsPanelController.cs` | 修改 | 注入只读属性来源、可重复 Attach/Detach，首刷和退订幂等 |
| `Assets/Scripts/Pipeline/UI/Virtualization/ILoopDataSource.cs` | 新增 | Count、BindCell、UnbindCell 数据源契约 |
| `Assets/Scripts/Pipeline/UI/Virtualization/LoopLayout.cs` | 新增 | 纯计算：总尺寸、可见区间、条目坐标 |
| `Assets/Scripts/Pipeline/UI/Virtualization/LoopCellView.cs` | 新增 | cell 当前 Index、解绑和视觉复位 |
| `Assets/Scripts/Pipeline/UI/Virtualization/LoopScrollView.cs` | 新增 | ScrollRect 接线、可见索引差集与容器私有 cell 池 |
| `Assets/Scripts/Pipeline/UI/Virtualization/LoopList.cs` | 新增 | 纵向单列配置入口 |
| `Assets/Scripts/Pipeline/UI/Virtualization/LoopGrid.cs` | 新增 | 纵向多列配置入口 |
| `Assets/Scripts/Pipeline/UI/Virtualization/LoopScrollDemo.cs` | 新增 | 样例数据源，演示增删、跳转与网格复用 |
| `Assets/Scripts/Gameplay/Quest/QuestListController.cs` | 新增 | Quest 数据源适配、选择状态与行展示数据 |
| `Assets/Scripts/Gameplay/Quest/QuestLogSlot.cs` | 修改 | 可复用的任务行 View，不持有永久业务状态 |
| `Assets/Scripts/Gameplay/Quest/QuestLogPanel.cs` | 修改 | 托管任务列表 Controller 与详情显示 |
| `Assets/Scripts/Gameplay/Quest/QuestManager.cs` | 修改 | 增加只读任务快照与变更通知，剥离直接槽位遍历 |
| `Assets/Prefabs/UI/Runtime/StatsPanel.prefab` | 新增 | 从现有 StatsCanvas 提取的独立视图 |
| `Assets/Prefabs/UI/Runtime/ConfirmDialog.prefab` | 新增 | 通用确认框，引用本实例内控件 |
| `Assets/Prefabs/UI/Runtime/LoopDemoCell.prefab` | 新增 | 虚拟化样例行/格共用的简单显示单元 |
| `Assets/Scenes/UIVirtualizationDemo.unity` | 新增 | 独立样例场景，避免给正式常驻场景加入假数据 |
| `Assets/GameSO/UI SO/UIViewCatalog.asset` | 新增 | Stats 与 Confirm 首批注册配置 |
| `Assets/Scenes/GameScene/PersistentScene.unity` | 修改 | 常驻根、目录引用、Stats 替换与任务列表接线 |
| `Assets/Prefabs/UI/Slots/QuestSlot.prefab` | 修改 | 任务行复用接线 |

首版 UIViewId 包含 Stats 和 Confirm；Quest 只改列表时继续使用原画布身份，不提前声明它已成为可缓存面板。原 `CanvasToToggle` 保持既有数值和 Default 语义，新旧 ID 的迁移映射由 UIManager 显式管理。

## 5. 界面注册、生命周期与缓存设计

### 5.1 定义与实例身份

定义包含 `Id`、`Prefab`、`Layer`、`CachePolicy` 与 `CloseOnEscape`。`Layer` 为 Page 或 Modal；`CachePolicy` 为 KeepAlive、LRU 或 DestroyOnClose。Page 在 Runtime 中每个 ID 只允许一个活跃实例；Modal 每次打开产生一个独立 Handle，可以嵌套同一种 Confirm。KeepAlive 首版只允许配置给 Page，Catalog 拒绝 Modal 与 KeepAlive 的组合。

拟议调用接口如下；这里只固定边界，不把示例当成已经存在的代码：

```csharp
UIHandle Open(UIViewId id, UIOpenArgs args);
bool Close(UIHandle handle, UICloseReason reason);
bool CloseTopModal(UICloseReason reason);
void TrimIdle();
void Reset(UIResetReason reason);
```

`UIHandle` 包含当前 Runtime 创建时生成的 runtimeId 和单调递增序号，不使用全局静态计数。Reset 清掉实例映射但不复用序号；旧 Runtime 或旧实例的 Handle 调 Close 返回 false，不能关闭新实例。默认值 `UIHandle.Invalid` 表示打开失败；未注册 ID、缺 Prefab、层配置错误、重置中、强阻塞中或超过弹窗深度都返回 Invalid，并记录包含 ID 与原因的诊断，不制造空节点。

`UICloseReason` 至少区分确认按钮、取消按钮、ESC、程序关闭、切场、死亡、宿主销毁和打开失败；`UIResetReason` 区分切场与宿主销毁。死亡使用内部的 CancelModals（关闭原因为死亡），不要求把所有闲置页面一起清空。所有类型在 UIOpenArgs.cs 中定义，Confirm 的结果由关闭原因映射，只有确认按钮映射为 Confirmed。

### 5.2 生命周期与数据订阅

```text
定义已注册 -> 创建未激活实例 -> Initialize 一次 -> Show 每次打开
                                                    |
                         Modal 被覆盖 <-> 恢复栈顶  |
                                                    v
                                              Hide 每次关闭
                                                    |
                                  ClosedCached <----+----> Dispose/Destroy
                                       |
                                  再次 Show 或淘汰
```

被覆盖的弹窗仍处于打开状态，保留上下文和必要的数据订阅，仅失去交互；不进入 LRU。真正关闭才解除本次业务订阅、清空打开参数和回调，并按策略缓存或销毁。

创建流程在 inactive 的 CacheRoot 下实例化，随后明确设置实例 inactive，再注入参数并完成 Initialize，然后移到目标 Root 并 Show。业务绑定不能依赖 Awake/OnEnable/Start 的相对顺序；Prefab 上的 OnEnable 也不能读取尚未注入的业务参数。

UIView 的 Initialize 负责一次性控件缓存和按钮接线；Show 负责本次 Controller 绑定及首刷；Hide 负责退订、取消本次工作、清除选中和参数；Dispose 负责最终资源清理且允许重复调用。关闭状态不继续写 Text/Image 或执行面板自己的 Update。

StatsPanelController 改为持有注入的 `IPlayerStatsReadOnly`；Detach 从原订阅对象解绑，避免服务实例变化时对新对象退订。StatsPanelView 在自身领域内获取 StatsService；Pipeline 不引用 Gameplay.Player。

### 5.3 LRU 数据结构与策略

运行实例表归 Runtime；Cache 仅持有闲置实例。闲置对象有全局 LRU 链表节点及所属 ID 的闲置链表节点，取用或淘汰都同时摘除两个节点，避免一处删除另一处仍可取用。

```text
Open(id):
    活跃 Page 已存在 -> 更新展示参数并刷新，提升焦点，返回同一 Handle，不重复绑定
    有该 Page 的关闭中 KeepAlive 实例 -> 取回该实例并 Show
    否则从该 id 的闲置集合取实例；命中时同步移出全局 LRU
    未命中则实例化；登记、Show；Modal 再入栈

Close(handle):
    非活跃 Handle 或非栈顶 Modal -> 返回 false
    标记关闭中，移出活跃映射/模态栈，Hide
    KeepAlive -> 保留为该 Page 的关闭实例，脱离 LRU
    LRU -> 挂到该 id 闲置集合与全局 LRU 尾部
    DestroyOnClose -> Dispose、Destroy、注销
    TrimIdle；结构和焦点恢复完毕后交付关闭结果

TrimIdle():
    从 LRU 头开始销毁，直到数量预算满足
    超时扫描也从头开始；已失效的 Unity 对象直接清理记录
```

初始可调值为 `MaxIdleInstances = 4`、`IdleTimeoutSeconds = 120`、`TrimIntervalSeconds = 1`。它们是起始配置，不是测量得到的性能结论。数量为 0 时关闭即淘汰；超时为 0 时关闭超时策略，仅保留数量限制；用 unscaled time，暂停时也能回收。

KeepAlive 仅用于少量确认需要保留的普通页；模态默认 LRU。自动回收不碰打开的实例。CacheRoot inactive 只防止继续更新，不释放业务引用；Hide 必须主动清理这些引用。

### 5.4 同帧重复、失败与重入

每个 Handle 有关闭中标记，按钮与 ESC 同帧命中也只关闭一次。UI 生命周期回调期间产生的 Open/Close 请求排队到本次状态提交后处理，避免遍历期间改集合。

Show 或绑定失败时，撤销入栈和活跃登记，Dispose 失败实例并恢复前一个栈顶。销毁清理以 finally 收尾，单个 View 的异常不阻止后续记录注销。首版不带异步资源加载，保留挂载前初始化边界；后续若加入异步，需要单独设计加载合并、取消与资源句柄释放。

## 6. 模态层、关闭语义和输入

### 6.1 常驻层级与排序

```text
PersistentScene
  UIManager                 组合 Runtime、Cache、ModalStack
  UIRoot
    PageRoot                接管后的普通页面；各 Canvas 保留焦点排序
    ModalRoot               专用模态宿主
      ModalEntry[0]         遮罩 + 弹窗
      ModalEntry[1]         遮罩 + 弹窗
    CacheRoot (inactive)    已关闭实例
  其余现有 Canvas
  FadeCanvas                现有过渡层
```

不批量搬动全部现有 Canvas。只对已迁移界面变更父节点与生命周期。

基于当前排序，首版使用普通焦点区间至 90、现有 GameOver 为 99、模态区间 200–215、Fade 为 1000。第 d 层遮罩 order 为 `200 + 2*d`，弹窗为 `201 + 2*d`，d 从 0 起，最大深度 8。专用 Canvas 的 overrideSorting 和 sorting layer 必须一致；Prefab 内的 Canvas 不允许自行越过分配区间。第 9 个弹窗请求返回深度错误，不挤占 Fade 层。

GameOver 属于强制流程：当前死亡控制器通过 `RequestCanvasToggle(GameOver)` 排队，所以需要在消费普通模态输入之前优先识别这个请求，先进入短暂的 UI 请求拒绝区、取消普通模态，再走既有画布事件展示 GameOver。若 GameOver 已开，重复开启请求只消费，不把它切成关闭。直接收到它的开启/阻塞声明时也执行同样的模态清理。强阻塞画布存在时拒绝新的普通弹窗，避免出现 order 更高的弹窗盖住必需的死亡操作。Fade 始终覆盖 UI。

### 6.2 交互隔离与关闭结果

下层 Modal 的 CanvasGroup 设置为不可交互且不拦射线；栈顶允许交互，其全屏遮罩拦截普通页面的射线。栈顶弹窗与遮罩之间的父子顺序、Canvas order 都由宿主统一设置。下层 Page 的手柄/键盘导航也需临时禁用，不能只靠遮罩阻止鼠标。

打开弹窗时记录 EventSystem 当前选择对象，清空旧选择，设置本弹窗默认按钮；关闭后仅当原对象仍有效、激活且可交互时恢复，否则选下层弹窗默认按钮，栈空时按普通页规则恢复。

模态关闭规则固定如下：

| 请求 | 行为 |
|---|---|
| CloseTopModal | 仅关闭当前栈顶 |
| Close(下层模态 Handle) | 返回 false，保持栈不变 |
| ESC 且栈顶可关闭 | 关闭栈顶并消费本帧请求 |
| ESC 且栈顶不可关闭 | 消费请求，保持当前栈，不向下穿透 |
| 点击遮罩 | 首版只拦截输入，不自动关闭 |
| 切场、死亡、宿主销毁 | 内部 ForceReset 从顶到底关闭全部；业务代码不调用此接口越过栈顶 |

ConfirmDialogArgs 包含标题、正文、确认/取消文本及结果回调。结果包含 Confirmed、Cancelled 及关闭原因；只有显式确认按钮产生 Confirmed。业务写命令放在调用方的确认分支；Bind、刷新、退订不发奖、不交易、不改任务状态。

所有关闭路径的回调最多交付一次。先复制结果和回调、清除 View 引用、完成出栈及交互恢复，再调用业务回调。Reset 期间拒绝回调产生的新 Open，请求返回重置中错误，防止取消一个弹窗又把它重开。

### 6.3 UIManager 的输入桥接

保持现有强阻塞规则，在正常输入流程中增加模态优先级：

```text
CollectInputBindingRequests
 -> 收到 GameOver 强制请求：拒绝重入 Open、取消模态、展示死亡画布、结束本帧
 -> 存在强阻塞画布：清理不应共存的模态，按现有规则消费请求
 -> 模态栈非空：处理栈顶 ESC，消费其他普通画布切换请求
 -> 模态栈为空：现有 ESC 与普通面板焦点流程
 -> 清空本帧请求
```

一次 ESC 只关闭一个弹窗，不能在出栈后继续关闭底下的页面。外部 UIManager.RequestCanvasToggle 也经过同一输入规则，其中 GameOver 使用上述强制分支；程序化开启普通页面由 Runtime 检查模态状态，不绕过限制。原有 ToggleCanvasEventSO 仍登记在 UIManager 中，由 UIManager 桥接到迁移后的 Stats，不保留第二个同时负责显隐的旧 Stats 监听者。

这里的模态阻塞覆盖 UI 射线、UI 导航和普通面板快捷键。玩家移动/攻击属于独立输入链路，当前 `BlocksGlobalInput` 不能据名称推断会屏蔽它们。首版不增加全局暂停行为；需要冻结战斗的业务弹窗应由调用方结合现有暂停/输入机制另行设计，不能在 Hide 中无条件恢复玩家输入。

### 6.4 切场与宿主销毁

UIManager 已订阅加载请求：在同一收尾入口调用 Runtime.Reset，递增本次上下文版本、丢弃旧队列、强制关闭模态、关闭迁移后的普通页、清空 LRU 中可能持有旧场景引用的实例，并调用现有画布复位。常驻 Root 保留。

KeepAlive 页面在切场时也必须 Hide 并释放业务上下文；如果无法证明没有旧场景引用，按销毁处理。所有回调得到取消结果，不能执行确认命令。

当前加载请求广播早于后续加载校验，因此不能仅因收到广播就把 UI 永久锁住并等待“加载完成”；无效请求可能没有完成通知。首版只做同步清理，不新增这一长时锁。宿主 OnDestroy 再次执行幂等 Reset/Dispose，不依赖 OnDisable 一定先发生。

## 7. 循环列表与网格的算法和接口

### 7.1 共同契约

拟议 `ILoopDataSource` 提供 `Count`、`BindCell(LoopCellView cell, int index)` 与 `UnbindCell(LoopCellView cell, int oldIndex)`。容器持有数据源引用与实际 cell；数据源负责把 index 映射到当前业务条目。`BindCell` 只显示数据、建立本次点击绑定，不能执行接任务、买卖等写命令。

容器公开 SetDataSource、RefreshData、RefreshItem、ScrollToIndex；列表固定 1 列，网格由配置 ColumnCount 决定列数。修改列数、cell 大小或视口尺寸会整体重算布局，但不会改变业务数据。

固定尺寸纵向布局约定如下：

```text
N = Count，C = 列数，h = cell 高，s = 竖向间距，H = 视口高
Pt/Pb = 上/下内边距，B = 上下各预留 1 行
rows = ceil(N / C)
rawHeight = Pt + Pb + rows*h + max(0, rows-1)*s
contentHeight = max(H, rawHeight)
pitch = h + s
y = clamp(content.anchoredPosition.y, 0, max(0, rawHeight-H))
firstRow = max(0, floor((y-Pt)/pitch)-B)
lastRow = min(rows-1, ceil((y+H-Pt)/pitch)+B)
firstIndex = firstRow*C
lastIndexExclusive = min(N, (lastRow+1)*C)
```

N 为 0 时直接使用空区间，清理所有绑定并归零滚动位置。C≥1、h>0、s≥0、内边距≥0，否则配置失败并停用该容器。网格的横向坐标按列与格宽/间距计算，宽度不足时明确配置错误；自适应列数可在后续业务要求下加入。

### 7.2 创建、复用与复杂度

容器维护 `Dictionary<int, LoopCellView>` 表示活跃索引，维护自己的闲置 cell 集合。每次滚动只解绑离开区间的索引，为新进入区间的索引取空闲 cell 并绑定；仍在区间内的 cell 不重复赋文字、贴图。大幅跳转的成本与目标窗口大小有关，不逐条经过中间数据。

创建窗口所需数量的保守上界为 `min(N, C*(ceil(H/pitch)+2+2*B))`，视口缩小时可以保留该容器曾需要的少量空闲 cell，释放数据源或销毁容器时统一销毁。不会按 N 预热。数据变动后的快照或排序可以是 O(N)，普通滚动不得全量遍历 N。

每个 cell 绑定前覆盖标题、图标、数量、alpha、可交互状态、完成态、选择态和点击目标。解绑清空当前条目、关闭 tooltip、移除本次监听并取消本次工作。点击监听读取当前绑定的条目或其稳定身份，不捕获第一次 Bind 时的 index。初始化按钮监听与条目数据绑定分开，避免多次 Bind 累加监听。

### 7.3 布局、数据变动与焦点

Content 使用上方锚点和向下增大的偏移；容器手工设置 cell 的 anchoredPosition 与 Content 高度。受控 Content 不再同时使用 VerticalLayoutGroup、GridLayoutGroup 或 ContentSizeFitter，以免与虚拟化重复控制布局。Mask/RectMask2D 保留裁剪，ScrollRect 保留拖动和惯性。

滚动索引用钳制后的 y 计算，避免弹性回弹时产生负索引。容器在 OnEnable、视口尺寸变化、数据源变化时重新计算；订阅和退订 ScrollRect.onValueChanged 成对。已有 `ScrollbarFix` 若已接线，保持它的滚动条单向同步方式，不另加第二个改写 normalizedPosition 的同步器。

同条目数据变化用 RefreshItem；数量、过滤和排序变化用 RefreshData。数据源变更时先解绑旧数据源，再绑定新数据源。重排时按条目身份保存选择与顶部锚点；身份仍存在则恢复，否则选择第一个有效条目并钳制位置。不能把复用中的 index 当成任务或背包物品的永久身份。

手柄/键盘焦点由逻辑选中条目维护：目标离开视口时先 ScrollToIndex，等新 cell 绑定后再交给 EventSystem。正在回收的 cell 被选中时先移开选择对象，避免输入落到已改绑的同一 GameObject 上。列表被模态覆盖时暂停其导航。

## 8. 业务迁移约束

### 8.1 属性面板：先验证可销毁 View

Stats 数据已在常驻 StatsService/PlayerStatsModel，可只提取视图 Prefab。打开按当前只读模型首刷；关闭 Detach，隐藏期间不刷新；LRU 淘汰后再打开重新创建 View/Controller，显示当前属性。

现有热键、集成菜单与焦点仍经过 UIManager。桥接过程中只能有一条开关消费链；旧 StatsCanvas 的组件与场景引用在替换提交中一起清理，保留已有脚本 meta/GUID。

### 8.2 任务列表：先虚拟化展示，不销毁进度宿主

QuestManager 暂时保留常驻业务状态。增加按当前任务板返回任务列表快照的只读入口与列表变更通知；QuestListController 构造对应行数据，负责稳定的任务身份、完成态与选择态。

InitiateQuestSlots 的显示循环迁到数据源；SetQuestSlotToDoneState 的效果改成状态变更后的行重绑定，包含“已完成且当前不在视口内”的条目。QuestLogSlot 不再依赖固定数组位置。

任务详情渲染只读当前状态、目标和奖励。当前 OpenQuest 会重放 QuestStateChanged，而 Completed 分支会发奖；迁移时必须把“选择/显示任务”与“提交完成任务”分离，防止 Bind、首刷、滚动回来或重新打开面板触发奖励。任务状态、进度统计和奖励通道保留原有业务含义；提交完成只能由显式完成命令触发，并对已完成状态做重复提交保护。

现有 `QuestOptionsEventSO` 和奖励事件资产继续使用原资产引用；业务写入口由 Controller 调用或转发。任务整页进入缓存是后续步骤，必须先把 QuestManager 从可销毁视图树中移出，并完成详情与按钮组引用分离。

### 8.3 商店：价格查询先独立于可见槽位

现有 SellItem 遍历 ShopSlot 定价。虚拟化后，滚出视口的商品不应失去可卖出的价格，因此先建立当前商人/当前分类的商品数据查询；首版保持既有的当前分类、首次匹配规则，再替换展示数组。需要改变不同分类的卖价策略时单独确认业务语义。

购买、卖出继续经 IShopInteractable 和 InventorySlotsStatsSO 结算。cell 解绑后清理 tooltip 与商人引用，离开商人触发关闭时取消该商人的弹窗上下文。

### 8.4 背包：数据模型拆分是前置工作

当前 InventorySlot 自身持有 itemSO 和 quantity；直接复用它会把显示格的变化当成背包内容变化。先规划纯数据槽位（稳定槽位身份、物品、数量）及服务命令，再将 InventorySlot 变成展示与操作入口。

迁移必须保留快捷栏优先于背包的填充顺序、堆叠上限、空槽不变量、出售前选中槽位、拾取余量掉落和物品史口径。背包模型拆分范围明显大于 UI 容器，因此单独执行，不能在接一个 LoopGrid 时顺手替换。

## 9. 分阶段实施任务

各阶段开工前都要重读对应 Router/Guide、代码与资产，并取得产品文件、场景/Prefab 的写租约。下面是待执行动作；本轮仅完成文档。

### 阶段 1：注册与基础生命周期

文件：新增 §4 的 Runtime 下 UIViewDefinition、UIViewCatalogSO、UIHandle、UIOpenArgs、UIView、UIRuntime；修改 UIManager；新增目录资产与 Stats Prefab。

- [ ] 核对当前命名空间、StatsCanvas 现有父节点和引用，记录替换所需字段及 UnityEvent 接线。
- [ ] 定义 Page/Modal、三种缓存策略、Stats/Confirm ID、Handle 和错误/关闭原因；按 §5.1 固定 API。
- [ ] 实现 Catalog 的重复 ID、空 Prefab、UIView 缺失和非法策略校验；初始化失败时只禁用新 Runtime，保留现有 UI 流程。
- [ ] 实现 inactive 创建、Initialize/Show/Hide/Dispose；按钮接线一次，业务订阅跟随显示期。
- [ ] UIManager 在自己的初始化钩子组合 Runtime，不在 Awake 跨单例取业务数据；根节点和 Catalog 使用序列化引用。
- [ ] Stats Controller 注入只读来源；Stats View 改为显示期绑定；替换时移除旧开关监听链。
- [ ] 打开 Stats、关闭、改变玩家属性、再次打开；确认只显示当前模型值且没有重复面板。

阶段结果：新 Runtime 与旧焦点栈共存，Stats 成为可独立创建与销毁的 View。先使用 DestroyOnClose 验证重建路径，再接缓存。

### 阶段 2：闲置缓存与淘汰

文件：新增 UIViewCache；修改 UIRuntime、UIViewCatalogSO、UIManager 的关闭/清理接线。

- [ ] 实现按 ID 取用和双闲置链表，所有增删按 §5.3 同步更新。
- [ ] 实现数量预算、unscaled 超时、无效 Unity 对象剔除与 Dispose 幂等。
- [ ] 为 Runtime 提供实例创建数、活跃数、闲置数和淘汰数的只读诊断数据，不在每帧打印日志。
- [ ] 将 Stats 策略切为 LRU：关闭后确认 View 无业务订阅、CacheRoot 中实例 inactive。
- [ ] 以小预算打开/关闭多个开发样例实例，核对最久闲置实例先淘汰，正在显示实例从不淘汰。
- [ ] 核对同帧重复 Close 和 Show 失败的撤销路径，确认缓存与活跃表无重复 Handle。

阶段结果：缓存命中时复用实例，预算溢出时可预测地销毁闲置实例。

### 阶段 3：模态栈与确认框

文件：新增 §4 的 Modal 文件及 ConfirmDialog Prefab；修改 UIRuntime、UIManager、PersistentScene。

- [ ] 按 §6.1 建立 ModalRoot、CacheRoot 和每层遮罩/弹窗 Canvas，配置最大深度及排序。
- [ ] 实现栈顶关闭、非栈顶拒绝关闭、ForceReset 从顶到底清理；入栈和出栈只发生在 Runtime 真实状态提交时。
- [ ] 实现上层交互、下层导航隔离、EventSystem 选择保存与恢复。
- [ ] 按 §6.3 插入输入桥接：模态 ESC 消费后立即结束本帧，普通面板请求不得穿透。
- [ ] 把排队的 GameOver 请求提升到模态消费前，接入死亡取消模态与切场同步 Reset；不能仅等待可能缺失的加载完成事件解除锁。
- [ ] ConfirmDialogView 只返回关闭结果；调用方确认分支执行命令，取消和重置不会执行命令。
- [ ] 连开两个相同 Confirm，逐个关闭；同时触发按钮/ESC；打开 GameOver；切场取消；分别核对回调一次与焦点恢复。

阶段结果：相同类型弹窗可嵌套，只有栈顶可交互，取消路径和普通画布互不串线。此阶段完成界面运行时子项目。

### 阶段 4：固定行高 LoopList

文件：新增 §4 的 Virtualization 中 ILoopDataSource、LoopLayout、LoopCellView、LoopScrollView、LoopList、LoopScrollDemo；新增样例 Prefab 与场景。

- [ ] 按 §7.1 固定坐标、尺寸、空数据与区间约定，将计算集中到 LoopLayout。
- [ ] 实现活跃索引差集与局部 cell 池；滚动仅处理离开/进入区间的索引。
- [ ] 实现首次绑定、完整解绑、重复 Bind 的监听清理与容器销毁；数据源切换先解绑旧源。
- [ ] 实现 RefreshItem、RefreshData、ScrollToIndex 以及视口变化后的重算。
- [ ] 在独立样例场景放入 0、1、100、1,000 条数据，支持增删与跳转；假数据不加入正式常驻场景。
- [ ] 记录 cell 创建峰值与 Bind 次数，核对数量上界和大幅跳转成本；容器销毁后清理全部 cell。

阶段结果：单列纵向虚拟化可独立使用，不依赖任务域。

### 阶段 5：LoopGrid 与交互边界

文件：新增 LoopGrid；修改 LoopLayout、LoopScrollView、LoopScrollDemo 及同一样例场景。

- [ ] 用相同核心增加 C 列映射，计算最后一行不足 C 个条目的情况与 cell 横向坐标。
- [ ] 增加列数/尺寸校验、分辨率变化后的布局重算和位置钳制。
- [ ] 增加按条目身份恢复选中与顶部锚点的示例，回收前清理 EventSystem 对该 cell 的选择。
- [ ] 核对 B=1 的上下预留行、弹性回弹、拖动滚动条和快速拖到底，无负索引或残留条目。
- [ ] 模态覆盖样例列表时暂停底层导航，关闭模态恢复仍有效的条目选择。

阶段结果：列表与网格共用核心，容器及交互可以独立验收。

### 阶段 6：任务列表的 MVP 迁移

文件：新增 QuestListController；修改 QuestManager、QuestLogPanel、QuestLogSlot、QuestSlot.prefab 和 PersistentScene。

- [ ] 读取 Quest_Guide 并核对当前完成/领奖调用链；先把纯选择/渲染与显式完成命令分开，加入重复完成保护。
- [ ] QuestManager 增加只读任务快照与变更通知；任务进度字典继续由它持有，不放进 cell。
- [ ] QuestListController 实现数据源，集中构建标题、等级、完成态与选中态。
- [ ] QuestLogSlot 解绑时清空当前 Quest 与点击目标；Bind 覆盖所有视觉状态，不调用 OpenQuest/QuestStateChanged。
- [ ] 用 LoopList 替换固定槽位数组显示流程；任务详情仍使用现有视图，由 Controller 协调选择。
- [ ] 检查 Prefab 内部引用与按钮 UnityEvent，移除 QuestManager 对固定 questLogSlots 的遍历依赖。
- [ ] 核对滚出/滚回的已完成任务、打开关闭、任务板切换、重复刷新：外观正确且不会重复发奖。

阶段结果：任务列表可以容纳超过现有槽位数的任务，UI 重建不改变任务状态。任务整页仍不进入 LRU。

### 阶段 7：商店与背包分别制定迁移提交

文件：商店以 ShopManager、ShopSlot、ShopInfoPanel、ShopCategoryToggles 与常驻场景为范围；背包以 InventoryManager、InventorySlot、UseItem、BackpackCanvasManager 与相关资产为范围。

- [ ] 商店先让 SellItem 查询当前分类的数据价格，核对可见区外同商品的卖出结果与现有规则一致。
- [ ] 商店再接 LoopGrid，每次复用重新绑定商品、价格和商人上下文。
- [ ] 背包先明确槽位数据模型与服务接口，逐项列出现有填充顺序、堆叠、拾取、使用、丢弃和交易不变量。
- [ ] 背包数据脱离 InventorySlot 后，再设计 cell 到稳定槽位身份的绑定；快捷栏保留自己的固定显示需求。
- [ ] 每个业务分别记录实际改动与验证结果；需要使整个业务页进入 LRU 时，先移走其中的常驻服务和状态。

阶段结果：具备继续迁移的清晰前置条件。背包模型实现需要独立细化计划，不计入前三个子项目的首版完成条件。

## 10. 后续实施的验收标准

以下是未来实施时的验收清单，本次写文档没有运行 Unity 编译、场景操作、测试或性能测量。自动化测试的新增与运行待用户明确要求测试/验证后执行。

| 范围 | 验收条件 |
|---|---|
| 注册 | 未注册 ID、重复 ID、空 Prefab 都有可定位错误，原 UI 流程不因此失效 |
| 生命周期 | 每次打开首刷；关闭退订；淘汰后重建显示当前数据，View 销毁不改业务状态 |
| 缓存 | 预算内关闭再开命中同一实例；溢出先回收最久闲置者；打开实例不回收 |
| 模态 | 同类型可多实例；只关闭栈顶；一次 ESC 只关闭一个；深度上限明确失败 |
| 输入与焦点 | 鼠标、导航、快捷键不穿透模态；关闭后只恢复有效且可交互的选择 |
| 强制流程 | 切场/死亡清空模态且回调只交付取消一次；Fade 位于模态上方 |
| 列表/网格 | 节点数随视口变化，满足 §7.2 的上界；滚动不随 N 全量更新 |
| 数据变动 | 空数据、过滤、增删、重排、尾行不足、分辨率变化无越界与残留状态 |
| 任务 | 已完成条目滚回后仍禁用/淡化；Bind、重新打开和纯选择不会再次发奖 |
| 回归 | 原普通面板焦点/互斥/ESC、集成菜单、GameOver、切场复位维持既有业务语义 |

性能记录在同一设备、相同分辨率和同一数据下比较：界面反复开关的创建/销毁计数、滚动时 Bind 次数、Layout/Canvas 重建耗时、GC 分配与 cell 峰值。先记录基线，再判断收益；不在未测量前承诺毫秒数或内存下降比例。

如后续获得测试授权，纯状态回归可放在 `Assets/Tests/Editor/`，分别覆盖 LRU 淘汰顺序、模态栈顶约束和 LoopLayout 的空数据/最后一行/回弹区间；Unity 对象与 EventSystem 行为在样例场景核对。现有 CanvasFocusStackTests 的断言应保持成立。

执行 EditMode 测试使用 `Y_MultipleAgentWorkflow/Workflow/Project_Validation_Guide.md` C12 的既有命令，先检查编辑器占用并取得运行资源租约，以结果 XML 为准；本计划不重复固化当前机器的测试计数。

## 11. 文档与落地后的维护

当前计划保留在 Docs/Plans，不改权威索引、Router、Guide 或维护计数，也不把拟议类型描述为已有能力。

后续每阶段实际完成后，只根据真实代码与验证结果同步对应权威文档：UI 组合与生命周期归 Architecture.Composition，MVCS 边界归 Architecture.Layering，任务迁移归 Gameplay.Quest，商店/背包迁移归 Gameplay.InventoryShop，Prefab/事件资产接线归 Assets；触及切场语义时同步 SceneFlow。命名空间已有独立改造，应由该任务按实际结果同步，不依据本计划回退代码。

代码与 Unity 资产分阶段形成可审阅的提交；Scene/Prefab 改动与对应脚本接线一起审查，新增 Unity 资产携带其 meta。每次结束释放租约；发生并发改动先核对，不覆盖他人的未提交内容。

本文件记录的是设计边界和实施顺序。进入任何产品实现阶段，需要用户后续明确开始实施的指令，并以当时的工作区重新确认落点。
