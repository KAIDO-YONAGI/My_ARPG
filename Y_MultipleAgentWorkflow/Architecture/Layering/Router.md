# Architecture.Layering Router

文档 ID：`BUS-ARCHITECTURE-LAYERING`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

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

- 三层目录的判定标准；跨层依赖方向：契约层反向依赖 0 处，玩法域到管线层 0 处，管线层到玩法域 1 处，位于 `PlayerStatsSO`。
- 玩家数值线 MVCS 四层的层归属、命名空间与实例化方式。
- 管线层与玩法层的全局命名空间类共 111 个。
- "唯一写入口 + 只读接口"的可执行判定标准：读经 `StatsService.Instance.Stats`，写经 6 个 Service 命令，Model 私有。
- 测试基线：编辑器侧 5 个测试文件、52 个 `[Test]`。

**约束**

- 新 asmdef 属 `Architecture.AssemblyPlan` 域，不在本域授权范围内。
