# 测试基线与覆盖

文档 ID：`BUILD-TESTBASELINE-GUIDE`
状态：`Active`
最后更新：`2026-11-01`
权威范围：只负责本工程自动化测试的现状事实——位置、框架、用例分布、覆盖与零覆盖域。逐条用例的断言内容归各业务域 Guide；测试执行命令见 `..\Workflow\Project_Validation_Guide.md`。本域只描述已落地的测试，不列测试计划。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `跑测试` / `EditMode 测试` | §2.6 |
| `PlayMode 测试` | §2.7 |
| `测试总数` / `测试全绿` / `用例数` | §2.2、§2.3 |
| `test-framework` / `NUnit` | §2.1 |
| `Test Runner 跑不出来` / `asmdef` | §3.2、§3.6 |
| `新增单元测试` / `测试放哪` | §3.1 |
| `改 PlayerStatsModel 前先跑什么` | §2.5 |
| `哪些域没有测试` | §2.5 |
| `测试退出码` / `testResults` XML | §2.6 |

## 2. 当前实现

### 2.1 位置与框架

- 测试集中在 `Assets/Tests/Editor` 目录下，共 6 个文件，命名空间统一为 `Gameplay.Tests`。
- 框架为 Unity Test Framework **1.4.6**；断言用 NUnit，`nunit.framework` 由扩展 NUnit 包提供。
- **0 个 asmdef**。测试与编辑器侧的 `InputActionReferenceRebuilder` 一起被编译进预定义编辑器程序集 `Assembly-CSharp-Editor`，该程序集引用 `Assembly-CSharp`。

### 2.2 用例分布

6 个文件，共 61 项用例：

| 文件 | 用例数 | 被测对象 |
|---|---|---|
| `PlayerStatsModelTests` | 24 | `PlayerStatsModel` / `StatsService`：钳制、事件广播、经验曲线、等级上限、拷贝语义 |
| `CanvasFocusStackTests` | 15 | `CanvasFocusStack`：开闭顺序、order 计算、ESC 行为 |
| `AStarOpenHeapTests` | 6 | `AStarOpenHeap`：堆序、平局规则、重复入堆、随机输入排序 |
| `ObjectPoolTests` | 5 | `ObjectPool<T>`：预热、Get/Return 回调、池内对象销毁后的选取 |
| `PlayerStatsSOTests` | 2 | `PlayerStatsSO.CreateInitialData()`：模板拷贝语义 |
| `UICacheRegressionTests` | 9 | 任务选择与关闭、快照版本确认与只读性、背包槽位地址、金币局部刷新、对话与任务的开关路由 |
| **合计** | **61** | |

- 特性统计：`[Test]` 59，`[TestCase]` 2，`[UnityTest]` 0，`[TestFixture]` 0，`[Category]` 0。
- `[SetUp]` 与 `[TearDown]` 分布在 `ObjectPoolTests`、`PlayerStatsSOTests`、`PlayerStatsModelTests`、`UICacheRegressionTests`，`CanvasFocusStackTests` 亦有 `SetUp`。

### 2.3 用例计数的口径

- 每个 `[Test]` 计一项，每个 `[TestCase]` 参数实例计一项；用例数按此口径汇总。

### 2.4 选型：测试落在不依赖 UnityEngine 的纯 C# 逻辑上

- `AStarOpenHeap` 的说明写明它是“A* 开表（open list）用的二叉小顶堆，**不依赖 UnityEngine**”；对应测试只引入 `System`、`System.Collections.Generic` 与 `NUnit.Framework`。
- `CanvasFocusStack` 的说明写明它是“**纯 C# 逻辑**……不依赖 MonoBehaviour 与 ToggleCanvasEventSO……**便于脱离 Unity 生命周期做单元测试**”；对应测试只引入 `System.Collections.Generic`、`Contracts`、`Pipeline.UI.SystemCanvasManagers` 与 `NUnit.Framework`，被测类型与 `CanvasToToggle` 都来自不含 UnityEngine 类型的程序集代码。
- 命名空间改造后，测试侧引用的产品类型全部要显式 `using`：`CanvasFocusStackTests` 用 `using Contracts;`（`CanvasToToggle`）与 `using Pipeline.UI.SystemCanvasManagers;`（`CanvasFocusStack`）；`AStarOpenHeapTests` 用 `using Pipeline.Pathfinding;`；`ObjectPoolTests` 用 `using Pipeline;`（`IPoolable`）；`PlayerStatsSOTests` 用 `using Pipeline.SO;`（`PlayerStatsSO`）。漏写任何一条都是 `CS0246`，不会像全局命名空间时代那样静默可用。

