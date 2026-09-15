# My_ARPG 重构优化清单 · 已解决归档

> 2026-09-13 从《My_ARPG_重构优化清单》拆分。本文档只保留**已完成/已关闭项**，作为实现与验证记录存档；
> 未完成与待评估项（asmdef、xLua、数据驱动/状态机、多场景加载、GameJam 按需迁移、事件总线重评估、事件可视化工具可行性）见《My_ARPG_重构优化清单_未解决.md》。
> 章节标题沿用原文档编号，便于与历史执行记录对照。

---

## 一之三、菜单场景画布屏蔽（2026-09-08 第三轮，最终为独立组件方案）

**需求**：StartingMenu（Menu 类场景）中屏蔽游戏期 HUD 画布（血条/经验），物品栏保持可见。

**演进记录**：第一版把 ShowInStartingMenu 塞进 ICanvasManager（开关面板族 TryShowInStartingMenu + HUD 族 ApplyMenuVisibility + 6 实现类序列化字段 + 新建 InventoryCanvasManager），用户否决——**功能不应进接口**。用户手工回退了全部代码后定稿为独立组件方案。

**最终实现**：`Gameplay/UI/MenuSceneCanvasHider.cs`——可挂接组件：序列化引用目标 **CanvasGroup** 组件 + `SceneLoadedVoidEventSO`；OnEnable 订阅场景加载完成事件，Menu 类场景屏蔽目标（alpha=0 + interactable=false + blocksRaycasts=false），非 Menu 场景恢复。与 ICanvasManager 完全解耦，不进焦点栈。

**挂接**：HealthCanvas、ExpCanvas 已挂接并接线（屏蔽对象）；InventoryCanvas 不挂（默认可见=物品栏在菜单显示）。场景已保存。

**验证**：编译零 error；28 测试全绿；Play（InitialScene→StartingMenu）：InventoryCanvas α=1/interactable=True（显示），HealthCanvas 与 ExpCanvas α=0 + interactable=False + blocksRaycasts=False（屏蔽）。

### SceneChanger 加载时序加固（2026-09-08 追加）

**审查结论**：OnLoadCompleted 内 currentScene 赋值→sceneLoadedEvent 广播为同步顺序执行，本身安全；但存在三个隐患——①正确性裸奔在行序上（无注释守护）；②加载窗口期（请求→完成，含 fade 等待）GetCurrentGameScene() 返回旧场景；③双请求竞态：第二请求覆写 sceneToLoad 字段导致完成时记录错场景 + 双卸载协程。

**加固**：`isLoading` 守卫（加载窗口内新请求拒绝+LogWarning，完成后解锁）；OnLoadCompleted 改用闭包捕获的本次目标场景（不再回读字段）；赋值行加不变量注释。②的彻底治法（事件携带场景载荷）记录未做。

**Play 验证**：连发 Scene1+Scene2 双请求→仅 Scene1 加载、currentScene=Scene1（Scene2 被拒）；再发 Scene2→currentScene=Scene2（闭包记录正确）；完成后第三次请求 MenuScene 被受理（锁正常释放不死锁）。
**环境备忘**：用户编辑器 Console 的 Warning 开关处于关闭状态——Unity 日志 API 遵循该过滤，MCP read_console 读不到任何 Warning（Debug.Log 正常）。调试时注意。

---

## 一之二、GetComponent 族治理（2026-09-08 第二轮）

用户政策：尽量杜绝 GetComponent 族，改用序列化引用（动机：性能 + 摆脱层级结构依赖）。全项目清点 58 处真实调用 / 34 文件，**每帧路径 0 处**，事件驱动未缓存 25 处为治理重点。

**四条政策（经逐项确认）**：①事件驱动未缓存的全部消灭（序列化引用）；②自身组件的一次性 Awake 缓存（rb/animator 等 ~12 处）保留；③碰撞对方/每场景物体 5 处保留 + TryGetComponent/判空加固，文档化例外；④SaveCanvasPanelManager 按钮组重构。

