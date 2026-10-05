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
- `path:Assets\Scripts\Contracts\`
- `path:Assets\Scripts\Gameplay\Player\{Models,Services,Controllers,Views}\`
- `path:Assets\Tests\Editor\`（只读核验，不写入）

## 能力边界

**Active（已核验、可直接依据）**

- 三层目录的判定标准、跨层依赖方向实测结论（`Contracts` 反向依赖 0 处；`Gameplay → Pipeline` 0 处；`Pipeline → Gameplay` 1 处，位于 `Pipeline\SO\PlayerStatsSO.cs:2`）。
- 玩家数值线 MVCS 四层的层归属、命名空间与实例化方式。
- "唯一写入口 + 只读接口"的可执行判定标准（读经 `StatsService.Instance.Stats`，写经 6 个 Service 命令，Model 私有）。
- 测试基线：`Assets/Tests/Editor` 5 文件 52 个 `[Test]`。
- 旧文档缺陷核验：测试数据过期（46→52）、§1.5 标题重复（属实）。

**Proposal（记录、未动工）**

- 消除 `Pipeline → Gameplay` 唯一反向依赖：把 `PlayerStatsData` 的引用点改为契约层传输接口或让其留在 `Gameplay` 侧。
- 补齐 `Contracts/ISaveable.cs` 两条注册入口（默认实现 vs `SaveableService`）的调用约定。

**需要用户确认的事项**

- 是否为 `Assets/Scripts` 引入程序集边界（新 asmdef 属 `Architecture.AssemblyPlan` 域，不在本域授权范围内）。
- 是否把 `Pipeline/*` 与 `Gameplay/*` 的 111 个全局命名空间类逐步收进命名空间——工作量按文件计，且会大面积改动引用。
- 旧文档 `Docs\My_ARPG_MVCS项目现状.md` 的 §1.5 重复标题与 §3.1 测试数据是否授权直接修订（本域无 `Docs/**` 写权限）。
