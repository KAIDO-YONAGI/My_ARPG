# Architecture.AssemblyPlan Router

文档 ID：`BUS-ARCHITECTURE-ASSEMBLYPLAN`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `asmdef` `Assembly Definition` `程序集拆分` `编译时间` | `AssemblyPlan_Proposal.md`（§3.1） |
| `测试 asmdef` `Assets/Tests/Editor` 程序集引用 | `AssemblyPlan_Proposal.md`（§3.1 前置依赖） |
| `xLua` `热更` `LuaManager` `LuaScripts` `Addressables` 下发 | `AssemblyPlan_Proposal.md`（§3.2） |
| `效果数据驱动` `技能效果` `物品效果` `switch(skillName)` | `AssemblyPlan_Proposal.md`（§3.3） |
| `状态机基类` `PlayerState` `EnemyState` `NPCState` | `AssemblyPlan_Proposal.md`（§3.3 前置依赖） |
| `事件总线` `EventBus` `12 条通道` `维持不采用` | `AssemblyPlan_Proposal.md`（§3.4） |
| `事件引用可视化` `订阅链查看器` `接线图` `EventGraph` | `AssemblyPlan_Proposal.md`（§3.5） |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Architecture.AssemblyPlan`
- `path:Y_MultipleAgentWorkflow\Architecture\AssemblyPlan\`

本域当前**不声明任何 `Assets/**` 写资源**：域内全部内容为未实施设计，任何落地方案（新增 asmdef、导入 xLua、新增 `Assets/Editor/EventGraph/`）都必须先经用户确认并重新声明资源，再路由到相应业务域。

## 能力边界

**Active**

- 本域暂无 Active 权威文档。唯一文档 `AssemblyPlan_Proposal.md`（`ARCH-ASSEMBLY-PLAN`）状态为 `Proposal`。
- 可依据 Router 完成路由与线索定位，但不得把 `AssemblyPlan_Proposal.md` 的任何条目当作现行规则或已实现能力引用。

**Proposal（记录、未实施，不得冒充当前能力）**

- `AssemblyPlan_Proposal.md` 记录五项未实施设计：`Assembly Definition` 拆分、xLua 热更 MVP、技能/物品效果数据驱动 + 状态机基类、事件总线（**维持不采用**）、事件引用可视化插件。
- 该文档同时记录了五项上游线索文档与代码不符之处（失效路径、过期计数、指向不存在类的引用），复核前必须先按源码重跑检索。

**需要用户确认的事项**

- 五项设计均无排期承诺；`Docs/My_ARPG_重构优化清单_未解决.md` 的「⏸ 已排期 / 留存」标记是否仍代表当前意愿需用户确认。
- 程序集拆分的第一步（消除 `Contracts → UI` 反向引用，见 `Architecture\Composition\Composition_Guide.md` §2.6）会改动 `Assets/Scripts/**`，超出本域授权，需用户确认后路由到对应业务域。
- xLua 接入涉及 `Assets/xLua/`、`Assets/Plugins/` native 插件与 Addressables 分组，均需用户确认后再声明可写路径。
- 状态机基类要先决策「三套状态枚举（`MyEnums.PlayerState` / `EnemyState` / `NPCState`）统到什么粒度」，属架构决策，需用户确认。