**改动明细**：
- 每次掉血 `healthText.GetComponent<Animator>` → 序列化 `healthTextAnimator`（HealthCanvasManager）
- 每次属性变动 `statsSlots[i].GetComponentInChildren<TMP_Text>` → 序列化 `TMP_Text[] statTexts`；Canvas 一并序列化（StatsCanvasManager）
- Integrated：OnFocus 的 GetComponent\<Canvas\> 与每次 OnEnable 重取按钮/文本 → 全序列化（Canvas/Button[]/TMP_Text[]/toggleMenuText），初始化移到一次性——**顺带修复文本列表只 Add 不 Clear、随重激活无限堆积的 bug**；删除 integratedButtonsParent 字段
- DialogManager：选项按钮文本 → 序列化 `TMP_Text[] optionTexts`（与 optionButtons 并行）
- QuestManager:335 → QuestLogSlot.SlotCanvas 公开访问器（本来就缓存了）
- Loot：DataDefinition Awake 缓存（Initialize/存档不再查）
- SceneChanger：`player.GetComponent<Transform>` → `player.transform`；ForbidInput/AllowInput 的 GetComponentInChildren\<PlayerMovement\> ×2 → 新增 `PlayerMovement.Main` 静态定位器（Awake 赋值/OnDestroy 清空；ForbidInput 禁用不清空，AllowInput 仍可用）
- EnemyCombat：每次攻击 GetComponentInChildren\<PlayerHealth/PlayerMovement\> → `PlayerHealth.Instance` + `PlayerMovement.Main`
- InventoryManager：`FindObjectOfType<SceneChanger>` → `SceneChanger.Instance`；`GameObject lootPrefab` → `Loot lootPrefab`（类型化组件引用，Instantiate 兜底直接返组件）
- PlayerBow：`GameObject arrowPrefab` → `Arrow arrowPrefab`（同上）
- ShopSlot ×13 / ShopToggles：`GetComponentInParent<IShopInteractable>` → 序列化 `Component shopRef` + Awake 一次性 as 转型（接口不能序列化，Component 承载；显式引用、无层级搜索）
- PlayerAnimationEventRelay：删 GetComponentInChildren 兜底（场景已接线，验证过），空引用改 LogError
- B：Stats/Shop/Quest/Backpack/Skills/Integrated 六管理器 Canvas 统一为序列化字段（对齐 SaveCanvasPanelManager 写法）
- C：SaveCanvasPanelManager 按钮组 `GetChild+GetComponent` 发现链 → `[System.Serializable] SaveLoadButtonGroup` 序列化列表（26 组已烘焙接线），监听注册移 Start 一次性；删除 LoadButtons
- D（例外+加固）：PlayerCombat/Arrow 的 IDamageable → TryGetComponent；ElevationEntry/Exit 补 TryGetComponent 判空（原无判空有 NRE 风险）；ConfinerFinder 原本已判空保留。**例外理由：碰撞对方/每场景物体运行时才知道是谁，无法预引用**（曾评估 DamageableRegistry 注册表与 IntEventSO 事件化方案，用户选择保留）

**场景接线**：一次性 MCP 迁移脚本烘焙（HealthCM 动画/StatsCM 文本+Canvas/四管理器 Canvas/Integrated 数组/Dialog 文本/存档按钮组 26 组/Shop 引用 ×14/类型化 prefab×2），场景已保存。旧字段留下的孤儿 YAML 无害。

**验证**：编译零 error 零 warning；28 测试全绿；Play 冒烟（InitialScene 启动）：PlayerMovement.Main OK、statTexts 接线 OK、`UpdateDamage(2)` 文本 "Damage:6"→"Damage:8"、HP "2/2"→"1/2"、ShopSlot 转型 13/13、healthTextAnimator OK。
**环境备忘**：编辑器失焦且 runInBackground 关闭时 Play 循环不走帧（frameCount 停滞，Start/Update 不执行）——自动化验证需 `EditorApplication.Step()` 泵帧，或聚焦编辑器。

---

## 一、2026-09-08 执行记录（本轮）

本轮经逐项确认后执行，全部改动在 Unity Editor 开启 + MCP 连接状态下完成，每阶段 `refresh + read_console` 验证零 error，EditMode 测试 28/28 全绿，关键链路（互斥/输入阻塞/GameOver）经 Play 模式冒烟验证。**改动尚未 git commit（当时状态，后续已随仓库提交推进）。**

