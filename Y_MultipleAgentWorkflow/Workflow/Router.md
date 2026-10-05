# Workflow Router

文档 ID：`WF-ROUTER`  
状态：`Active`  
维护计数：`0/5`  
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| 路由、分类增殖、目录深度、维护计数 | `Workflow_Guide.md` |
| 租约、冲突、等待、覆盖、心跳 | `Concurrency_Guide.md` |
| 新建业务根 | `Templates\BusinessRouter.template.md` |
| 租约操作 | `Scripts\WorkingAgent.ps1` |
| 租约回归测试 | `Scripts\Test-WorkingAgent.ps1` |
| 本工程可执行的验证命令 | `Project_Validation_Guide.md` |
| 已确证但需用户拍板的决策点 | `OpenDecisions_Proposal.md` |
| 通用配置方法（跨项目） | `..\Workflow_Configuration_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:root`（仅根结构变更）
- `path:Y_MultipleAgentWorkflow\Workflow`
- `path:Y_MultipleAgentWorkflow\WorkingAgent`

## 能力边界

- Active：7 个业务根的路由与维护规则、WorkingAgent 租约协议（`Acquire` / `Heartbeat` /
  `UpdateScope` / `Wait` / `Release` / `Status`）、结构与索引校验脚本。
- 需要用户确认：新增顶级分类、迁移或删除既有权威、改变工作区级规则、修改模型入口文件。
- 不负责：业务事实本身由各业务根的 Guide/Design 拥有，本域只拥有流程与结构规则。
