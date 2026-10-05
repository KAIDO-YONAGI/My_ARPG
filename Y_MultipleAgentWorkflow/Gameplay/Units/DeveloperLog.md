# Gameplay.Units Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Gameplay.Units 权威文档

**写入的文件**

- `Units_Guide.md`，新建，文档 ID `GP-UNITS-GUIDE`，状态 Active
- `Router.md`，按项目中文模板重写，文档 ID 保持 `BUS-GAMEPLAY-UNITS`，下级导航表逐行保留 `None` 行
- `DeveloperLog.md`，本条目

**依据**

- 单位脚本九个类型：`EnemyMovement`、`EnemyCombat`、`EnemyHealth`、`EnemyKnockBack`、`NPCStateController`、`NPCWander`、`NPCPatrol`、`NPCDialogTrigger`、`ShopKeeper`
- 移动依赖：`PathFollower` 的 `GetPosToGo`、`ArrivedPos`、`ResetPath`、`GetThreshold` 四个对外方法，以及 `AStarNodeManager`、`AStarPathFinder`、`AStarDetails`、`AStarOpenHeap`、`ObjectsMapManager` 组成的寻路链
- 契约与通道：`IDamageable`、`MyEnums`、`YSingleton`、`IShopInteractable`；`PlayerDamagedEventSO`、`EnemyDefeatedEventSO`、`ShopKeeperEventSO`、`ToggleCanvasEventSO`
- 跨域消费与生产侧：`PlayerCombat`、`PlayerDamageController`、`ExperienceController`、`StatsService`、`IPlayerStatsReadOnly`、`PlayerMovement`；`ShopManager`、`ShopPortraitCamera`
- 接线证据：火炬哥布林预制体、紫色棋子 NPC 预制体、黄色棋子 NPC 预制体、店主预制体的组件挂载与序列化引用，以及 Scene1、Scene2、StartingMenu、PersistentScene、TestScene 的实例覆写
- 动画侧：火炬哥布林控制器、紫色棋子控制器、Interaction Icon 控制器的参数与状态
- 数值与层：`PlayerStatsSO` 资产、`ShopKeeperEvent` 资产、TagManager 的层定义

**已核验**

- 九个脚本、全局命名空间、无统一基类；`EnemyHealth` 是全工程唯一 `IDamageable` 实现者，`IDamageable` 的其余命中点是消费方 `PlayerCombat` 与 `Arrow`。
- 组件接线：敌人五个组件同挂火炬哥布林预制体根，`EnemyMovement.pathFollower` 指向同根实例；NPC 四个行为脚本与 `PathFollower` 同挂紫色棋子 NPC 预制体根；店主预制体只有 `ShopKeeper`，没有 `PathFollower`。
- 三个 NPC 行为组件在预制体里出厂即 disabled，`DefaultState` 为 `NPCState.Patrol`。
- 动画参数名对齐：敌人控制器声明 `isIdle`、`isChasing`、`isAttacking` 三个参数，NPC 本体控制器声明 `isWalking`，交互图标控制器含 `Chat` 与 `Idle` 两个状态。
- 店主唯一接口是 `ShopKeeperEventSO`，订阅方只有常驻场景里的 `ShopManager` 与 `ShopPortraitCamera`，三处引用同一资产 guid `5b083df9b77ba3f4c877b0871e81e6bf`；触发体是店主预制体根上的 `CapsuleCollider2D` trigger，size 5×5。
- 商品列表在店主预制体里为空，内容由场景实例覆写；`dialogSO` 同样按实例覆写。
- 玩家 tag 的唯一来源是常驻场景；`playerMask` 与 `playerLayer` 的取值 256 对应 TagManager 中第 8 层 `Player`。
- `PathFollower` 四个对外方法的语义与返回值边界；`AStarNodeManager.Instance` 为空时 `GetThreshold()` 返回 `0f`。
- 本域与全工程都没有程序集定义资产，全部脚本落在预定义程序集里。

**维护计数**

- 本次是首次建档，`BUS-GAMEPLAY-UNITS` 维持 `0/5`。
