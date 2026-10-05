# Gameplay.Units Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Gameplay.Units 权威文档

**写入的文件**

- `Y_MultipleAgentWorkflow/Gameplay/Units/Units_Guide.md`（新建，ID `GP-UNITS-GUIDE`，状态 Active）
- `Y_MultipleAgentWorkflow/Gameplay/Units/Router.md`（按项目中文模板重写，文档 ID 保持 `BUS-GAMEPLAY-UNITS`，下级导航表逐行原样保留 `None` 行）
- `Y_MultipleAgentWorkflow/Gameplay/Units/DeveloperLog.md`（本条目，未删除既有内容）

**依据的证据路径**

- 代码 `Assets/Scripts/Gameplay/Units/Enemy/EnemyMovement.cs`、`EnemyCombat.cs`、`EnemyHealth.cs`、`EnemyKnockBack.cs`
- 代码 `Assets/Scripts/Gameplay/Units/NPC/NPCStateController.cs`、`NPCWander.cs`、`NPCPatrol.cs`、`NPCDialogTrigger.cs`
- 代码 `Assets/Scripts/Gameplay/Units/ShopKeeper/ShopKeeper.cs`
- 移动依赖 `Assets/Scripts/Pipeline/Pathfinding/PathFollower.cs`、`AStarNodeManager.cs`、`AStarPathFinder.cs`、`AStarDetails.cs`、`ObjectsMapManager.cs`
- 契约与通道 `Assets/Scripts/Contracts/IDamageable.cs`、`MyEnums.cs`、`YSingleton.cs`、`IShopInteractable.cs`；`Assets/Scripts/Pipeline/SO/Events/PlayerDamagedEventSO.cs`、`EnemyDefeatedEventSO.cs`、`ShopKeeperEventSO.cs`、`ToggleCanvasEventSO.cs`
- 跨域消费/生产侧 `Assets/Scripts/Gameplay/Player/PlayerCombat.cs`、`Controllers/PlayerDamageController.cs`、`Controllers/ExperienceController.cs`、`Services/StatsService.cs`、`Models/IPlayerStatsReadOnly.cs`、`PlayerMovement.cs`；`Assets/Scripts/Gameplay/Shop/ShopManager.cs`、`ShopPortraitCamera.cs`
- 接线证据 `Assets/Prefabs/Units/TorchGoblin_Red.prefab`、`Assets/Prefabs/Units/NPCs/PurplePawn_NPC.prefab`、`YellowPawn_NPC.prefab`、`ShopKeeper.prefab`（按 `.meta` GUID 反查 `m_Script`）；`Assets/Scenes/GameScene/Scene1.unity`、`Scene2.unity`、`StartingMenu.unity`、`PersistentScene.unity`、`Assets/Scenes/TestScene.unity`
- 动画侧 `Assets/Animation/Units/Enemy/TorchGoblin_Red/TorchGoblin_Red.controller`、`Assets/Animation/Units/NPC/PurplePawn.controller`、`Interaction Icon.controller`
- 数值与层 `Assets/GameSO/PlayerStatsSO.asset`、`Assets/GameSO/Events/ShopKeeperEvent.asset`、`ProjectSettings/TagManager.asset`
- 旧文档 `Docs/My_ARPG_MVCS项目现状.md`（1.2 节，与代码一致）、`Docs/UML/03_Enemy_NPC_System.puml`（过期）、`Docs/UML/06_Dialog_Inventory.puml`、`Docs/开发日志.txt`

**已核验**