| 项 | 结果 |
|---|---|
| 4.1 目录迁移 | ✅ 完成。`Assets/Scripts/` 根只剩 `Contracts/` + `Gameplay/`；CanvasManagers 并入 `Gameplay/UI/CanvasManagers/`。经 AssetDatabase/文件系统搬移，GUID 全保留 |
| 4.2 Shop 解耦 | ✅ 全套完成（详见下） |
| 4.3 StatsManager SO 化 | ✅ 三步全做（详见下） |
| 4.4 单元测试 | ✅ test-framework 1.4.6 + `Assets/Tests/Editor/` 28 个 EditMode 测试全绿 |
| 4.5 asmdef | ⏸ 用户决策：缓到下轮 → **明细移至未解决清单** |
| 4.6 FrameWork UI 回填 | ✅ 三步全做 + Play 验证（详见下） |
| 4.7 事件卫生清理 | ✅ 完成（含 FinshCombat，前提已满足） |
| 4.8 事件可视化工具 | ❌ 当时决策不做 → **2026-09-13 重启可行性探讨，移至未解决清单** |
| 4.9 xLua | ⏸ 用户决策：下轮 → **方案附录移至未解决清单** |
| 4.10 对象池 | ✅ 完成（迁 GameJam ObjectPool + 两消费点）。数据驱动/状态机两项留存 → 未解决清单 |
| 4.11 多场景加载 | ⏸ 维持缓办 → 未解决清单 |
| 4.12 FinshCombat 别名 | ✅ 已删。**发现清单前提已过时**：Slash.anim 动画事件早已使用新拼写 FinishCombat，别名实际零引用 |
| 4.13 Materials | ✅ `git rm -r --cached`（704 文件出库，磁盘保留）+ `.gitignore` 加 `/Materials/` |
| 4.14 TimeManager 计数版 | ✅ 完成（消除现存不对称 bug）。其余 GameJam 成果按需迁移 → 未解决清单 |

### 4.2 落地明细

- 新建 `Contracts/IShopInteractable.cs`（IsShopOpen/CurrentPortraitTarget/TryBuyItem/SellItem/Open{Item,Weapon,Armor}Shop），`ShopManager` 实现。
- 新建 `Gameplay/SO/Events/ShopKeeperEventSO.cs` + 资产 `Assets/GameSO/Events/ShopKeeperEventSO.asset`，已接线到 ShopManager（场景）、ShopPortraitCameraController（场景）、ShopKeeper（prefab，所有实例继承）。
- "keeper 离开自动关店/反注册"逻辑收进 ShopManager 自身（`OnKeeperExited`，带 `activeShopKeeper != keeper` 防护）。
- `ShopSlot`：删除从未使用的 `[SerializeField] ShopManager` 字段，改 `GetComponentInParent<IShopInteractable>()`。
- `SubShopToggler.cs` → 改名 `ShopToggles.cs` 对齐类名（GUID 保留，场景引用不断），三个分页按钮走接口。
- `ShopPortraitCameraController`：轮询单例改为订阅 ShopKeeperEventSO 事件驱动，LateUpdate 只做跟随。
- `InventorySlot`：按原决策暂留单例（它还耦合 InventoryManager，属另一条链）。
- 顺手清理：ShopKeeper.prefab 里两个孤儿字段（`shopLoadEvent` 指向已删除的死资产、早已不在代码里的 `toggleShopCanvasEvent`）。

### 4.3 落地明细

