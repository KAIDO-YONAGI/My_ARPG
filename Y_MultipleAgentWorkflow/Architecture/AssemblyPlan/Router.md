# Architecture.AssemblyPlan Router

文档 ID：`BUS-ARCHITECTURE-ASSEMBLYPLAN`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `asmdef` `Assembly Definition` `程序集拆分` `编译时间` | `AssemblyPlan_Proposal.md`（§3.1） |
| `测试 asmdef` `编辑器测试程序集引用` | `AssemblyPlan_Proposal.md`（§3.1 前置依赖） |
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

## 能力边界

**Active**

- 本域暂无 Active 权威文档。唯一文档 `AssemblyPlan_Proposal.md` 状态为 `Proposal`。
- 可依据 Router 完成路由与线索定位；`AssemblyPlan_Proposal.md` 的条目按提案记录引用。

**Proposal：记录项，尚未实施**

- `AssemblyPlan_Proposal.md` 记录五项未实施设计：`Assembly Definition` 拆分、xLua 热更 MVP、技能与物品效果数据驱动加状态机基类、事件总线维持不采用、事件引用可视化插件。
