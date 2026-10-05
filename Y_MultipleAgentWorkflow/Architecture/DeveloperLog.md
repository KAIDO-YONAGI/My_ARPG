# Architecture Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Architecture 域中文 Router 与两个子域权威文档

- 任务：为 Architecture 域重写中文模板 Router，并建立 `Composition`（Active）与 `AssemblyPlan`（Proposal）两个子域的权威文档与子 Router。
- 写入的文件：
  - `Y_MultipleAgentWorkflow\Architecture\Router.md`（重写；3 行下级导航原样保留）
  - `Y_MultipleAgentWorkflow\Architecture\Composition\Composition_Guide.md`（`ARCH-COMPOSITION-GUIDE`，Active）
  - `Y_MultipleAgentWorkflow\Architecture\Composition\Router.md`、`Composition\DeveloperLog.md`
  - `Y_MultipleAgentWorkflow\Architecture\AssemblyPlan\AssemblyPlan_Proposal.md`（`ARCH-ASSEMBLY-PLAN`，Proposal，未实施）
  - `Y_MultipleAgentWorkflow\Architecture\AssemblyPlan\Router.md`、`AssemblyPlan\DeveloperLog.md`
  - `Y_MultipleAgentWorkflow\Architecture\DeveloperLog.md`（本条目）
- 依据的证据路径：
  - 组合与时序：`Assets/Scripts/Contracts/{YSingleton,SaveableService,SaveRegistry,ISaveable,ICanvasManager,MyEnums}.cs`、`Pipeline/{TimeManager,InitialLoad}.cs`、`Pipeline/Scene/{SceneChanger,SceneDataForSave,CameraPixelSnap,PersistentSceneRegistry}.cs`、`Pipeline/UI/SystemCanvasManagers/{UIManager,CanvasFocusStack,IntegratedUICanvasManager}.cs`、`Gameplay/Player/Services/StatsService.cs`、`Gameplay/Skills/SkillTreeManager.cs`、`Gameplay/Save/{SaveSystem,SaveDataManager,SaveLoadCanvasManager}.cs`、`Assets/Tests/Editor/CanvasFocusStackTests.cs`
  - 程序集与热更：`Docs/My_ARPG_重构优化清单_未解决.md`（该文件已由用户在 2026-10-05 删除，本轮按 `git HEAD` 版本核对）、`Assets/Scripts/Pipeline/SO/Events/*.cs`、`Gameplay/Skills/SkillManager.cs:19-21`、`Gameplay/Inventory/UseItem.cs:8-19`、`Gameplay/Player/PlayerMovement.cs:171`
  - 线索来源（非权威）：`Docs/My_ARPG_MVCS项目现状.md`
- 已核验（静态核对）：单例 19 个具体 + 1 个抽象基类；`DefaultExecutionOrder` 全工程 1 处且 `ProjectSettings` 无执行序配置；`RuntimeInitializeOnLoadMethod` 2 处；`ICanvasManager` 实现者 10 个；`OnSingletonInitialized` 重写点 6 个；`Assets/**` asmdef 数为 0；无 xLua/Lua 资产与 `LuaManager`；事件类 11 个 / 通道 12 条 / 事件资产 28 个。
- 未核验：场景与 prefab 上的单例挂载与接线、`EditorSettings.asset` 的 Domain Reload 字段值、运行时 `Awake` 顺序实测、拆 asmdef 的可编译性、用户排期意愿（未运行 Unity，未做编译验证）。
- 发现的缺陷：`SaveRegistry.Clear()` 零调用点；`SaveSystem.IsLoadingSaveRequest` 与 `SceneChanger.isLoading` 缺 try/finally 会卡死标志位；`RefreshCanvaOrder` 在 `UIManager.Instance == null` 时静默降级；`RequestSceneLoad` 广播先于 `isLoading` 置位导致拦不住广播段重入；`VisitedHistoryManager` 为空壳单例；单例预算实际超 6 项；上游文档事件类路径/资产计数过期、`MovementController.cs:187` 与 `StatsManager`/`StatsBridge` 均为失效指针。
- 维护计数：`0/5 -> 1/5`