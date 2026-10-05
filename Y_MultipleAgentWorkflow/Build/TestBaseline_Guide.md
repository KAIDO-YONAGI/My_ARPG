# 测试基线与覆盖

文档 ID：`BUILD-TESTBASELINE-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
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

- 测试集中在 `Tests/Editor` 目录下，共 **5 个文件**，命名空间统一为 `Gameplay.Tests`。
- 框架为 Unity Test Framework **1.4.6**；断言用 NUnit，`nunit.framework` 由扩展 NUnit 包提供。
- **0 个 asmdef**。测试与编辑器侧的 `InputActionReferenceRebuilder` 一起被编译进预定义编辑器程序集 `Assembly-CSharp-Editor`，该程序集引用 `Assembly-CSharp`。

### 2.2 用例分布

5 个文件、52 个 `[Test]`：

| 文件 | `[Test]` 数 | 被测对象 |
|---|---|---|
| `PlayerStatsModelTests` | 24 | `PlayerStatsModel` / `StatsService`：钳制、事件广播、经验曲线、等级上限、拷贝语义 |
| `CanvasFocusStackTests` | 15 | `CanvasFocusStack`：开闭顺序、order 计算、ESC 行为 |
| `AStarOpenHeapTests` | 6 | `AStarOpenHeap`：堆序、平局规则、重复入堆、随机输入排序 |
| `ObjectPoolTests` | 5 | `ObjectPool<T>`：预热、Get/Return 回调、池内对象销毁后的选取 |
| `PlayerStatsSOTests` | 2 | `PlayerStatsSO.CreateInitialData()`：模板拷贝语义 |
| **合计** | **52** | |

- 特性统计：`[Test]` 52，`[TestCase]` 0，`[UnityTest]` 0，`[TestFixture]` 0，`[Category]` 0。
- `[SetUp]` 与 `[TearDown]` 分布在 `ObjectPoolTests`、`PlayerStatsSOTests`、`PlayerStatsModelTests`，`CanvasFocusStackTests` 亦有 `SetUp`。

### 2.3 用例计数的口径

- 本指南的口径：5 个文件、52 个 `[Test]`，与 §2.2 的表格一致。

### 2.4 选型：测试落在不依赖 UnityEngine 的纯 C# 逻辑上

- `AStarOpenHeap` 的说明写明它是“A* 开表（open list）用的二叉小顶堆，**不依赖 UnityEngine**”；对应测试只引入 `System`、`System.Collections.Generic` 与 `NUnit.Framework`。
- `CanvasFocusStack` 的说明写明它是“**纯 C# 逻辑**……不依赖 MonoBehaviour 与 ToggleCanvasEventSO……**便于脱离 Unity 生命周期做单元测试**”；对应测试只引入 `System.Collections.Generic`、`MyEnums` 与 `NUnit.Framework`，而 `MyEnums` 是不含 UnityEngine 类型的枚举命名空间。

各测试文件的引擎依赖：

| 文件 | 是否 `using UnityEngine` | 说明 |
|---|---|---|
| `AStarOpenHeapTests` | 否 | 6 个用例只引入 `System`、`System.Collections.Generic` 与 `NUnit.Framework` |
| `CanvasFocusStackTests` | 否 | 15 个用例只引入 `System.Collections.Generic`、`MyEnums` 与 `NUnit.Framework` |
| `PlayerStatsModelTests` | 是 | 24 个用例中 1 个用引擎 API：`LogAssert.Expect(LogType.Warning, ...)` |
| `ObjectPoolTests` | 是 | 5 个用例构造 `GameObject` 与 `MonoBehaviour`，依赖 EditMode 下的 `Object.DestroyImmediate` |
| `PlayerStatsSOTests` | 是 | 2 个用例用 `ScriptableObject.CreateInstance` 与 `Object.DestroyImmediate` |

这套测试整体需要 Unity 才能运行。

### 2.5 覆盖与零覆盖

有覆盖的只有 4 个被测类型加 1 个 SO：`PlayerStatsModel` / `StatsService`、`CanvasFocusStack`、`AStarOpenHeap`、`ObjectPool<T>`、`PlayerStatsSO`。

**零覆盖域**：对话、任务、商店与背包、存档、场景编排与加载、技能、单位与 NPC；测试目录下对这些域无任何引用，其中场景编排与加载指 `SceneChanger` 与 `InitialLoad`。

改 `PlayerStatsModel`、`StatsService`、`ExperienceController` 之前先跑绿 `PlayerStatsModelTests`。

### 2.6 唯一的已记录执行方式

headless EditMode 命令把 Editor 路径与工程路径写死；两个前提：**Unity 编辑器不能开着这个工程**，否则第二个实例起不来；**结果看 XML 根节点的 `result` 与失败计数，退出码不可靠**。

完整命令与环境检查清单见 `..\Workflow\Project_Validation_Guide.md` §3。

### 2.7 无 PlayMode 测试

52 个用例全为 `[Test]`，见 §2.2；`[UnityTest]` 为 0，仓库中不存在显式的 PlayMode 测试。“进场景 / 场景切换 / 存档往返”这类路径目前靠手工验证，真机冒烟见 `AndroidBuild_Guide.md` §2.7。

## 3. 约定与硬边界

### 3.1 `Tests/Editor` 这个目录名是承重的

它让测试落进预定义编辑器程序集 `Assembly-CSharp-Editor`。移动测试目录、或把测试挪出 `Editor` 子目录，都会改变程序集归属并影响编译与枚举。

新增单元测试统一放在 `Assets\Tests\Editor\`，命名空间用 `Gameplay.Tests`。

### 3.2 新增 asmdef 会排除现有测试

当前 0 asmdef 是“测试能自动被 Test Runner 看到”的前提。引入 asmdef 后这 5 个文件改由新程序集编译，必须显式引用 `UnityEngine.TestRunner`、`UnityEditor.TestRunner`、test-framework 与产品程序集，否则编译不通过或测试无法被枚举。

### 3.3 不要用 IDE 用的 MSBuild 目标文件判断测试是否被排除

它只把测试源文件从 MSBuild/IDE 项目模型摘掉，注释明确 Unity 编译与 Test Runner 不受影响。后果：IDE 里没有补全、跳转与 Find Usages，但测试照跑。

### 3.4 退出码不是成功判据

判据是 `testResults` XML 根节点的 `result` 与失败计数。

### 3.5 测试数量

5 个文件、52 个 `[Test]`，见 §2.2。

### 3.6 同一工程不能同时开两个 Editor 实例

跑 headless 测试前必须关掉已打开的该工程实例。
