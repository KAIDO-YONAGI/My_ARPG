# 根工作流开发日志

本文件记录跨业务的结构变更与根索引维护。业务证据写进对应业务根的 DeveloperLog。

## 2026-10-05：工作流初始化

- 按 `multiple-agent-workflow-config` 方法初始化根结构：根 Router、工作流指南、
  WorkingAgent 租约注册表（`WorkingAgent\`）与 7 个业务根。
- 业务根：`Workflow`、`Architecture`、`Gameplay`、`SceneFlow`、`Assets`、`Data`、`Build`；
  二级：`Architecture.{Layering,Composition,AssemblyPlan}`、
  `Gameplay.{PlayerStats,Units,Dialog,Quest,InventoryShop,Skills}`。
- 分类依据来自只读盘点（5 路并行调查 + 3 路补齐），不是按源码目录形状切分。
- 模型入口初始化时未修改（`EntryMode=None`）；全仓本就不存在 `..\AGENTS.md` / `..\CLAUDE.md` /
  项目级 Skill，入口职责暂由根 `Router.md` 的“必读起点”承担。
- 项目验证模式选 `Guide`，落地在 `Workflow\Project_Validation_Guide.md`。

## 2026-10-31：命名空间重构后的文档收口

- 触发：用户把 `Assets\Scripts` 下全部 124 个产品代码文件收进与目录同构的命名空间，并
  把原 `MyEnums` 类拆成 10 个顶层枚举，随后要求整理文档并提交一次。
- 代码侧补漏：`Assets\Tests\Editor` 的 5 个测试文件未同步命名空间，Unity 报 6 个
  `CS0246`。只补 `using`，不改用例逻辑——`CanvasFocusStackTests` 加 `using Contracts;`
  与 `using Pipeline.UI.SystemCanvasManagers;`，`AStarOpenHeapTests` 加
  `using Pipeline.Pathfinding;`，`ObjectPoolTests` 加 `using Pipeline;`，
  `PlayerStatsSOTests` 加 `using Pipeline.SO;`。
- 文档改动集中在 `Architecture\Layering`：`Layering_Guide.md` 的 §2.1、§2.2、§2.3、
  §2.5、§3.2、§3.4 与线索表按实测重写，`Router.md` 计数 0/5 → 1/5 并改能力边界。
  细节与证据见 `Architecture\Layering\DeveloperLog.md` 同日条目。
- 其余业务根以"枚举去前缀"和"命名空间归属"两类事实修正为主：`Assets\Assets_Guide.md`、
  `Assets\EventChannels_Guide.md`、`SceneFlow\SceneFlow_Guide.md`、
  `Gameplay\Dialog\Dialog_Guide.md`、`Gameplay\Quest\Quest_Guide.md`、
  `Gameplay\Units\Units_Guide.md` 与其 `Router.md`、`Gameplay\PlayerStats\PlayerStats_Guide.md`、
  `Build\TestBaseline_Guide.md`、`Architecture\AssemblyPlan\AssemblyPlan_Proposal.md`。
- 各业务根计数：`Architecture` 0/5 → 1/5，`Gameplay`、`Assets`、`Build`、`SceneFlow` 各
  0/5 → 1/5；二级 `Architecture.Layering`、`Gameplay.Units`、`Gameplay.PlayerStats`、
  `Gameplay.Quest`、`Gameplay.Dialog` 各 0/5 → 1/5。
- 未改历史：各业务根的旧 `DeveloperLog.md` 条目按当时事实保留，不回溯改写；本轮只追加。
- 验证：Roslyn `csc.dll` 配 Unity 2022.3.62f3c1 的 286 条程序集引用编译 130 个源文件，
  0 error 0 warning。未启动 Unity 编辑器，未跑 EditMode 用例。
