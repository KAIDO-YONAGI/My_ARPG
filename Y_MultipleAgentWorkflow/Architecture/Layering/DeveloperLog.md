# Architecture.Layering Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 分层边界与读写约定 权威文档

**写入的文件**

- `Y_MultipleAgentWorkflow\Architecture\Layering\Layering_Guide.md`（文档 ID `ARCH-LAYERING-GUIDE`，新建，状态 Active）
- `Y_MultipleAgentWorkflow\Architecture\Layering\Router.md`（按项目中文模板重写，无下级导航）
- `Y_MultipleAgentWorkflow\Architecture\Layering\DeveloperLog.md`（本条目）

未写入任何 `Assets/**`、`ProjectSettings/**`、`Packages/**`、`Docs/**`、`README*.md` 或 `.cs`。

**依据的证据路径**

- 旧架构权威（仅作线索）：`Docs\My_ARPG_MVCS项目现状.md`
- 三层目录实测：`Assets\Scripts\Contracts\`（8 .cs）、`Assets\Scripts\Gameplay\`（59 .cs）、`Assets\Scripts\Pipeline\`（57 .cs），合计 124 .cs
- 数值线 MVCS：`Assets\Scripts\Gameplay\Player\{Models,Services,Controllers,Views}\`
- 契约与基建：`Assets\Scripts\Contracts\{YSingleton,SaveableService,SaveRegistry,ISaveable,ICanvasManager,MyEnums}.cs`
- 管线侧：`Assets\Scripts\Pipeline\SO\PlayerStatsSO.cs`、`Assets\Scripts\Pipeline\UI\SystemCanvasManagers\{UIManager,CanvasFocusStack}.cs`
- 存档侧交叉核对：`Assets\Scripts\Gameplay\Save\{SaveData,SaveDataManager}.cs`
- 测试基线：`Assets\Tests\Editor\` 5 个文件
- 流程约束：`Y_MultipleAgentWorkflow\Workflow\WorkflowInstance.json`（category `Architecture.Layering`）、`Workflow\Templates\BusinessRouter.template.md`

**已核验项（静态检索 + 逐文件阅读，未启动 Unity）**

- 跨层 `using` 实测：`Contracts → Gameplay/Pipeline` = 0；`Gameplay → Pipeline` = 0；`Pipeline → Gameplay` = 1（`Pipeline\SO\PlayerStatsSO.cs:2`）。
- 命名空间覆盖：124 个 .cs 中 13 个带 namespace（`MyEnums` 1 个 + `Gameplay.Player.*` 12 个），111 个在全局命名空间。
- `Assets/` 下 `*.asmdef` 计数 = 0 ⇒ 全工程一个 `Assembly-CSharp`。
- 单例规模：直接继承 `YSingleton<T>` 的具体类 18 个，另有抽象基类 `SaveableService<TSelf>`，其子类仅 `StatsService`。
- 测试基线复算：`Assets/Tests/Editor` 5 文件、52 个 `[Test]`（`PlayerStatsModelTests` 24、`CanvasFocusStackTests` 15、`AStarOpenHeapTests` 6、`ObjectPoolTests` 5、`PlayerStatsSOTests` 2），无 `[UnityTest]`。
- 唯一写入口判定链：`IPlayerStatsReadOnly`（4 事件 + 15 只读属性，无写方法）→ `StatsService.Stats`（`StatsService.cs:24`）→ 6 个写命令单行转发（`StatsService.cs:40-50`）→ `model`/`GetStats`/`LoadStats` 均 private（`StatsService.cs:21,53,56`）；`PlayerStatsModelTests.cs:171-180` 断言 `StatsService` 无 `Model` 属性。
- 旧文档排版缺陷核验：`Docs\My_ARPG_MVCS项目现状.md:53` 与 `:82` 两处标题均为 `### 1.5`，`:62` 的 `### 1.4` 夹在其中——标题序号为 1.1/1.2/1.3/1.5/1.4/1.5，重复与错序均属实。
- Addressables 包测试复算：`Library\PackageCache\com.unity.addressables@1.22.3\Tests\` 209 个 .cs 含 991 个 `[Test]`，与旧文档 `:183` 所称"包自带 1 个"不符（该包测试 asmdef 是否启用未核验）。

**未核验项（已全部写入 Guide §5，标注为「假设」）**

- 52 个 `[Test]` 是否全绿、EditMode 无失败。
- 无 asmdef 的 `Assets/Tests/Editor` 是否仍被 Test Runner 的 EditMode 平台发现。
- `Pipeline → Gameplay` 是否存在 `using` 之外的隐式反向依赖（反射 / 字符串 / Addressables 资产路径）。
- `Assembly-CSharp` 是否为唯一产品程序集（未读 `ProjectSettings/` 与 `Packages/manifest.json` 交叉确认）。
- `StatsService` 的 `statsConfig` 是否在所有场景完成接线。
- `Gameplay/Player` 下 6 个全局命名空间类（移动/战斗/弓箭线）是否确实未分层。

**发现的缺陷（已写入 Guide §4）**

- D1 `Pipeline\SO\PlayerStatsSO.cs:2` 是管线层唯一反向依赖 `Gameplay` 的点。
- D2 `PlayerStatsSO.Data`（`PlayerStatsSO.cs:14`）返回资产内部引用而非拷贝，且无测试约束。
- D3 `Contracts\ISaveable.cs:7-14` 的默认注册实现与 `SaveableService` 的注册路径并存，两条入口无约定。
- D4 旧文档 `Docs\My_ARPG_MVCS项目现状.md:174,183` 测试数据过期：工程侧 46/47 应为 52（漏记 `AStarOpenHeapTests`），包侧"1 个"与实测 991 个不符。
- D5 旧文档 §1.5 标题重复（`:53`、`:82`）且 1.4 错序，排版缺陷属实。
- D6 `ICanvasManager` 拼写不一致（`Canva`/`Canvas`），`StatsPanelView.cs:61,67,73` 依赖显式转型调用。
- D7 `ICanvasManager` 默认方法体内直接调 `UIManager.Report` / `UIManager.Instance`（`ICanvasManager.cs:75,90-93`），契约层隐式绑定管线层具体单例。

**维护计数**：`0/5`（未变；本次为首次建档）
