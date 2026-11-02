# WorkingAgent Registry

## 维护规则

本文件对应的实现、约束或规则发生变化时，按已核验事实更新相关章节，证据记录在[开发日志](../Workflow/DeveloperLog.md)。修改小标题时同步文档内目录；计数与维护周期遵循[工作流维护规则](../Workflow/Workflow_Guide.md#维护规则)。

This directory stores temporary JSON lease markers named `<AgentName>_<Hash>.txt`.
Only this README and `.gitignore` are versioned. Agents must use
`..\Workflow\Scripts\WorkingAgent.ps1` for registry operations.

Markers older than 15 minutes without a heartbeat are only suspected stale. They are
never deleted or taken over automatically.

In Codex, native task messages may point agents at an overlapping lease or announce
that a range is free. Messages do not replace this registry: verify the lease and
the actual workspace before editing, and release the lease before announcing
completion.
