# Architecture.Layering Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 分层边界与读写约定 权威文档

**写入的文件**

- `Layering_Guide.md`，文档 ID `ARCH-LAYERING-GUIDE`，新建，状态 Active。
- `Router.md`，按项目中文模板重写，无下级导航。
- `DeveloperLog.md`，本条目。

本轮写入范围限于 `Y_MultipleAgentWorkflow` 下的文档。

**依据的证据**

- 三层目录实测：`Contracts` 8 个、`Gameplay` 59 个、`Pipeline` 57 个，合计 124 个产品代码文件。
- 数值线 MVCS：`Player` 下的 `Models`、`Services`、`Controllers`、`Views` 四个层目录。
- 契约与基建：`YSingleton`、`SaveableService`、`SaveRegistry`、`ISaveable`、`ICanvasManager`、`MyEnums` 六个类型。
- 管线侧：`PlayerStatsSO`、`UIManager`、`CanvasFocusStack` 三个类型。
- 存档侧交叉核对：`SaveData`、`SaveDataManager` 两个类型。
- 测试基线：编辑器侧测试 5 个文件。
- 流程约束：`..\..\Workflow\WorkflowInstance.json` 的 `Architecture.Layering` 分类、`..\..\Workflow\Templates\BusinessRouter.template.md` 模板。

**本轮核对的静态事实**

- 跨层 `using` 实测：`Contracts → Gameplay/Pipeline` 为 0 处；`Gameplay → Pipeline` 为 0 处；`Pipeline → Gameplay` 为 1 处，位于 `PlayerStatsSO` 顶部的 `using Gameplay.Player.Models;`。
- 命名空间覆盖：124 个产品代码文件中 13 个带 namespace，含 `MyEnums` 1 个与 `Gameplay.Player.*` 12 个，其余 111 个在全局命名空间。
- 全工程 asmdef 计数为 0，只有 `Assembly-CSharp` 一个程序集。
- 单例规模：直接继承 `YSingleton<T>` 的具体类 18 个，另有抽象基类 `SaveableService<TSelf>`，其子类只有 `StatsService`。
- 测试基线复算：编辑器侧测试 5 个文件、52 个 `[Test]`，其中 `PlayerStatsModelTests` 24、`CanvasFocusStackTests` 15、`AStarOpenHeapTests` 6、`ObjectPoolTests` 5、`PlayerStatsSOTests` 2，无 `[UnityTest]`。
- 唯一写入口判定链：`IPlayerStatsReadOnly` 有 4 个事件与 15 个只读属性，成员集合里没有写方法；读入口是 `StatsService.Stats`，写入口是 6 个单行转发命令，`model`、`GetStats`、`LoadStats` 均为 private；`PlayerStatsModelTests` 断言 `StatsService` 的公开成员里没有 `Model` 属性。

**维护计数**：`0/5`，本次为首次建档。

## 2026-10-31 命名空间重构后复核分层事实

**触发**

用户整理命名空间：`Assets\Scripts` 下 124 个产品代码文件全部收进与目录同构的命名空间，原 `MyEnums` 类拆成 10 个顶层枚举。测试侧 5 个文件未同步 `using`，Unity 报 6 个 `CS0246`。

**写入的文件**

- `Layering_Guide.md`：§1 线索表、§2.1、§2.2、§2.3、§2.5、§3.2、§3.4 按实测重写；`最后更新` 改为 `2026-10-31`。
- `Router.md`：`维护计数` 0/5 → 1/5，能力边界三条改为实测数字。
- 本条目。

**依据的证据**

- 命名空间实测：124 个产品代码文件全部带 `namespace`，零个落在全局命名空间；命名空间共 25 个，与目录一一对应。
- 逐文件 `using` 统计：`Contracts → Gameplay` 2、`Contracts → Pipeline` 3、`Gameplay → Contracts` 29、`Gameplay → Pipeline` 62、`Pipeline → Contracts` 22、`Pipeline → Gameplay` 6。
- `Contracts\MyEnums.cs` 现状：`namespace Contracts` 下 `PlayerState`、`EnemyState`、`SceneType`、`PersistentType`、`AStarNodeType`、`NPCState`、`QuestState`、`CanvasToToggle`、`SaveType`、`SaveLoadPanelType` 共 10 个枚举。
- `Player` 目录 18 个文件的命名空间分布：`Gameplay.Player` 7、`.Controllers` 4、`.Models` 3、`.Views` 3、`.Services` 1。
- 编辑器侧 `Assets\Editor\InputActionReferenceRebuilder.cs` 仍是全局命名空间，是仅剩的一个。
- 编译验证：Roslyn `csc.dll` 配 Unity 2022.3.62f3c1 的 286 条程序集引用，编译 130 个源文件，0 error 0 warning。

**结论**

- 旧的"跨层依赖 0 处"不成立，但它不是这轮重构引入的耦合：全局命名空间时代跨层引用不需要 `using`，依赖统计不到。新数字才是真实依赖图。
- §3.4 从"已满足的约束"改记为"违反 5 处的待偿债务"，因为契约层现在 `using` 了 `Gameplay.Save`、`Pipeline.SO.Events`、`Pipeline.UI.SystemCanvasManagers`、`Pipeline.SO`。

**维护计数**：`0/5` → `1/5`。
