# Architecture.Composition Router

文档 ID：`BUS-ARCHITECTURE-COMPOSITION`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `单例`、`YSingleton`、`Instance`、`单例预算` | `Composition_Guide.md`（§2.1/§2.2） |
| `初始化顺序`、`Awake 顺序`、`Instance 为 null`、`OnSingletonInitialized` | `Composition_Guide.md`（§2.3） |
| `可重试订阅`、`订阅空引用`、`isLoading`、`IsLoadingSaveRequest` | `Composition_Guide.md`（§2.3.2/§2.3.3） |
| `执行顺序`、`DefaultExecutionOrder`、`CameraPixelSnap` | `Composition_Guide.md`（§2.4） |
| `SaveRegistry`、`注册表`、`Domain Reload`、`局间复位`、`ISaveable` | `Composition_Guide.md`（§2.5） |
| `ICanvasManager`、`焦点栈`、`CanvasFocusStack`、`sortingOrder` | `Composition_Guide.md`（§2.6） |
| `新增单例吗`、`能不能加个 Manager`、`接线约定` | `Composition_Guide.md`（§3） |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Architecture.Composition`
- `path:Y_MultipleAgentWorkflow\Architecture\Composition\`
- 契约脚本目录与系统画布管理脚本目录以只读证据源身份登记；写入这两处需 Gameplay 域与资产域授权。

## 能力边界

Active 能力：

- `Composition_Guide.md`（`ARCH-COMPOSITION-GUIDE`）是本域唯一权威文档，覆盖单例清单与预算、初始化时序三套解法、唯一执行序属性语义、`SaveRegistry` 登记处契约、画布焦点栈契约。
- 可据此判定「新增单例是否超预算」「跨单例协作该用哪种时序兜底」「画布契约是否被违反」。

Proposal：

- `Architecture\AssemblyPlan\AssemblyPlan_Proposal.md`（`ARCH-ASSEMBLY-PLAN`）涉及程序集拆分前后的组合边界，尚未实施，按提案记录引用。

需要用户确认的事项：

- 把 19 个具体单例收敛回 6 个合规项（合并/下沉历史三件套与寻路三件套）属于架构决策，需用户确认后才可立项。
- 为 `SaveRegistry.Clear()` 建立局间复位调用点会改动重开新局流程，需用户确认时机。
- 本域 Router 不含脚本目录的写入授权；如需修改被本域描述的源码，须路由到对应业务域的 Router。
