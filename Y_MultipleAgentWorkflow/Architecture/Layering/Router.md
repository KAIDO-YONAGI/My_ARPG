# Architecture.Layering Router

## 维护规则

任务路由、权威文档或职责边界变化时更新本文，详细文档索引在所属功能域 Router 就近维护。证据记录在[开发日志](DeveloperLog.md)；计数与维护周期遵循[工作流维护规则](../../Workflow/Workflow_Guide.md#维护规则)。

文档 I:：`BUS-ARCHITECTURE-LAYERING`
状态：`Active`
维护计数：`2/5`
最后更新：`2026-11-07`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `分层` `Layering` `三层目录` | `Layering_Guide.md` |
| `Contracts` `Gameplay` `Pipeline` | `Layering_Guide.md` |
| `依赖方向` `反向依赖` `跨层引用` | `Layering_Guide.md` |
| `asmdef` `程序集边界` `Assembly-CSharp` | `Layering_Guide.md` |
| `命名空间` `全局命名空间` | `Layering_Guide.md` |
| `MVCS` `四层` `唯一写入口` `只读接口` | `Layering_Guide.md` |
| `StatsService` `IPlayerStatsReadOnly` 读写约定 | `Layering_Guide.md` |
| `测试用例数` `[Test]` 统计 | `Layering_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Architecture.Layering`
- `path:Y_MultipleAgentWorkflow\Architecture\Layering\`
- 契约层脚本目录
- 玩家数值四层目录
- 编辑器测试目录，只读

## 能力边界

**Active**

- 三层目录的判定标准；跨层依赖方向按 `using` 实测：契约层反向依赖 6 处（`Contracts → Gameplay` 2、`Contracts → Pipeline` 4），玩法域到管线层 65 处，管线层到玩法域 6 处，管线层到契约层 41 处，玩法域到契约层 47 处，见 §2.2。
- 玩家数值线 MVCS 四层的层归属、命名空间与实例化方式。
- 命名空间分布：31 个命名空间覆盖全部 140 个产品代码文件，命名空间一律等于目录路径，全局命名空间只剩 1 个编辑器脚本。
- "唯一写入口 + 只读接口"的可执行判定标准：读经 `StatsService.Instance.Stats`，写经 6 个 Service 命令，Model 私有。
- 测试基线：编辑器侧 6 个测试文件、61 项用例，命名空间 `Gameplay.Tests`，测试侧对产品类型的引用靠 `using Contracts` / `using Pipeline.*`。

**约束**

- 新 asmdef 属 `Architecture.AssemblyPlan` 域，不在本域授权范围内。
