# 根工作流开发日志

本文件记录跨业务的结构变更与根索引维护。业务证据写进对应业务根的 DeveloperLog。

## 2026-10-05：工作流初始化

- 按 `multiple-agent-workflow-config` 方法初始化根结构：根 Router、工作流指南、
  WorkingAgent 租约注册表（`WorkingAgent\`）与 7 个业务根。
- 业务根：`Workflow`、`Architecture`、`Gameplay`、`SceneFlow`、`Assets`、`Data`、`Build`；
  二级：`Architecture.{Layering,Composition,AssemblyPlan}`、
  `Gameplay.{PlayerStats,Units,Dialog,Quest,InventoryShop,Skills}`。
- 分类依据来自只读盘点（5 路并行调查 + 3 路补齐），不是按源码目录形状切分。
- 模型入口未修改（`EntryMode=None`）；全仓本就不存在 `AGENTS.md` / `CLAUDE.md` /
  项目级 Skill，入口职责由根 `Router.md` 的“必读起点”承担。
- 项目验证模式选 `Guide`，落地在 `Workflow\Project_Validation_Guide.md`。

## 2026-10-05：权威文档库建立

- 10 路写手并行产出 16 份域权威文档（7 个一级域），路径互不相交，全程中文：
  `Architecture` 3 份（Layering / Composition / AssemblyPlan-Proposal）、
  `Gameplay` 6 份、`SceneFlow` 1 份、`Assets` 2 份、`Data` 1 份、`Build` 3 份。
- 每份业务 Router 按项目中文模板重写，保留父级所需的下级导航行。
- 根 Router 建立可解析的文档索引（41 条），结构校验逐条核对索引路径。

## 2026-10-05：并发冲突记录

- 取得父租约 `ec977814-b875-4904-a0bb-e62172c1c5b9`（`DSH-Main`，
  AccessMode `write`，覆盖 7 个业务根与 `README.md` / `README.en.md` / `Docs`）。
- 作业期间观测到**不入租约注册表**的并发写入：工作区里出现用户本人的未提交改动
  （`SceneChanger` 移除重试订阅与 `Respawn()`、`PlayerDamageController` 接管重试复活、
  `SaveDataManager` 回灌跳过 `GetDataID()` 为空的固定槽位服务），以及
  `Docs/README.md`、`Docs/My_ARPG_重构优化清单_已解决.md`、
  `Docs/My_ARPG_重构优化清单_未解决.md` 三个跟踪文件被删除。
- 处置：用户确认上述改动均为其本人所做，不回退；三个删除保持删除；
  库内写入照常，共享路径的修复按工作区现状重新落回。
- 结论：租约注册表对“不入表的外部写入”没有约束力。权威顺序第 2 条（以当前工作区为准）
  是这类情况下的实际兜底；发现冲突时必须报告用户，不得静默覆盖。