- 九个脚本、全局命名空间、无统一基类；`EnemyHealth` 是全工程唯一 `IDamageable` 实现者（`IDamageable` 全仓命中仅 `Contracts/IDamageable.cs`、`EnemyHealth.cs:6`、消费方 `PlayerCombat.cs:31`、`Arrow.cs:81`）。
- 组件接线：敌人 5 个组件同根（`TorchGoblin_Red.prefab:242-323`，`EnemyMovement.pathFollower` → 同根 `fileID 4744295733257843321`）；NPC 4 脚本 + `PathFollower` 同根（`PurplePawn_NPC.prefab:362-431`）；`ShopKeeper` 无 `PathFollower`（`ShopKeeper.prefab:215-232`）。
- 三个行为组件出厂 `m_Enabled: 0`（`PurplePawn_NPC.prefab:388,406,423`），`DefaultState: 2` = `NPCState.Patrol`（`MyEnums.cs:39-45`）。
- 动画参数名对齐：敌人控制器只声明 `isIdle`/`isChasing`/`isAttacking`（无 KnockBack 位），NPC 本体控制器声明 `isWalking`，交互图标控制器含 `Chat`/`Idle` 状态。
- 店主唯一接口：`ShopKeeperEventSO`（`ShopKeeper.cs:28,38,46`）；订阅方仅 `ShopManager`（`PersistentScene.unity:23597`）与 `ShopPortraitCamera`（`PersistentScene.unity:21804`），三处引用同一资产 guid `5b083df9b77ba3f4c877b0871e81e6bf`；触发体为根上 `CapsuleCollider2D` trigger 5×5（`ShopKeeper.prefab:180-214`）。
- 商品列表预制体为空、由场景实例覆写（`ShopKeeper.prefab:230-232` vs `Scene1.unity:281-311`）；`dialogSO` 同样按实例覆写（`Scene1.unity:8678,21079,22574`、`Scene2.unity:643`）。
- 玩家 tag 唯一来源 `PersistentScene.unity:32287`；`playerMask`/`playerLayer` 的 `m_Bits: 256` = 层 8 `Player`（`ProjectSettings/TagManager.asset:20`）。
- `PathFollower` 四个对外方法的语义与返回值边界（`:21-72`，`AStarNodeManager.Instance` 为空时 `GetThreshold()` 返回 `0f`）。
- `Assets/**/*.asmdef` 无结果（本域与全工程均无 asmdef）。

**未核验**

- 未运行 Unity：预制体/场景的引用是否丢引用、`NPCStateController.Start` 与玩家触发进场的先后、禁用组件首次启用时的 `Awake`/`OnEnable` 顺序、`StatsService.Instance` 在首次 `TakeDamage` 时是否就绪。
- 未核验 `EnemyCombat.Attack()` 的调用来源（本域脚本内无调用点，推断为动画事件）。
- 未核验 `Physics2D` 层碰撞矩阵是否允许层 8 与敌人/店主触发体相交。
- 未逐一打开 `StartingMenu.unity` / `Scene1.unity` 的每个店主实例确认事件资产未被覆写。

**发现的缺陷**（详见 Guide §4）

- D1 `NPCWander.WaitAndContinue` 的无上限同步 `do-while`（`NPCWander.cs:96-106`）：寻路不可用时主线程死循环。
- D2 `PathFollower.GetThreshold()` 在 `AStarNodeManager` 缺失时返回 `0f`（`:62-69`）→ 单位永不 `ArrivedPos`，原地播走路动画。
- D3 `EnemyMovement.Start` 无 `pathFollower` 空检查（`:66`），而 `NPCPatrol.Start` 有（`:49-54`）。
- D4 击退数值双向来源不对称：敌人打玩家用敌人自身序列化值，玩家打敌人读 `StatsService`（`EnemyHealth.cs:47-49`，且无 null 守卫）。
- D5 `ChangeHealth` 先 `Destroy` 再进击退协程（`EnemyHealth.cs:32-33,45-50`），死亡当帧可见击退动画、协程静默中断。
- D6 NPC Chat 切换只按 tag 去重（`NPCStateController.cs:28-41`），触发圈重叠时存在对话竞争与状态互踩。
- D7 KnockBack 态整段跳过 `Update`（`EnemyMovement.cs:70`）→ 攻击冷却与侦测计时冻结。
- D8 旧 UML `Docs/UML/03_Enemy_NPC_System.puml` 记为 `NPCChat`/`MovementController`/`ExpManager`/`MonsterDefeated`/`PlayerHealth`，五个类名在代码中零命中；`Docs/开发日志.txt:51` 与 `Docs/UML/06_Dialog_Inventory.puml:136,182` 仍称 `ShopPortraitCameraController`（实际 `ShopPortraitCamera`）。

**维护计数**

- `BUS-GAMEPLAY-UNITS` 维持 `0/5`（首次建档，未发生 5 次触发的维护轮次）。