- 新建 `Gameplay/SO/PlayerStatsSO.cs` + 资产 `Assets/GameSO/PlayerStatsSO.asset`。**场景里 StatsManager 组件上的原调参（damage=2/speed=5/maxHealth=2 等 14 项）已抓取并写入资产，无丢失**。
- 事件：`HealthChanged`（血量/上限）、`StatsChanged`（速度/伤害）；`LoadStats` 广播两者。
- `HealthCanvasManager`/`StatsCanvasManager`：OnEnable 订阅被动刷新（Health 顺带修复了"首次显示前不刷新"），OnEnable 即时刷一次。
- `StatsManager` 瘦身为转发层，对外 API 完全不变（UseItem/PlayerCombat/EnemyHealth/ExpManager/DataManager 等调用点零改动）。
- 场景接线：三个组件的 `statsSO` 字段已接资产；StatsManager 组件上旧的 `stats:` 内联数据块已从场景 YAML 清除。
- **运行时副本加固（2026-09-08 追加，方案 B）**：StatsManager 序列化字段改为 `statsConfig`（初始值模板），运行时 `Instantiate` 出 `RuntimeStats` 副本承载全部读写与事件——**资产本体永不被运行时写入**，消除编辑器下"Play 中的数值改动污染资产落盘"的经典 SO 陷阱。UI 两面板改为 Start 时绑 `StatsManager.RuntimeStats`（面板可能随 GamePlay 根节点延迟激活，届时单例必已就绪），订阅在 Start/OnDestroy 生命周期，重激活走 OnEnable 补刷新（**同时修复了 StatsCanvasManager 失活期间错过事件不补刷的缺口**）。Play 验证：`UpdateDamage(3)` 后 runtime=5、资产仍=2、退出 Play 资产仍=2；失活期间扣血→重激活文本自动补齐。

### 4.6 落地明细（三步全做）

1. **互斥**：UIManager 增 `mutexCanvases` 列表 + `CloseOtherMutexCanvases` + `IsMutexCanvas`/`IsClosableCanvas`；`ReportCanvasState` 扩为 4 参（closeOnEscape/blocksGlobalInput 带默认值）。CanvasFocusStack 增 `GetOpenCanvases()`（副本快照）与 `HandleESCOrOpen()`。
   - 场景配置（PersistentScene 的 UIManager 组件）：互斥列表位于 UIManager 组件 Inspector「互斥面板 (Mutex Canvases)」，规则=表内面板任一打开自动关表内其它、表外面板永远共存。曾配置 Stats/Skills/Dialog/Quest/Shop/Backpack 六项，**2026-09-08 用户决策改为清空**（恢复重构前全面板自由共存行为；互斥代码保留，Inspector 里随时可再往表里加）。
   - **堵 2 处绕过点**：`PlayerHealth` 死亡改走 `UIManager.RequestCanvasToggle(GameOver)`；`OpenSaveLoadCanvasButton` 改走 `RequestCanvasToggle(SaveLoad)`。两脚本上的事件序列化字段已删。
   - 为使上述统一入口生效：UIManager 的 `toggleCanvasEvents` 补挂 **ToggleGameOverEventSO** 资产（此前缺失，GameOver 不在可关闭体系内）；`inputBindings` 补 **GameOver、SaveLoad 两条 action=null 的绑定**（无按键、仅接受代码请求）。
   - GameOver/Integrated 补状态回报：GameOver 声明 `closeOnEscape:false, blocksGlobalInput:true`；Integrated 的私有 SetCanvaState 恢复 Report 调用（不在 toggleCanvasEvents 列表 → 仍不可被 UIManager 主动关闭，自管理语义保留）。
2. **输入阻塞**：`ICanvasManager` 增 `CloseOnEscape`/`BlocksGlobalInput` 默认实现，`SetCanvaState` 上报时携带；UIManager 增 `IsGlobalInputBlocked()`，`ToggleCanvas()` 入口吞掉全部切换输入（含 ESC），`RequestCanvasClose` 刻意不受阻塞；ESC 分支消费 closeOnEscape 声明。
3. **画布自管理**：`ICanvasManager` 增 `SceneLoadedEvent` 属性要求 + `SetCanvaInactive` 默认实现；10 个画布管理器（Backpack/SaveLoad/SkillTree/Stats/Shop/Quest/Integrated/ESCMenu/GameOver/Dialog）接 `SceneLoadedVoidEventSO` 自复位（含各自内部状态复位：SaveLoad 的 isPanelOpen、Integrated 的页码、Dialog 的 ForeceEndDialog、ESC 的 OnESC(false)）。
4. **测试**：`Assets/Tests/Editor/`（无 asmdef，落 Assembly-CSharp-Editor）：CanvasFocusStackTests 15 个（开闭/order/ESC/快照契约——注意**关面板是"事件→回报"两段式**，出栈以 ReportState(false) 为准）+ PlayerStatsSOTests 7 个 + ObjectPoolTests 5 个。
- **Play 冒烟验证**（InitialScene 正常启动，当时互斥表为 6 面板配置）：Stats 经统一入口开启 ✓；Backpack 开启互斥关 Stats ✓（互斥表现已清空，此行为不再触发）；GameOver 经统一入口开启并暂停 ✓；GameOver 打开时 Backpack/ESC 请求被吞 ✓；`RequestCanvasClose(GameOver)` 不受阻塞 ✓。

