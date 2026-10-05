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