各测试文件的引擎依赖：

| 文件 | 是否 `using UnityEngine` | 说明 |
|---|---|---|
| `AStarOpenHeapTests` | 否 | 6 个用例只引入 `System`、`System.Collections.Generic` 与 `NUnit.Framework` |
| `CanvasFocusStackTests` | 否 | 15 个用例只引入 `System.Collections.Generic`、`Contracts`、`Pipeline.UI.SystemCanvasManagers` 与 `NUnit.Framework` |
| `PlayerStatsModelTests` | 是 | 24 个用例中 1 个用引擎 API：`LogAssert.Expect(LogType.Warning, ...)` |
| `ObjectPoolTests` | 是 | 5 个用例构造 `GameObject` 与 `MonoBehaviour`，依赖 EditMode 下的 `Object.DestroyImmediate` |
| `PlayerStatsSOTests` | 是 | 2 个用例用 `ScriptableObject.CreateInstance` 与 `Object.DestroyImmediate` |
| `UICacheRegressionTests` | 是 | 临时组件与 SO 验证业务状态；读取实际预制体验证打开策略和关闭路由 |

这套测试整体需要 Unity 才能运行。

### 2.5 覆盖与零覆盖

有覆盖的对象包括 `PlayerStatsModel` / `StatsService`、`CanvasFocusStack`、`AStarOpenHeap`、`ObjectPool<T>`、`PlayerStatsSO`，以及任务视图字段刷新、快照过期确认、集合只读性、背包槽位、快捷栏局部刷新与缓存事件路由。

缓存回归覆盖对话与任务预制体的开关策略、任务选择与关闭、背包槽位地址、快捷栏局部刷新。商店交易、完整对话流程、存档、场景编排与加载、技能、单位与 NPC 的业务路径依靠手工验证。

改 `PlayerStatsModel`、`StatsService`、`ExperienceController` 之前先跑绿 `PlayerStatsModelTests`。

### 2.6 唯一的已记录执行方式

headless EditMode 命令把 Editor 路径与工程路径写死；两个前提：**Unity 编辑器不能开着这个工程**，否则第二个实例起不来；**结果看 XML 根节点的 `result` 与失败计数，退出码不可靠**。

完整命令与环境检查清单见 `..\Workflow\Project_Validation_Guide.md` §3。

### 2.7 无 PlayMode 测试

用例使用 NUnit 的 `[Test]` 与 `[TestCase]` 在 EditMode 执行。“进场景 / 场景切换 / 存档往返”这类路径靠手工验证，真机冒烟见 `AndroidBuild_Guide.md` §2.7。

## 3. 约定与硬边界

### 3.1 `Tests/Editor` 这个目录名是承重的

它让测试落进预定义编辑器程序集 `Assembly-CSharp-Editor`。移动测试目录、或把测试挪出 `Editor` 子目录，都会改变程序集归属并影响编译与枚举。

新增单元测试统一放在 `Assets\Tests\Editor\`，命名空间用 `Gameplay.Tests`。

### 3.2 新增 asmdef 会排除现有测试

当前测试由预定义编辑器程序集编译；引入 asmdef 后必须显式引用 `UnityEngine.TestRunner`、`UnityEditor.TestRunner`、test-framework 与产品程序集。

### 3.3 不要用 IDE 用的 MSBuild 目标文件判断测试是否被排除

它只把测试源文件从 MSBuild/IDE 项目模型摘掉，注释明确 Unity 编译与 Test Runner 不受影响。后果：IDE 里没有补全、跳转与 Find Usages，但测试照跑。

### 3.4 退出码不是成功判据

判据是 `testResults` XML 根节点的 `result` 与失败计数。

### 3.5 测试数量

用例分布与计数口径见 §2.2、§2.3。

### 3.6 同一工程不能同时开两个 Editor 实例

跑 headless 测试前必须关掉已打开的该工程实例。
