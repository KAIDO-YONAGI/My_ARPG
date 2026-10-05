# Architecture.Composition Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 组合与初始化时序 权威文档

- 任务：为 Architecture.Composition 建立权威 Guide，覆盖单例清单与预算、初始化时序三套手写解法、唯一执行序属性、`SaveRegistry` 登记处契约、UI 画布焦点栈契约。
- 写入的文件：
  - `Y_MultipleAgentWorkflow\Architecture\Composition\Composition_Guide.md`（`ARCH-COMPOSITION-GUIDE`，Active）
  - `Y_MultipleAgentWorkflow\Architecture\Composition\Router.md`（重写为中文模板格式）
  - `Y_MultipleAgentWorkflow\Architecture\Composition\DeveloperLog.md`（本条目）
- 依据的证据路径：
  - `Assets/Scripts/Contracts/YSingleton.cs`、`SaveableService.cs`、`SaveRegistry.cs`、`ISaveable.cs`、`ICanvasManager.cs`、`MyEnums.cs`
  - `Assets/Scripts/Pipeline/TimeManager.cs`、`Pipeline/InitialLoad.cs`、`Pipeline/Scene/{SceneChanger,SceneDataForSave,CameraPixelSnap,PersistentSceneRegistry}.cs`
  - `Assets/Scripts/Pipeline/UI/SystemCanvasManagers/{UIManager,CanvasFocusStack,IntegratedUICanvasManager}.cs`
  - `Assets/Scripts/Gameplay/Player/Services/StatsService.cs`、`Gameplay/Skills/SkillTreeManager.cs`、`Gameplay/Save/{SaveSystem,SaveDataManager,SaveLoadCanvasManager}.cs`、`Gameplay/Inventory/Loot.cs`
  - `Assets/Tests/Editor/CanvasFocusStackTests.cs`
  - `Docs/My_ARPG_MVCS项目现状.md`（仅作线索）
- 已核验（静态核对已做）：
  - 全工程单例实数：18 个直接继承 `YSingleton<T>` + `StatsService` 经抽象基类 `SaveableService<TSelf>` 间接继承 = 19 个具体单例；`SaveableService` 自身为唯一抽象派生。
  - `Assets/**` 内 `DefaultExecutionOrder` 仅 1 处（`CameraPixelSnap.cs:9`）；`ProjectSettings/` 无 `MonoManager.asset`、无 `m_ScriptExecutionOrder`。
  - `RuntimeInitializeOnLoadMethod` 仅 2 处：`SaveRegistry.cs:18`、`PersistentSceneRegistry.cs:39`，均为 `SubsystemRegistration`。
  - `OnSingletonInitialized` 重写点 6 处；18 个单例派生类均未重写 `Awake`。
  - `ICanvasManager` 实现者 10 个；`Assets/**` 内 `*.asmdef` 数量为 0。
- 未核验：场景/prefab 资产上的单例挂载位置与接线、`EditorSettings.asset` 的 Domain Reload 字段值、运行时 `Awake` 顺序与 `CameraPixelSnap` 的实际画面结果（未运行 Unity）。
- 发现的缺陷：`SaveRegistry.Clear()` 零调用点；`SaveSystem.IsLoadingSaveRequest` 与 `SceneChanger.isLoading` 缺 try/finally 会造成标志位卡死；`RefreshCanvaOrder` 在 `UIManager.Instance == null` 时静默降级；“广播先于 isLoading 置位”使标志位拦不住广播段重入；`VisitedHistoryManager` 为空壳单例；单例预算实际超 6 项。
- 维护计数：`0/5 -> 1/5`