### 4.10 对象池落地明细

- `Gameplay/Systems/Pooling/ObjectPool.cs`：GameJam 版全量迁移（IPoolable + 泛型池），**新增 Get 时跳过已被场景卸载销毁的池内对象**的防护。
- `PlayerBow`：箭矢池（prewarm 8 / max 30，父级 PlayerBow），`Shoot` 取件发射，池满走 Instantiate 兜底；`Arrow` 实现 IPoolable——`OnPoolGet` 按当前攻击力刷新伤害并重置贴图/旋转/父级，寿命到期或由池回收；埋箭（AttachToTarget）语义保留，归还时恢复物理状态。
- `InventoryManager`：掉落物池（prewarm 4 / max 20）。**Loot 的 ISaveable 注册生命周期随池走**：`OnPoolGet` 注册、`OnPoolReturn` 注销（原 Awake 注册已移除，避免预热对象混入存档）；拾取后的 DisableAfterDelay 改为归还池。掉落物仍 MoveGameObjectToScene 随场景卸载，池 Get 自动跳过已销毁对象并兜底。

### 计划外修复（本轮发现并顺手处理）

1. **YSingleton 重复实例竞态**：`Awake` 重复路径现在 `enabled = false` 后再 `Destroy(gameObject)`——Destroy 有延迟，此前销毁前那一帧 Update 会在未初始化实例上跑（Play 中曾触发 `KeyNotFoundException: 'ESC'`）。另注意：**从 PersistentScene 直接进 Play 是非正常启动路径**（正确入口 InitialScene），会放大该竞态。
2. **SaveCanvasPanelManager 启动竞态**：`LoadInfoToSaveList` 增 `SaveSystem.Instance == null` 守卫（OnEnable 可能早于 SaveSystem.Awake）。
3. **死资产**：`GameSO/Events/ToggleCanvasEvents/ToggleGameOverEvent.asset` 零引用（与 ToggleCanvas/ToggleGameOverEventSO.asset 重名重复），已删。
4. TimeManager 原状记录（已修）：旧版 Pause 不计数、Resume 却递减——多系统同时暂停时第一次 Resume 就会全部解除。

### 已知行为备忘（非 bug，记录备查）

- `QuestManager.OnToggleQuest` 刻意忽略 true 态事件（开启只认 openQuestEventSO，来自 QuestBoard 流程）；互斥关闭走 toggle(false) 正常。
- 关闭 GameOver 不恢复 timeScale（原设计：重试按钮走 `ForceResumeGame`）。
- 菜单场景下 `GamePlay` UI 根节点未激活，其下画布管理器单例/订阅在进入游戏场景前不可用（设计使然）。

---

## 二、GameJam2607 可迁移成果调研（已完成部分）

### 2.0 总览：FrameWork 框架层

