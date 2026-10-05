# AGENTS.md

<!-- Y_MultipleAgentWorkflow:BEGIN — 由工作流维护的导航块，块内请勿手工改动 -->

## 项目文档入口

本工程的权威文档、业务路由与并发规则统一收在 `Y_MultipleAgentWorkflow/`。

开工顺序：

1. 先读 `Y_MultipleAgentWorkflow/Router.md`：它包含必读起点、7 个业务根的业务路由表、
   可被结构校验逐条解析的文档索引，以及全局并发资源清单。
2. 按路由读对应业务根的 `Router.md`，再读其中的 Guide / Design。
3. 需要修改文件、启动子 Agent 或使用会改变状态的工具**之前**，按
   `Y_MultipleAgentWorkflow/Workflow/Concurrency_Guide.md` 取得 WorkingAgent 租约；
   成功、失败或取消都要释放。

文档与实际代码、资产冲突时，**以当前工作区为准**，并立即修正文档；发现无法协调的
冲突时报告用户，不得静默覆盖对方的改动。

<!-- Y_MultipleAgentWorkflow:END -->

> 本导航块同时存在于 `CLAUDE.md`，内容一致；块外内容各自独立。
