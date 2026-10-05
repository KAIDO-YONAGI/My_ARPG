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
| `敌人血量` / `EnemyHealth` / `IDamageable` / `TakeDamage` / `expReward` / 死亡 | `Units_Guide.md` §2.2 §3.4 |
| `击退` / `EnemyKnockBack` / `knockBackForce` / `stunTime` / `knockBackTime` / 硬直 | `Units_Guide.md` §2.2 §3.4 §4 D4 |
| `NPC` / `NPCStateController` / `SwitchState` / `enabled` 互斥 | `Units_Guide.md` §2.3 §3.6 |
| `巡逻` / `NPCPatrol` / `patrolSize` / `clockwise` / 矩形路线 | `Units_Guide.md` §2.3 |
| `游荡` / `NPCWander` / `patrolRadius` / `randomDirection` / 卡住不动 | `Units_Guide.md` §2.3 §4 D1 |
| `对话触发` / `NPCDialogTrigger` / `Chat` / `advanceDialogAction` / 交互图标 | `Units_Guide.md` §2.3 §3.7 |
| `店主` / `ShopKeeper` / `ShopKeeperEventSO` / 进出触发范围 / `PortraitTarget` | `Units_Guide.md` §2.4 §3.8 |
| `商品列表` / `shopItems` / `shopWeapon` / `shopArmor` / 场景实例覆写 | `Units_Guide.md` §2.4 §3.9，结算归 InventoryShop |
| `PathFollower` / `GetPosToGo` / `ArrivedPos` / `ResetPath` / `GetThreshold` / 单位不动 | `Units_Guide.md` §2.5 §3.1 §3.2 §4 D2 |
| `Awake` / `OnEnable` / `isKinematic` / 组件时序 | `Units_Guide.md` §2.3 §4 §5 |
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

- 声明并维护单位九个脚本的当前实现：敌人 4 个 `EnemyMovement`、`EnemyCombat`、`EnemyHealth`、`EnemyKnockBack`；NPC 4 个 `NPCStateController`、`NPCWander`、`NPCPatrol`、`NPCDialogTrigger`；店主 1 个 `ShopKeeper`。同时声明无统一基类、全局命名空间、`EnemyHealth` 是唯一 `IDamageable` 实现者这一现状（§2.0）。
- 声明单位与 `PathFollower` 的移动契约：各自持实例、只调 `GetPosToGo`/`ArrivedPos`/`ResetPath`/`GetThreshold` 四个方法、`Vector3.zero` 表示无路、阈值语义与重算规则（§2.5 §3.1 §3.2）。
- 声明单位的三条对外通道与订阅方：`PlayerDamagedEventSO` 对 `PlayerDamageController`、`EnemyDefeatedEventSO` 对 `ExperienceController`、`ShopKeeperEventSO` 对 `ShopManager` 与 `ShopPortraitCamera`，以及敌人反向读 `StatsService.Instance.Stats` 的击退数值耦合（§2.2 §2.4 §3.4）。
- 声明 `NPCStateController` 的互斥 `enabled` 行为切换、`NPCDialogTrigger` 的 `isKinematic` 接管约定（§2.3 §3.6 §3.7）。
- 登记本域 7 条已知缺陷（§4 D1–D7），其中 `NPCWander` 的无上限 `do-while`（D1）与 `GetThreshold()` 返回 0 的静默失效（D2）是当前最需要用户知情的行为风险。

**不在本域范围，请转到对应 Router**

- 寻路算法、网格构建、开表堆、`AStarDetails`、空壳的 `ObjectsMapManager` → 尚未建立权威文档。
- 商店买卖、库存结算、`IShopInteractable` 面板逻辑 → `Gameplay\InventoryShop\Router.md`。
- 对话推进规则、`DialogManager`、`DialogSO` 分支 → `Gameplay\Dialog\Router.md`。
- 玩家数值、击退与硬直数值本身 → `Gameplay\PlayerStats\Router.md`。
- 玩家攻击命中侧的 `PlayerCombat.DealDamage` 与 `Arrow` → `Gameplay\PlayerStats\Router.md`，本域只登记被调用关系。
- 事件总线机制与 MVC 边界 → `Architecture\Router.md`。

**Proposal（尚未实施，仅记录）**

- 按功能域拆 asmdef。本域当前无任何 asmdef，全部落 `Assembly-CSharp`；拆分方案需用户确认后再动，且会牵动 EditMode 测试的装配体。
- 为 `NPCWander.WaitAndContinue` 的重试循环加上限（D1），为 `EnemyMovement.Start` 补 `pathFollower` 空检查（D3）。两者都属「违反当前实现约定即静默失效」的缺陷修复，须用户确认后另立 Design 文档。
- `ObjectsMapManager` 为空实现且全工程零引用，是否删除待确认。

**需要用户确认的事项**

- 任何修改单位预制体与单位动画控制器接线的动作。`pathFollower`、`animator`、`dialogSO`、`shopKeeperEvent` 字段直接决定本域脚本能否运行，且 `dialogSO` 与三个商品列表按场景实例覆写。
- 调整敌人击退与硬直手感时先定改哪一侧：敌人打玩家读敌人自身的 `knockBackForce` 与 `stunTime`，玩家打敌人读玩家数值（§4 D4）。
- 新增或修改店主实例的商品列表，即 Scene1 等场景实例上的覆写，涉及商店内容与价格。
