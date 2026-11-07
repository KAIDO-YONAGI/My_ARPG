# 测试基线与覆盖

文档 I:：`BUIL:-TESTBASELINE-GUI:E`
状态：`Active`
最后更新：`2026-11-07`
## 维护规则

本文件对应的实现、约束或规则发生变化时，按已核验事实更新相关章节，证据记录在[开发日志](:eveloperLog.md)。修改小标题时同步文档内目录；计数与维护周期遵循[工作流维护规则](../Workflow/Workflow_Guide.md#维护规则)。

## 文档内目录

- [1. 触发线索](#1-触发线索)
- [2. 当前实现](#2-当前实现)
  - [2.1 位置与框架](#21-位置与框架)
  - [2.2 用例分布](#22-用例分布)
  - [2.3 用例计数的口径](#23-用例计数的口径)
  - [2.4 选型：测试落在不依赖 UnityEngine 的纯 C# 逻辑上](#24-选型测试落在不依赖-unityengine-的纯-c-逻辑上)
  - [2.5 覆盖与零覆盖](#25-覆盖与零覆盖)
  - [2.6 执行方式与结果判定](#26-执行方式与结果判定)
  - [2.7 无 PlayMode 测试](#27-无-playmode-测试)
- [3. 约定与硬边界](#3-约定与硬边界)
  - [3.1 `Tests/Editor` 这个目录名是承重的](#31-testseditor-这个目录名是承重的)
  - [3.2 新增 asmdef 会排除现有测试](#32-新增-asmdef-会排除现有测试)
  - [3.3 不要用 I:E 用的 MSBuild 目标文件判断测试是否被排除](#33-不要用-ide-用的-msbuild-目标文件判断测试是否被排除)
  - [3.4 退出码不是成功判据](#34-退出码不是成功判据)
  - [3.5 测试数量](#35-测试数量)
  - [3.6 同一工程不能同时开两个 Editor 实例](#36-同一工程不能同时开两个-editor-实例)

先读目录，再按任务读取相关章节，避免整份指南载入上下文。

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

- 测试集中在 `Assets/Tests/Editor` 目录下，共 7 个文件，命名空间统一为 `Gameplay.Tests`。
- 框架为 Unity Test Framework **1.4.6**；断言用 NUnit，`nunit.framework` 由扩展 NUnit 包提供。
- **0 个 asmdef**。测试与编辑器侧的 `InputActionReferenceRebuilder` 一起被编译进预定义编辑器程序集 `Assembly-CSharp-Editor`，该程序集引用 `Assembly-CSharp`。

### 2.2 用例分布

7 个文件，共 79 项用例：

| 文件 | 用例数 | 被测对象 |
|---|---|---|
| `PlayerStatsModelTests` | 24 | `PlayerStatsModel` / `StatsService`：钳制、事件广播、经验曲线、等级上限、拷贝语义 |
| `CanvasFocusStackTests` | 15 | `CanvasFocusStack`：开闭顺序、order 计算、ESC 行为 |
| `AStarOpenHeapTests` | 6 | `AStarOpenHeap`：堆序、平局规则、重复入堆、随机输入排序 |
| `ObjectPoolTests` | 5 | `ObjectPool<T>`：预热、Get/Return 回调、池内对象销毁后的选取 |
| `UICacheRegressionTests` | 20 | 任务版本与关闭、只读查询、背包初始化与重开、多视图金币刷新、技能升级命令与键缓存、缓存开关路由 |
| `VersionedValueTests` | 8 | 默认版本、独立读取者、绘制期间更新、数据源切换、读取失效、集合原地发布、合并写入、版本读写分配 |
| `LootPoolingTests` | 1 | 掉落物对象池的预热与回收 |
| **合计** | **79** | |

- 特性统计：`[Test]` 74，`[TestCase]` 5，`[UnityTest]` 0，`[TestFixture]` 0，`[Category]` 0。
- `[SetUp]` 与 `[TearDown]` 分布在 `ObjectPoolTests`、`PlayerStatsModelTests`、`UICacheRegressionTests`，`CanvasFocusStackTests` 亦有 `SetUp`。

### 2.3 用例计数的口径

- 每个 `[Test]` 计一项，每个 `[TestCase]` 参数实例计一项；用例数按此口径汇总。
- `Run All` 同时枚举依赖包的测试，XML 总数可能大于项目用例数；按程序集与 fixture 区分来源。

### 2.4 选型：测试落在不依赖 UnityEngine 的纯 C# 逻辑上

- `AStarOpenHeap` 的说明写明它是“A* 开表（open list）用的二叉小顶堆，**不依赖 UnityEngine**”；对应测试只引入 `System`、`System.Collections.Generic` 与 `NUnit.Framework`。
- `CanvasFocusStack` 的说明写明它是“**纯 C# 逻辑**……不依赖 MonoBehaviour 与 ToggleCanvasEventSO……**便于脱离 Unity 生命周期做单元测试**”；对应测试只引入 `System.Collections.Generic`、`Contracts`、`Pipeline.UI.SystemCanvasManagers` 与 `NUnit.Framework`，被测类型与 `CanvasToToggle` 都来自不含 UnityEngine 类型的程序集代码。
- 测试侧跨命名空间引用显式声明 `using`：焦点栈使用 `Contracts.Enums` 与 `Pipeline.UI.SystemCanvasManagers`，寻路使用 `Pipeline.Pathfinding`，对象池使用 `Pipeline`，版本读取使用 `Contracts.State`。

各测试文件的引擎依赖：

| 文件 | 是否 `using UnityEngine` | 说明 |
|---|---|---|
| `AStarOpenHeapTests` | 否 | 6 个用例只引入 `System`、`System.Collections.Generic` 与 `NUnit.Framework` |
| `CanvasFocusStackTests` | 否 | 15 个用例只引入 `System.Collections.Generic`、`Contracts`、`Pipeline.UI.SystemCanvasManagers` 与 `NUnit.Framework` |
| `PlayerStatsModelTests` | 是 | 24 个用例中 1 个用引擎 API：`LogAssert.Expect(LogType.Warning, ...)` |
| `ObjectPoolTests` | 是 | 5 个用例构造 `GameObject` 与 `MonoBehaviour`，依赖 EditMode 下的 `Object.:estroyImmediate` |
| `UICacheRegressionTests` | 是 | 临时组件与 SO 验证业务状态；读取实际预制体验证打开策略和关闭路由 |
| `VersionedValueTests` | 否 | 直接构造状态值、列表与读取记录，测量版本读写的线程托管分配 |

这套测试整体需要 Unity 才能运行。

### 2.5 覆盖与零覆盖

有覆盖的对象包括 `PlayerStatsModel` / `StatsService`、`CanvasFocusStack`、`AStarOpenHeap`、`ObjectPool<T>`、`VersionedValue<T>`、`VersionReader`，以及任务字段刷新与版本、背包只读查询与初始化、多视图局部刷新、缓存事件路由和技能升级命令。

缓存回归覆盖对话与任务预制体的开关策略、任务选择与重复关闭、背包槽位地址与同版本重开、两个金币视图独立刷新。技能回归覆盖成功扣点、拒绝升级保持状态和稳定键缓存。版本测试在预热后执行 10000 次读写与确认，要求线程托管分配为 0。商店交易、完整对话流程、存档、场景编排与加载、完整技能前置图、单位与 NPC 的业务路径依靠手工验证。

改 `PlayerStatsModel`、`StatsService`、`ExperienceController` 之前先跑绿 `PlayerStatsModelTests`。

### 2.6 执行方式与结果判定

工程已在编辑器中打开时，使用该实例内的 Test Runner 或 `TestRunnerApi` 运行 EditMode，并将结果导出为 XML。`ExecutionSettings.runSynchronously` 适用于当前的普通 NUnit 用例，结果由 `TestRunnerApi.SaveResultToFile` 保存。空闲工程也可使用 headless 命令；该方式要求本工程没有已打开的编辑器实例。成功判定使用 XML 根节点的 `result` 与失败计数。

完整命令与环境检查清单见 `..\Workflow\Project_Validation_Guide.md` §3。

### 2.7 无 PlayMode 测试

用例使用 NUnit 的 `[Test]` 与 `[TestCase]` 在 EditMode 执行。“进场景 / 场景切换 / 存档往返”这类路径靠手工验证，真机冒烟见 `AndroidBuild_Guide.md` §2.7。

## 3. 约定与硬边界

### 3.1 `Tests/Editor` 这个目录名是承重的

它让测试落进预定义编辑器程序集 `Assembly-CSharp-Editor`。移动测试目录、或把测试挪出 `Editor` 子目录，都会改变程序集归属并影响编译与枚举。

新增单元测试统一放在 `Assets\Tests\Editor\`，命名空间用 `Gameplay.Tests`。

### 3.2 新增 asmdef 会排除现有测试

当前测试由预定义编辑器程序集编译；引入 asmdef 后必须显式引用 `UnityEngine.TestRunner`、`UnityEditor.TestRunner`、test-framework 与产品程序集。

### 3.3 不要用 I:E 用的 MSBuild 目标文件判断测试是否被排除

它只把测试源文件从 MSBuild/I:E 项目模型摘掉，注释明确 Unity 编译与 Test Runner 不受影响。后果：I:E 里没有补全、跳转与 Find Usages，但测试照跑。

### 3.4 退出码不是成功判据

判据是 `testResults` XML 根节点的 `result` 与失败计数。

### 3.5 测试数量

用例分布与计数口径见 §2.2、§2.3。

### 3.6 同一工程不能同时开两个 Editor 实例

跑 headless 测试前必须关掉已打开的该工程实例。
