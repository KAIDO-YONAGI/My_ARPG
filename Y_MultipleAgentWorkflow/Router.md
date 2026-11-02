# My_ARPG 多 Agent 工作流路由器

文档 ID：`ROOT-ROUTER`  
状态：`Active`  
最后更新：`2026-10-05`

本目录是本工程的权威文档入口。Agent 可以做轻量只读盘点；详细分析、启动子 Agent、修改
文件或使用会改变状态的工具之前，先用 `Workflow\Scripts\WorkingAgent.ps1` 取得精确租约。

## 权威顺序

1. 用户最新的明确要求。
2. 实际代码、资源、运行结果与可复现测试。
3. 相关 Guide / Design。
4. 相关业务 Router。
5. DeveloperLog 与 Proposal。
6. 模型入口文件与 Skill。

第 2 项与本目录文档冲突时，以当前工作区的代码与资源为准，并立即修正文档。

## 必须先读的规则

**`RULES.md`（文档 ID `RULES`）是本工程的协作规则，位于本文件同目录。** 开始任何分析、
写作或改代码之前先读它，并在全过程遵守。它约束 `Y_MultipleAgentWorkflow/` 下全部 Guide、
Router、DeveloperLog，以及项目根的 `AGENTS.md` 与 `CLAUDE.md`。

本文件与各业务 Router 只负责"知识在哪"，规则内容一律引用 `RULES.md`，本文件不复制。

## 业务路由

| 业务根 | Router |
|---|---|
| `Architecture` | `Architecture\Router.md` |
| `Assets` | `Assets\Router.md` |
| `Build` | `Build\Router.md` |
| `Data` | `Data\Router.md` |
| `Gameplay` | `Gameplay\Router.md` |
| `SceneFlow` | `SceneFlow\Router.md` |
| `Workflow` | `Workflow\Router.md` |

## 必读起点

1. 先读 `RULES.md`，全程遵守本工程的协作规则。
2. 经本文件路由到对应业务根。
3. 读 `Workflow\Concurrency_Guide.md`。
4. 用 `Workflow\Scripts\WorkingAgent.ps1` 取得精确租约。
5. 租约状态显示重叠任务、且客户端支持原生 Agent 通信时，编辑前先通知该任务。
6. 只读被路由到的 Guide/Design、Router 和必要证据。
7. 成功、失败或取消都要释放租约；客户端支持原生通信时，释放后再通知等待中的任务。

## 项目验证

项目专属验证统一走 `Workflow\Project_Validation_Guide.md`。该指南登记本工程已确认的命令
与状态变更，未确认的条目留空。

## 模型入口

`..\AGENTS.md` 与 `..\CLAUDE.md` 各含一个由本工作流维护的导航块，用
`<!-- Y_MultipleAgentWorkflow:BEGIN -->` 与 `<!-- Y_MultipleAgentWorkflow:END -->` 标记
包住，两块内容一致，可按标记幂等更新。两个文件都会被客户端装载为工作区指令。

块内只放指向本文件的指针，业务事实一律留在本目录。

## 全局并发资源

同一工作树上的多个 Agent 共用以下资源。写入前在租约里声明：文件类资源用 `path:` 前缀加
该资源的实际路径，其余按 `Workflow\Concurrency_Guide.md` 的命名规则声明。

| 资源 | 说明 |
|---|---|
| `git:index` | 共享暂存区；`push_to_github.bat` 会执行全量 `git add -A` 并推送 master |
| `runtime:UnityEditor` | 本机同时只应有一个编辑器实例；陈旧的锁文件不代表实例存活 |
| `workflow:root` | 仅在变更根结构时使用 |
| `codegraph:index` | codegraph 的索引库，重建需独占 |
| 产品代码 | 全部产品代码在一个程序集内，任一改动触发全量重编译 |
| 配置与事件资产 | 配置资产与事件通道；资产改名会让 Inspector 接线静默失效 |
| 场景 | 多场景组叠加加载，常驻场景与游戏场景是热点 |
| 预制体 | 与场景接线耦合 |
| 包清单与锁文件 | 成对变更，是依赖解析的单点 |
| 工程设置 | Unity 版本、构建场景列表、脚本后端等工程级配置 |
| 构建与日志输出 | 已被 gitignore 的产物目录 |

## 

## 参考资料

工程现状、Android 构建与开发历史等资料存放在项目 `Docs\` 目录，按名称检索。

## 维护

成功且实质影响某个业务根的任务，会让该业务根的计数 +1。到 `5/5` 时，对照任务证据与
工程实际状态复核，更新权威文档或记录 `reviewed-no-change`，然后清零。

不要自动截断 `DeveloperLog.md` 历史；只有用户明确确认某条目冗余或过时后才能删除。
