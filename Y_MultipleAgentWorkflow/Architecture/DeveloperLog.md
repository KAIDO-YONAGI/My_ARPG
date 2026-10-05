# Architecture Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

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
