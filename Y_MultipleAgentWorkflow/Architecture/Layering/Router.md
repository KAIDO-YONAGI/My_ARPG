# Architecture.Layering Router

文档 ID：`BUS-ARCHITECTURE-LAYERING`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-11-01`

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

- 三层目录的判定标准；跨层依赖方向按 `using` 实测：契约层反向依赖 9 处（`Contracts → Gameplay` 2、`Contracts → Pipeline` 7），玩法域到管线层 66 处，管线层到玩法域 6 处，管线层到契约层 26 处，玩法域到契约层 32 处，见 §2.2。
- 玩家数值线 MVCS 四层的层归属、命名空间与实例化方式。
- 命名空间分布：26 个命名空间覆盖全部 133 个产品代码文件，命名空间一律等于目录路径，全局命名空间只剩 1 个编辑器脚本。
- "唯一写入口 + 只读接口"的可执行判定标准：读经 `StatsService.Instance.Stats`，写经 6 个 Service 命令，Model 私有。
- 测试基线：编辑器侧 6 个测试文件、61 项用例，命名空间 `Gameplay.Tests`，测试侧对产品类型的引用靠 `using Contracts` / `using Pipeline.*`。

**约束**

- 新 asmdef 属 `Architecture.AssemblyPlan` 域，不在本域授权范围内。
