# Architecture Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-01 缓存画布生命周期统一

缓存服务改名为 `UIViewInstanceCache`，视图基类改为 `CachedUIViewBase` 与 `CachedCanvasViewBase`。缓存只维护实例和事件；视图生命周期通过 `ICanvasManager` 更新显隐状态、焦点与排序。Contracts 脚本按基类、接口、状态与枚举分目录，移动保留脚本 GUID。Unity 编译通过，三条 UI 缓存回归通过；全量 EditMode 有三条既有 `InventorySlot.OnValidate` 空引用失败。维护计数更新为 `0/5 -> 1/5`。

## 2026-11-01 Gameplay 视图通过 State 查询刷新

五个管理器视图改为从稳定的 `CurrentState` 按需读取，`DirtyValue<T>` 只保留管理器内部值与脏标记跟踪，不再向视图提供快照包装或集合副本。Architecture 维护计数达到 `5/5`，复核 Layering 路由与 Guide 并同步实现说明后清零为 `0/5`。Unity Editor 刷新后控制台返回 0 条错误。

## 2026-11-01 单例与业务刷新契约收紧

`YSingleton<T>` 新增必需依赖访问器；Contracts 的 DirtyValue 以版本快照向视图提供只读数据，Gameplay 各面板按绘制字段确认刷新。更新 Composition 与 Layering 权威说明。维护计数更新为 `3/5 -> 4/5`。

## 2026-10-31 属性面板预制体与闲置缓存

新增 Pipeline.UI.Runtime 的 CachedUIView / UIViewCache；UIManager 组合预制体注册与闲置 LRU，复用既有输入和焦点栈。更新组合契约与分层统计。

Unity 重编译及预制体引用检查完成；未运行测试或进入 Play 模式，复用与多界面淘汰仍待运行时验证。维护计数更新为 `2/5`。

## 2026-10-05 建立 Architecture 域中文 Router 与两个子域权威文档

- 任务：为 Architecture 域重写中文模板 Router，并建立 `Composition` 与 `AssemblyPlan` 两个子域的权威文档与子 Router；`Composition` 状态 Active，`AssemblyPlan` 状态 Proposal。
- 写入的文件：
  - `Router.md`，按中文模板重写，原有的 3 行下级导航原样保留。
  - `Composition\Composition_Guide.md`，文档 ID `ARCH-COMPOSITION-GUIDE`，状态 Active。
  - `Composition\Router.md` 与 `Composition\DeveloperLog.md`。
  - `AssemblyPlan\AssemblyPlan_Proposal.md`，文档 ID `ARCH-ASSEMBLY-PLAN`，状态 `Proposal`，未实施。
  - `AssemblyPlan\Router.md` 与 `AssemblyPlan\DeveloperLog.md`。
  - `DeveloperLog.md`，本条目。
- 依据的证据：
  - 组合与时序：契约类型 `YSingleton`、`SaveableService`、`SaveRegistry`、`ISaveable`、`ICanvasManager`、`MyEnums`；管线类型 `TimeManager`、`InitialLoad`、`SceneChanger`、`SceneDataForSave`、`CameraPixelSnap`、`PersistentSceneRegistry`、`UIManager`、`CanvasFocusStack`、`IntegratedUICanvasManager`；玩法类型 `StatsService`、`SkillTreeManager`、`SaveSystem`、`SaveDataManager`、`SaveLoadCanvasManager`；测试类型 `CanvasFocusStackTests`。
  - 程序集与热更：事件通道 SO 类型族；`SkillManager` 的 `skillName` 分派、`UseItem` 的 `ApplyItemEffects` 分派、`PlayerMovement` 的 `playerState` 分派。
- 本轮核对的静态事实：具体单例 19 个加抽象基类 1 个；`DefaultExecutionOrder` 全工程 1 处，工程设置中没有脚本执行顺序配置；`RuntimeInitializeOnLoadMethod` 2 处；`ICanvasManager` 实现者 10 个；`OnSingletonInitialized` 重写点 6 个；全工程 asmdef 数为 0；无 xLua 与 Lua 资产，`LuaManager` 类型未定义；事件类 11 个、通道 12 条、事件资产 28 个。
- 维护计数：`0/5 -> 1/5`
