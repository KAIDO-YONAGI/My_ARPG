# 待办红点 Guide

文档 ID：`ASSETS-REDDOT-GUIDE`  
状态：`Active`  
最后更新：`2026-11-14`

## 维护规则

红点树结构、表现绑定、待办导航或叶子判定变化时更新本文。画布开关与焦点栈见[组合契约](../Architecture/Composition/Composition_Guide.md)，缓存生命周期见[分层说明](../Architecture/Layering/Layering_Guide.md)，调用链总览见[红点调用与引用链路](../../Docs/Plans/RedDot_Plan.md)。本文记录当前代码与资产的结构、调用和引用关系。

## 文档内目录

- [1. 数据与引用归属](#1-数据与引用归属)
- [2. 初始化与计数传播](#2-初始化与计数传播)
- [3. Skills 调用链](#3-skills-调用链)
- [4. Quest 调用链](#4-quest-调用链)
- [5. Stats 调用链](#5-stats-调用链)
- [6. Image 与预制体引用链](#6-image-与预制体引用链)
- [7. 待办导航与画布调用链](#7-待办导航与画布调用链)
- [8. 生命周期与行为边界](#8-生命周期与行为边界)

## 1. 数据与引用归属

树采用 Parent 与 List，节点是普通 C# 对象。当前运行结构为根节点、画布分支、业务叶子三层：

```text
RedDotManager.Root
├─ Skills 分支 → 每个技能槽位的 SkillRedDotLeaf
├─ Quest 分支  → 每个已接取任务的 QuestRedDotLeaf
└─ Stats 分支  → 一个 StatsRedDotLeaf
```

| 持有者 | 数据与引用 | 作用 |
|---|---|---|
| `RedDotNodeBase` | Count、Parent、单个 Image，Image 通过 BoundImage 只读暴露 | 保存计数、父关系与当前表现 |
| `RedDotBranchNode` | `List<RedDotNodeBase> children`，通过 Children 只读访问 | 汇总直接子节点；根与画布分支共用本类型 |
| `RedDotLeafBase` | `CountChanged` 事件 | 变化时携带自身引用通知 Manager |
| `RedDotManager` | root、canvasBranches、canvasGates、canvasImages | 保存树、CanvasType 到分支/可用性回调/Image 的映射 |
| `RedDotManager` | navigationOrder、lastNavigatedCanvas、pendingCanvas、pendingFrame | 保存导航顺序、成功游标与待确认请求 |
| `SkillTreeManager` | `Dictionary<string, SkillRedDotLeaf> redDotLeaves` | 以槽位稳定路径关联技能状态与叶子 |
| `QuestManager` | trackedQuests、questProgress、`Dictionary<QuestSO, QuestRedDotLeaf> questRedDotLeaves` | 已接取集合、任务进度与任务叶子索引 |
| `StatsRedDotTracker` | leaf、实际订阅的 IPlayerStatsReadOnly、伤害与速度比较基线 | 保存属性提升的未查看状态与事件订阅 |

通用节点与 Manager 位于 `Pipeline.UI.RedDot`，具体叶子属于 `Gameplay.Skills`、`Gameplay.Quest`、`Gameplay.Player`。Manager 通过基类、CanvasType 与可用性回调连接业务。

计数为非负整数。业务叶子使用 0 或 1，分支等于直接子节点计数之和。Count 大于零时启用 Image。

## 2. 初始化与计数传播

RedDotManager 的 `OnSingletonInitialized` 创建根节点，`OnEnable` 启用导航输入，`OnDisable` 停用输入。画布分支由 `RegisterCanvas` 或 `AddLeaf` 经 `EnsureCanvasBranch` 按需建立，并挂到根下；此前登记的画布 Image 在分支建立时补绑。

Skills 在 Start 与 LateUpdate 尝试读取技能预制体并建立叶子；Quest 在 OnEnable、Start 与 LateUpdate 尝试红点初始化；StatsRedDotTracker 在 OnEnable、Start 与 LateUpdate 尝试取得数值源、订阅事件并建立叶子。初始化标记阻止重复建立，LateUpdate 的这些入口用于依赖就绪重试。

叶子的统一变化链：

```text
具体叶子 Refresh / MarkUnread / ClearUnread
→ RedDotLeafBase.SetCount
→ RedDotNodeBase.ApplyCount
  → 拒绝负数；同值返回
  → 写 Count → RefreshImage → Image.enabled
→ CountChanged(this)
→ RedDotManager.OnLeafCountChanged
→ RecalculateFrom(leaf.Parent)
  → branch.Recalculate → 子 Count 求和 → ApplyCount
  → branch = branch.Parent
  → 直到根节点
```

`AddLeaf` 校验叶子尚未挂接且 CanvasType 有效，调用 ApplyInitialCount，订阅 CountChanged，挂接父分支并重算父链。`RemoveLeaf` 退订事件、摘除叶子、清空 Parent、解除叶子 Image 绑定并隐藏图片，再重算原父链。分支的 Attach 与 Detach 同时维护 children 与 Parent。

证据：`Assets/Scripts/Pipeline/UI/RedDot/RedDotManager.cs:151、169、231`；`RedDotLeafBase.cs:19`；`RedDotNodeBase.cs:57`；`RedDotBranchNode.cs:20`。

## 3. Skills 调用链

### 3.1 建立叶子

```text
SkillTreeManager.Start / LateUpdate
→ TryBuildRedDotLeaves
→ UICacheManager.GetPrefab(CanvasType.Skills)
→ SkillsCanvasView 下的 SkillSlot
→ SkillSlot.BuildStateKey(slot, prefabRoot)
→ 读取 SkillSO、初始等级与解锁状态
→ new SkillRedDotLeaf
→ CanUpgradeSkill 计算初值
→ RedDotManager.AddLeaf(Skills, leaf, count)
→ 保存 redDotLeaves → RefreshRedDotLeaves
```

叶子保存 StateKey、SkillSO 与初始状态。`SkillRedDotLeaf.Refresh` 调 `CanUpgradeSkill`，已解锁、未满级且技能点大于零时 SetCount(1)。升级命令复用同一资格判断。读取预制体定义使叶子可以在画布首次实例化前建立。

### 3.2 升级、技能点与解锁

```text
角色升级 LevelUp
→ SkillTreeManager.UpdateAbilityPoints
→ StatsService.UpdateSkillPoints
→ RefreshAbilityPoints
→ 点数快照改变 → SkillsChanged
→ RefreshRedDotLeaves → 每个 SkillRedDotLeaf.Refresh
```

```text
SkillSlot 按钮点击
→ SkillSlot.TryUpgradeSkill
→ SkillTreeManager.TryUpgradeSkill
→ CanUpgradeSkill
→ SaveSkillState → SkillsChanged → 全部叶子重算
→ StatsService.UpdateSkillPoints(-1)
→ RefreshAbilityPoints → SkillsChanged → 全部叶子重算
```

满级后的解锁链为 `SkillSlot.OnMaxSkillLevel → SkillsCanvasView.HandleSkillMaxed → SkillSlot.Unlock → SaveState → SaveSkillState → SkillsChanged`。一次升级可以触发等级和点数两次重算，叶子同值时停止向上传播。

SkillsChanged 是 SkillTreeManager 自身的通知；技能点写入口保持现有接口，红点获知点数变化依赖 RefreshAbilityPoints。技能文本与图标仍按现有 VersionReader 刷新。

证据：`Assets/Scripts/Gameplay/Skills/SkillTreeManager.cs:93、142、155、173、189、197、215、226`；`SkillRedDotLeaf.cs:31`；`SkillSlot.cs:135、145、149`。

## 4. Quest 调用链

### 4.1 接取与移除

Quest 的参与集合采用 trackedQuests，Accepted、IsToComplete 加入，Decline、Completed 移出。

```text
QuestOptionsButton → QuestOptionsEventSO
→ QuestManager.OnQuestOptionChosen
→ QuestStateChanged → UpdateTrackedQuests
  ├─ 接取/待交付：AddQuestRedDotLeaf
  │  → RefreshObjectiveProgress → new QuestRedDotLeaf
  │  → IsQuestCompletable → RedDotManager.AddLeaf
  └─ 放弃/交付：RemoveQuestRedDotLeaf
     → RedDotManager.RemoveLeaf → 移除业务字典项
```

`QuestRedDotLeaf.Refresh → IsQuestCompletable → trackedQuests.Contains && IsQuestObjDone`。因此已接取且目标完成的 Accepted 任务也亮点。任务板上可见但尚未接取的任务不建立叶子。

### 4.2 目标变化

```text
ItemHistoryManager.RecordItem → ItemQuantityChanged
ConversationHistoryManager.RecordCharacter → CharacterTalkedChanged
→ QuestManager.OnItemQuantityChanged / OnCharacterTalkedChanged
→ RefreshTrackedQuestRedDots
  → 第一遍：对 trackedQuests 执行 RefreshObjectiveProgress
  → 第二遍：RefreshRedDotLeaf → QuestRedDotLeaf.Refresh
  → SetCount → 通用父链传播
```

记录入口每次调用均发送通知，叶子同值比较抑制重复计数传播。物品目标读取物品获得历史，角色目标读取对话历史，Completed 任务的目标进度保持冻结。历史通知重算进度与红点，Accepted 到 IsToComplete 的状态推进仍由任务板装载入口处理；奖励在首次进入 Completed 时发放。

范围内的打开链为 `QuestBoardManager → 加载任务列表事件 → OnRefreshQuestState → 同步叶子 → 打开事件 → UICacheManager.OpenCanvas`。范围外为 `QuestManager.OnToggleQuestCanvas → OpenQuestTracker → trackedQuests 显示列表 → 重算目标与 SyncRedDotLeaves → OpenQuestCanvas`。

证据：`Assets/Scripts/Gameplay/Quest/QuestManager.cs:214、258、270、280、293、307、325、336、431、498、525、559、576、680`；`QuestRedDotLeaf.cs:32`；`Assets/Scripts/Gameplay/Dialog/HistoryManager/ItemHistoryManager.cs:18`、`ConversationHistoryManager.cs:17`。

## 5. Stats 调用链

```text
StatsService.UpdateDamage / UpdateSpeed
→ PlayerStatsModel 修改数值 → StatsChanged
→ StatsRedDotTracker.OnStatsChanged
  → 当前 Damage/Speed 与上次事件基线比较
  → SyncBaseline
  → 任一实际增大：leaf.MarkUnread → SetCount(1)
```

Tracker 通过 TrySubscribe 保存实际订阅的只读模型，订阅前取当前值为基线。每次 StatsChanged 都把基线移到当前值；下降只移动基线，连续提升保持同一未查看标记。伤害与速度的跟踪范围与 StatsCanvasView 的展示字段一致。

查看与清除链：

```text
StatsCanvasView.OnOpen
→ new StatsCanvasController → 订阅 StatsChanged
→ controller.Refresh
→ view.SetStats(Damage, Speed)
→ StatsRedDotTracker.ConfirmDisplayed
→ leaf.ClearUnread → SetCount(0) → 父链刷新
→ SyncBaseline
```

面板打开期间的 StatsChanged 同样调用 Controller.Refresh。清除点位于写入属性文本之后，属于同步绘制流程。读档链为 `StatsService.LoadData → PlayerStatsModel.LoadFrom → StatsLoaded → Tracker.OnStatsLoaded → 同步基线并清除`；随后依次发送 HealthChanged、StatsChanged、ExpChanged。

证据：`Assets/Scripts/Gameplay/Player/StatsRedDotTracker.cs:77、93、122、135`；`Controllers/StatsCanvasController.cs:22、36`；`Views/StatsCanvasView.cs:71`；`Models/PlayerStatsModel.cs:75、120、126`。

## 6. Image 与预制体引用链

红点表现来自按钮、槽位中的嵌套预制体，随所属界面整体创建。节点绑定现有 Image 引用。

```text
ToggleMenuButton / IntegratedMenuButton / SkillButton / QuestSlot 预制体
→ 嵌套 RedDot.prefab
→ Unity UI Image
→ Assets/Sprites/UI/Icons/RedDot.png
```

红点图片不拦截射线，布局元素忽略布局。导入与显示参数直接以素材 meta、RedDot.prefab 为准。

| 表现位置 | 序列化引用与运行时绑定 |
|---|---|
| OpenMenu | PersistentScene 的 IntegratedUICanvasManager.toggleMenuRedDot → ToggleMenuButton 的 Image；Start → BindRootImage |
| 聚合面板入口 | integratedButtonRedDots → IntegratedMenuButton 的 Image；InitiatePage → BindPageRedDots → BindCanvasImage |
| 技能槽位 | UICacheManager 注册 SkillsCanvas → SkillButton → SkillSlot.redDotImage；SkillsCanvasView.OnOpen → GetRedDotLeaf(StateKey) → BindImage |
| 任务槽位 | UICacheManager 注册 QuestCanvas → QuestSlot → QuestLogSlot.redDotImage；QuestCanvasView.RefreshIfChanged → BindSlotRedDot → GetRedDotLeaf(quest) → BindImage |
| Stats | StatsRedDotTracker 持有叶子，聚合面板 Stats 入口显示分支红点 |

每个节点仅保存一个 Image。BindImage 替换当前引用并刷新；相同引用直接返回。UnbindImage 只在参数与当前引用一致时清空引用。图片的隐藏由 Manager 的解绑/移除入口或所属界面的显隐生命周期处理。

聚合分页先 `UnbindPageRedDots → UnbindCanvasImage`，再绑定本页。canvasImages 保存分支尚未建立时收到的 Image，EnsureCanvasBranch 建立后补绑。Skills 关闭逐槽解绑；Quest 复用旧槽、空槽与关闭时解除对应绑定。

证据：`Assets/Scripts/Pipeline/UI/SystemCanvasManagers/IntegratedUICanvasManager.cs:59、186、206`；`Assets/Scripts/Gameplay/Skills/SkillsCanvasView.cs:43、106`；`Assets/Scripts/Gameplay/Quest/QuestCanvasView.cs:88、142、151、165、204`；预制体引用位于 `Assets/Prefabs/UI/Buttons/`、`Slots/QuestSlot.prefab` 与 `RedDot.prefab`。

## 7. 待办导航与画布调用链

输入引用链为 `PersistentScene.RedDotManager.navigateAction → UI_RedDotNavigate.asset → GameInput.inputactions 的 UI.RedDotNavigate → <Keyboard>/backquote`。RedDotManager 自行启停并采样动作。

```text
RedDotManager.Update → WasPressedThisFrame
→ TryNavigateToPendingCanvas
→ 从 lastNavigatedCanvas 后循环 navigationOrder
→ HasPending：分支 Count > 0
→ IsCanvasAvailable：调用所属域的 gate
→ 保存 pendingCanvas 与 pendingFrame
→ UIManager.RequestCanvasToggle
→ inputState[canvas] = true
→ UIManager.Update / ToggleCanvas
→ 全局阻塞检查 → ESC 检查 → FindRequestedCanvas
→ CanvasFocusStack.ApplyFocusChange
  ├─ 未打开：RaiseCanvasEvent → ToggleCanvasEventSO
  └─ 已打开：调整焦点与排序
```

导航顺序由 IntegratedUICanvasManager 的 canvasToToggle 经 SetNavigationOrder 传入，过滤重复画布与 Default。UIManager 合并按键和外部请求，有绑定的画布按 inputBindings 顺序选择，再按枚举顺序处理无绑定画布。红点请求作为普通外部请求参与仲裁。

Stats 与 Skills 沿缓存开关事件进入 `UIViewInstanceCache.Entry.HandleToggle → SetOpen → 按需实例化 → CachedUIViewBase.Open → OnOpen → 显隐与状态上报`。QuestCanvasView 的 OpenOnToggleEvent 为 false，打开交给 Quest 业务准备列表，关闭仍由缓存处理。

下一帧 RedDotManager.LateUpdate 调 UICacheManager.IsCanvasOpen 确认 pendingCanvas，打开则更新 lastNavigatedCanvas，随后清空 pendingCanvas。确认依据是缓存打开状态。Stats gate 检查数值服务就绪，Quest gate 检查任务板在范围内或已接取列表非空；Skills 使用已建立分支的默认可用性。

证据：`Assets/Scripts/Pipeline/UI/RedDot/RedDotManager.cs:74、134、193、217、268`；`Assets/Scripts/Pipeline/UI/SystemCanvasManagers/UIManager.cs:126、252、334`；`CanvasFocusStack.cs:109`；`Assets/Scripts/Pipeline/UI/Runtime/UIViewInstanceCache.cs:46、218`。

## 8. 生命周期与行为边界

| 对象或入口 | 当前行为 |
|---|---|
| SkillsCanvasView 关闭 | 解绑技能 Image、退订满级事件，技能叶子保留 |
| QuestCanvasView 关闭 | 解绑任务 Image、清空显示上下文，trackedQuests、进度与叶子保留 |
| StatsCanvasView 关闭 | Dispose Controller，退订其 StatsChanged，Tracker 继续跟踪 |
| SkillTreeManager 停用 | 退订 SkillsChanged 与 LevelUp，叶子保留 |
| QuestManager 停用 | 退订任务与历史通知，叶子保留 |
| StatsRedDotTracker 停用/销毁 | 停用退订实际模型；销毁时 RemoveLeaf |
| 缓存淘汰 | 销毁界面实例，常驻树与业务索引继续保存计数 |
| 读档 | StatsLoaded 建立属性比较基线并清除属性提醒；技能和任务沿各自业务入口同步 |

技能点红点同步经过 SkillTreeManager 的 RefreshAbilityPoints 与 SkillsChanged。属性提醒跟踪 Damage、Speed，技能升级本身处理等级与点数，属性提升提醒由实际数值写入触发。Quest 查看追踪视图保持可交付提示，放弃或交付移除对应叶子。
