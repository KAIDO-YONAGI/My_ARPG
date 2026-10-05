# Gameplay.Units Router

文档 ID：`BUS-GAMEPLAY-UNITS`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `敌人` / `Enemy` / `怪物` / `TorchGoblin` / 状态机 `Idle/Chasing/Attacking/KnockBack` | `Units_Guide.md` §2.1 §2.2 |
| `敌人攻击` / `EnemyCombat` / `Attack()` / `weaponRange` / `attackPoint` | `Units_Guide.md` §2.2 |
| `敌人血量` / `EnemyHealth` / `IDamageable` / `TakeDamage` / `expReward` / 死亡 | `Units_Guide.md` §2.2 |
| `击退` / `EnemyKnockBack` / `knockBackForce` / `stunTime` / `knockBackTime` / 硬直 | `Units_Guide.md` §2.2 §3.4 |
| `NPC` / `NPCStateController` / `SwitchState` / `enabled` 互斥 | `Units_Guide.md` §2.3 §3.6 |
| `巡逻` / `NPCPatrol` / `patrolSize` / `clockwise` / 矩形路线 | `Units_Guide.md` §2.3 |
| `游荡` / `NPCWander` / `patrolRadius` / `randomDirection` | `Units_Guide.md` §2.3 |
| `对话触发` / `NPCDialogTrigger` / `Chat` / `advanceDialogAction` / 交互图标 | `Units_Guide.md` §2.3 §3.7 |
| `店主` / `ShopKeeper` / `ShopKeeperEventSO` / 进出触发范围 / `PortraitTarget` | `Units_Guide.md` §2.4 §3.8 |
| `商品列表` / `shopItems` / `shopWeapon` / `shopArmor` / 场景实例覆写 | `Units_Guide.md` §2.4 §3.9，结算归 InventoryShop |
| `PathFollower` / `GetPosToGo` / `ArrivedPos` / `ResetPath` / `GetThreshold` | `Units_Guide.md` §2.5 §3.2 |
| `Awake` / `OnEnable` / `isKinematic` / 组件时序 | `Units_Guide.md` §2.3 §3.7 |
| `没有统一基类` / 单位脚本清单 / 职责划分 | `Units_Guide.md` §2.0 |

> 本域无下级 Router，属单层业务域，全部权威内容在 `Units_Guide.md`；寻路算法本身归寻路模块，商店买卖归 InventoryShop，对话推进归 Dialog，玩家数值归 PlayerStats。

## 下级导航

| 子类 | Router |
|---|---|
| None | None |

## 并发资源

- `workflow:Gameplay.Units`
- 本域文档
- 单位脚本
- 单位预制体
- 单位动画控制器

## 能力边界

**Active 能力**

- 声明并维护单位九个脚本的当前实现：敌人 4 个 `EnemyMovement`、`EnemyCombat`、`EnemyHealth`、`EnemyKnockBack`；NPC 4 个 `NPCStateController`、`NPCWander`、`NPCPatrol`、`NPCDialogTrigger`；店主 1 个 `ShopKeeper`。同时声明无统一基类、全局命名空间、`EnemyHealth` 是唯一 `IDamageable` 实现者这一现状（`Units_Guide.md` §2.0）。
- 声明单位与 `PathFollower` 的移动契约：各自持实例、只调 `GetPosToGo`/`ArrivedPos`/`ResetPath`/`GetThreshold` 四个方法、`Vector3.zero` 表示无路、阈值语义与重算规则（`Units_Guide.md` §2.5 §3.1 §3.2）。
- 声明单位的三条对外通道与订阅方：`PlayerDamagedEventSO` 对 `PlayerDamageController`、`EnemyDefeatedEventSO` 对 `ExperienceController`、`ShopKeeperEventSO` 对 `ShopManager` 与 `ShopPortraitCamera`，以及敌人反向读 `StatsService.Instance.Stats` 的击退数值耦合（`Units_Guide.md` §2.2 §2.4 §3.4）。
- 声明 `NPCStateController` 的互斥 `enabled` 行为切换、`NPCDialogTrigger` 的 `isKinematic` 接管约定（`Units_Guide.md` §2.3 §3.6 §3.7）。

**不在本域范围，请转到对应 Router**

- 寻路算法、网格构建、开表堆、`AStarDetails`、`ObjectsMapManager` → `..\..\Architecture\Layering\Router.md`、`..\..\Architecture\Composition\Router.md`。
- 商店买卖、库存结算、`IShopInteractable` 面板逻辑 → `..\InventoryShop\Router.md`。
- 对话推进规则、`DialogManager`、`DialogSO` 分支 → `..\Dialog\Router.md`。
- 玩家数值、击退与硬直数值本身 → `..\PlayerStats\Router.md`。
- 玩家攻击命中侧的 `PlayerCombat.DealDamage` 与 `Arrow` → `..\PlayerStats\Router.md`。
- 事件总线机制与 MVC 边界 → `..\..\Architecture\Router.md`。
