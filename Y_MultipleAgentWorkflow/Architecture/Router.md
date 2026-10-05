# Architecture Router

文档 ID：`BUS-ARCHITECTURE`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `分层` `Layering` `三层目录` `依赖方向` `反向依赖` | `Layering\Layering_Guide.md` |
| `命名空间` `全局命名空间` `MVCS` `只读接口` | `Layering\Layering_Guide.md` |
| `单例` `YSingleton` `单例预算` `Instance 为 null` | `Composition\Composition_Guide.md` |
| `初始化顺序` `OnSingletonInitialized` `可重试订阅` | `Composition\Composition_Guide.md` |
| `isLoading` `IsLoadingSaveRequest` `DefaultExecutionOrder` | `Composition\Composition_Guide.md` |
| `SaveRegistry` `Domain Reload` `局间复位` | `Composition\Composition_Guide.md` |
| `ICanvasManager` `焦点栈` `sortingOrder` `画布契约` | `Composition\Composition_Guide.md` |
| `asmdef` `程序集拆分` `xLua` `热更` `事件总线` `事件可视化` | `AssemblyPlan\AssemblyPlan_Proposal.md`（状态 `Proposal`，未实施） |

## 下级导航

| 子类 | Router |
|---|---|
| `AssemblyPlan` | `AssemblyPlan\Router.md` |
| `Composition` | `Composition\Router.md` |
| `Layering` | `Layering\Router.md` |

## 并发资源

- `workflow:Architecture`
- `path:Y_MultipleAgentWorkflow\Architecture\`

本域是**文档域**，不直接持有 `Assets/**` 写资源：任何落地方案都须先路由到下级 Router 并按其声明的资源取租约。

## 能力边界

**Active**

- `Architecture\Composition\Composition_Guide.md`（`ARCH-COMPOSITION-GUIDE`）：单例清单与预算（19 个具体单例）、初始化时序三套手写解法、全工程唯一 `DefaultExecutionOrder(10000)` 的真实语义、`SaveRegistry` 登记处契约、UI 画布焦点栈契约。
- `Architecture\Layering\Layering_Guide.md`：三层目录判定标准、跨层依赖方向实测结论、MVCS 四层归属与命名空间现状。（细节以该域 Router 与 Guide 为准，本文件不复制。）
- `Architecture\Composition\Router.md`、`Architecture\AssemblyPlan\Router.md`、`Architecture\Layering\Router.md`：三个下级域的线索路由入口。

**Proposal（未实施，不得冒充当前能力）**

- `Architecture\AssemblyPlan\AssemblyPlan_Proposal.md`（`ARCH-ASSEMBLY-PLAN`，状态 `Proposal`）：`Assembly Definition` 拆分、xLua 热更 MVP、技能/物品效果数据驱动 + 状态机基类、事件总线（维持不采用）、事件引用可视化插件五项未实施设计，附目标/理由/当前状态/前置依赖。
- 该文档同时记录了上游线索文档与代码不符之处（失效路径、过期计数、指向不存在类的引用）；引用前必须按源码复核。

**需要用户确认的事项**

- 是否把 `Architecture` 下三个子域的单例/依赖收敛动作（合并超预算单例、消除 `Contracts → UI` 反向引用、引入 asmdef）立项——均属架构决策，且会改动 `Assets/Scripts/**`，超出本域授权。
- 是否授权修订 `Docs\My_ARPG_MVCS项目现状.md` 中已过期的事实条目（本域无 `Docs/**` 写权限）。
- 上游排期文档 `Docs/My_ARPG_重构优化清单_未解决.md` 已由用户在 `2026-10-05` 删除（内容见 `git HEAD:Docs/My_ARPG_重构优化清单_未解决.md`），其中「已排期」标记不构成立项承诺，是否推进需用户重新确认。
