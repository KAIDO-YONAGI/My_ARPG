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