GameJam2607 存在独立框架目录 `D:\Unity\Projects\GameJam2607\Assets\FrameWork\`，含 Core（枚举/单例）、SO（6 个事件/数据 SO）、UI（UIManager/ICanvasManager/CanvasFocusStack 等）、Scene（SceneChanger/PersistentSceneRegistry/TimeManager 等）四个子目录，另有 Samples/ 示例。整体设计与 My_ARPG 同源分叉（YSingleton、CanvasFocusStack、ScrollbarFix 等两边都有），但 FrameWork 版在这些共享文件上**多出三层能力：互斥分组、输入阻塞、画布自管理**。

不回流项（已决策关闭）：My_ARPG 的 `Joystick.cs`（78 行，含触屏显隐控制与编辑器分支）严格强于 GameJam 版（49 行）；`ScrollbarFix.cs` 两边逐行相同；`UIDrag.cs` My_ARPG 版启用了 HandleFocus 调用而 GameJam 版被注释。三个控件均以 My_ARPG 版本为准。

### 2.1 回填第 1 步：UI 互斥分组 ✅（2026-09-08 已完成，见第一节）

**GameJam 实现**（`FrameWork/Scripts/UI/UIManager.cs`，293 行）：
- `:26` `[SerializeField] List<CanvasToToggle> mutexCanvases` —— 互斥组在 Inspector 里配置，列在表中即参与互斥，未列出可与任意面板共存（`:144-147` `IsMutexCanvas`）。
- `:97-118` `ReportCanvasState(canvas, state, closeOnEscape = true, blocksGlobalInput = false)` —— 画布上报状态时同时声明两个行为参数，存入 `canvasCloseOnEscape`/`canvasBlocksInput` 字典（`:108-109`）。
- `:131-142` `CloseOtherMutexCanvases(opening)` —— 某互斥画布打开时，遍历焦点栈已打开画布，对其中互斥且可关闭的逐一 `RequestClose`。

**My_ARPG 原状**（回填前）：
- 无互斥逻辑。`ToggleCanvasEventSO.cs:12` 注释自认 TODO。
- `UIManager.cs:97-100` 的 `ReportCanvasState` 只有 2 参。
- 2 处绕过 UIManager 直接 Raise 事件：`PlayerHealth.cs:29`（死亡开 GameOver）、`OpenSaveLoadCanvasButton.cs:17`。
- 2 个画布不回报状态：`IntegratedUICanvasManager.cs:159`；`GameOverCanvasManager` 全文件无 `ReportCanvasState`。

### 2.2 回填第 2 步：输入阻塞（blocksGlobalInput）✅（2026-09-08 已完成）

**GameJam 实现**：
- `ICanvasManager.cs:16` 接口默认实现 `bool BlocksGlobalInput => false;`。
- `UIManager.cs:164-175` `IsGlobalInputBlocked()`；`:189-193` `ToggleCanvas()` 入口若被阻塞则 `ResetInputState()` 并 return；`:91` `RequestCanvasClose` 刻意不受阻塞。
- `:195-209` ESC 处理消费 `closeOnEscape` 声明。

**My_ARPG 原状**（回填前）：无统一输入阻塞位。输入限制只有 `SceneChanger.ForbidInput/AllowInput`（锁移动）与 ESCMenu/GameOver 各自 `PauseGame()`（停时间）。

### 2.3 回填第 3 步：画布自管理 inactive ✅（2026-09-08 已完成，10 个管理器接入）

**GameJam 实现**：
- `ICanvasManager.cs:9` 接口要求 `VoidEventSO SceneLoadedEvent { get; }`；`:29-34` 默认方法 `SetCanvaInactive`；`:36-50` `SetCanvaState` 连同声明上报。
- 效果：切场景时画布复位由各画布自己响应加载完成事件完成。

**My_ARPG 原状**（回填前）：画布复位依赖 UIManager 订阅 `LoadRequestEvent` 在切换**请求**时 `ResetCanvas()`；`sceneLoadedEvent` 完成广播只有 DataManager 一个订阅者。现在两层并存：请求时 UIManager 统一关、完成后各画布自复位。

> 原 2.4（多场景加载缓办依据）与 2.5（按需迁移候选清单）随对应未完成项移至未解决清单第 4、5 节。

---

## 三、事件系统改进（已完成部分）

### 3.1 现状盘点（2026-08-17 实测；4.7 清理后死代码项已消灭）

`Assets/Scripts/Gameplay/SO/Events/`（迁移后路径）下事件类，按 event 字段计通道，全部是 C# `event Action` 委托（无 UnityEvent）。曾经的问题及现状：

1. **ToggleCanvasEventSO 扇出最大**：toggle 通道 11 个订阅方 + focus 通道 7 个订阅方；2 处绕过统一出口的直接发布已于 2026-09-08 堵闭（PlayerHealth/OpenSaveLoadCanvasButton 改走 `UIManager.RequestCanvasToggle`）。
2. **VoidEventSO 一类五义**：同一类被 5 种语义的资产实例复用。2026-09-08 已在 `VoidEventSO.cs` 顶部立命名规范注释（资产名必须含语义前缀），现有 7 个资产名均符合，未改名。
3. **死代码**：~~OpenSaveLoadPanelEventSO、ShopLoadEventSO 两个整类零发布零订阅；InventorySlotsStatsSO.InventoryRespondEvent 零调用~~ → 2026-09-08 已删（含 .asset 与场景孤儿引用清理）。另删零引用的重复资产 ToggleCanvas/ToggleCanvasEvents 目录下的 ToggleGameOverEvent.asset。

> 原 3.2（事件总线，维持不采用待重评估）与 3.3（可视化工具，重启探讨）移至未解决清单第 6、7 节。

---

## 四、完成项收尾记录

### 4.1 目录迁移收尾 ✅（2026-09-08 完成）

`Assets/Scripts/` 根现只有 `Contracts/` 与 `Gameplay/`。Gameplay 下：A Star/Dialog/Grid/Inventory/Player/Quest/Save/Scene/Shop/Skills/SO/UI/Units + InitialLoad.cs；CanvasManagers 位于 `Gameplay/UI/CanvasManagers/`。搬移经 AssetDatabase/文件系统（目录+同级 .meta 同步），GUID 与场景引用全保留，编译零错误。

### 4.2 P0-A：Shop 面板解耦 ✅（2026-09-08 全套完成）

原方案（先做 ShopSlot 一条链路验证）升级为一次做完：IShopInteractable + ShopKeeperEventSO + 6 文件改造，InventorySlot 暂留单例。落地明细见第一节。调用点盘点更新：清单原记录 4 个文件，实测 6 个（多了清单外的 SubShopToggler.cs 6 处）。

### 4.3 P0-B：StatsManager 治理 ✅（2026-09-08 三步全做）

PlayerStatsSO 持数据+广播 → UI 订阅被动刷新 → StatsManager 瘦身为转发层（对外 API 不变）。场景原调参已迁入资产。落地明细见第一节。

### 4.4 单元测试 ✅（2026-09-08 建立）

`com.unity.test-framework` 1.4.6 已加入 manifest；`Assets/Tests/Editor/`（无 asmdef）含 CanvasFocusStackTests(15)/PlayerStatsSOTests(7)/ObjectPoolTests(5)，EditMode 28/28 全绿。后续新逻辑（尤其 UIManager 互斥/阻塞分支）建议随手补测。

### 4.6 FrameWork UI 层回填 ✅（2026-09-08 三步全做并 Play 验证）

互斥 → 输入阻塞 → 画布自管理全部落地；互斥配置、补挂事件、新增绑定、10 管理器接线明细见第一节。

### 4.7 事件卫生清理 ✅（2026-09-08 完成）

删 2 个死类+2 死资产+1 死通道+FinshCombat 别名（前提已满足：Slash.anim 已用新拼写）+1 个零引用重复资产（ToggleGameOverEvent.asset）；VoidEventSO 命名规范已立于类头注释。

### 4.12 FinshCombat 别名删除 ✅（2026-09-08 完成）

清单原记载"需先在 Animation 窗口改 Slash.anim 事件名"——实测 **Slash.anim 早已使用新拼写 FinishCombat**（别名全项目零引用），直接删除 PlayerCombat.cs 的转发壳即可，已删。

### 4.13 Materials/ 根目录处理 ✅（2026-09-08 完成：从 git 移除保留本地）

`git rm -r --cached Materials`（704 文件出库，磁盘全保留），`.gitignore` 追加 `/Materials/`。注意：仓库远端历史中仍留有这 26MB 的历史版本，如需彻底瘦身需改写历史（另立项）。

---

## 五、疑点（已解决）

~~Player.prefab 的场景引用来源~~ → **已解决（2026-09-08 核实关闭）**：`Assets/Prefabs/Player.prefab`（GUID 327117703f214384bb529a0e5f4bd728）已于 commit ca8421c（2026-08-30）删除；该 GUID 在当前及历史全部 .unity/.prefab 中零引用（`git log --all -S` 为空）。场景中 Player 实例应为场景内对象，"改 prefab 不生效"风险随 prefab 删除一并消失。
