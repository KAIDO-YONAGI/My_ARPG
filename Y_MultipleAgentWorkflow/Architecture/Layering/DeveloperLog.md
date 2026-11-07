# Architecture.Layering Developer Log

## 维护规则

按项目约定记录带日期的任务事实、验证证据与维护计数，现行结论同步到对应 Guide。了解现状优先读取 Guide；需要任务历史时，只定位并读取最近一天或两天的相关条目，禁止整份日志载入上下文。只有任务需要追溯历史时才按日期、标题或关键词定向读取更早条目。读取范围不改变存储保留，删除具体历史条目须经用户明确确认。纯文档维护不增加业务计数。

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-07 状态版本与视图读取记录分离

`Contracts.State.VersionedValue<T>` 保存值与 ulong 版本，`VersionReader` 作为每个视图的 struct 字段保存数据源身份、已绘制版本与首次读取标志。绘制前捕获版本、成功后记录，数据源替换和显式失效均触发刷新；集合原地修改通过 `Touch` 发布。

背包初始化与点击传递槽位值和地址，技能升级传递键与数值，两个管理器的运行代码均不再引用槽位组件类型。`VersionedValueTests` 的 8 项用例通过，其中预热后 10000 次版本读写与记录的线程托管分配为 0。全部 EditMode 80/80 通过，其中项目 79 项；XML 为 `Logs/ui-version-editmode.xml`。按当前源码统计 141 个产品文件，Contracts/Pipeline/Gameplay 为 13/66/62，并同步依赖与测试表。维护计数 `1/5 → 2/5`。

## 2026-11-02 音频契约与管线分层

音频枚举和请求放入 Contracts.Audio，播放器、配置与触发器放入 Pipeline.Audio，请求事件放入 Pipeline.SO.Events。播放器使用本工程的 Contracts.Base.YSingleton，核心代码不依赖原项目网络与角色输入。AudioType 使用类型别名明确区分 Contracts 与 UnityEngine 的同名枚举。

以当前 Assets/Scripts 统计产品代码 139 个、命名空间 31 个，Contracts、Pipeline、Gameplay 分别为 12、65、62 个文件。按非别名 using 指令复核依赖统计并同步 Guide。仅做静态文件与引用复核，未运行测试。维护计数 0/5 → 1/5。

## 2026-11-01 刷新边界改为 State 只读查询

五个 Gameplay 管理器通过稳定的 `State` 提供字段或索引查询；`DirtyValue<T>` 不再生成快照包装，集合查询不复制数据。维护计数达到 `5/5`，复核实现并更新 Layering Guide 后清零为 `0/5`。Unity Editor 刷新完成，控制台返回 0 条错误。

## 2026-11-01 Contracts 刷新边界改为只读快照

分层说明更正 `DirtyValue<T>` 为管理器内部变更跟踪，并登记 `DirtySnapshot<T>` 作为视图读取的只读版本快照；数据管理器不再向视图暴露可变 `State`。维护计数更新为 `3/5 -> 4/5`。

## 2026-10-31 面板迁出后复核分层与命名空间

- 文件数：`Pipeline` 59 → 62、`Gameplay` 59 → 65，产品代码合计 126 → 135；`Pipeline.UI.Runtime` 2 → 4、`Pipeline.SO.Events` 12 → 13，命名空间仍为 26 个。
- 跨层 `using`：`Gameplay → Pipeline` 62 → 75，`Pipeline → Contracts` 22 → 27，其余方向不变。
- §2.3 增加五域「数据管理器 + 缓存界面视图」的配对说明，删除「其余域是整体 Manager 形态」的旧判断。

## 2026-10-31 属性面板预制体与闲置缓存

新增 Pipeline.UI.Runtime，实测产品代码 126 个、命名空间 26 个；Gameplay → Pipeline 为 64 处，Pipeline → Contracts 为 24 处。

Unity 重编译及预制体引用检查完成；未运行测试或进入 Play 模式，复用与多界面淘汰仍待运行时验证。维护计数更新为 `2/5`。

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

- 跨层依赖按 `using` 指令统计，机制与数字见 Guide §2.2。
- §3.4 从"已满足的约束"改记为"违反 5 处的待偿债务"，因为契约层现在 `using` 了 `Gameplay.Save`、`Pipeline.SO.Events`、`Pipeline.UI.SystemCanvasManagers`、`Pipeline.SO`。

**维护计数**：`0/5` → `1/5`。

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
