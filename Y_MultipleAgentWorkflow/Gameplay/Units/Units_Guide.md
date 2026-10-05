# 单位行为线（Gameplay.Units：敌人 / NPC / 店主）

文档 ID：`GP-UNITS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
权威范围：本域只负责九个单位脚本的当前实现，即敌人 4 个、NPC 4 个、店主 1 个，以及它们通过 `PathFollower` 驱动移动、通过事件通道对外广播的接口；寻路算法本身归寻路模块，商店买卖与库存结算归 Gameplay.InventoryShop，对话推进规则归 Gameplay.Dialog，玩家数值规则归 Gameplay.PlayerStats。

- 单位脚本：`EnemyMovement`、`EnemyCombat`、`EnemyHealth`、`EnemyKnockBack`、`NPCStateController`、`NPCWander`、`NPCPatrol`、`NPCDialogTrigger`、`ShopKeeper`
- 移动依赖面：`PathFollower`、`AStarNodeManager`、`AStarPathFinder`、`AStarOpenHeap`
- 玩家侧与数值：`PlayerCombat`、`PlayerDamageController`、`StatsService`、`PlayerMovement`
- 通道与契约：`PlayerDamagedEventSO`、`EnemyDefeatedEventSO`、`ShopKeeperEventSO`、`ToggleCanvasEventSO`、`IDamageable`、`MyEnums`、`YSingleton`
- 预制体与动画控制器：火炬哥布林预制体、紫色棋子 NPC 预制体、黄色棋子 NPC 预制体、店主预制体、单位动画控制器

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `敌人` `Enemy` `怪物` `追击` `Chase` | §2.1、§2.2 |
| `击退` `KnockBack` `硬直` `stun` `knockBackForce` | §2.2、§3.4、§3.5 |
| `受击` `扣血` `死亡` `expReward` `IDamageable` | §2.0、§2.2 |
| `NPC` `巡逻` `Patrol` `游荡` `Wander` `挂机` | §2.3、§3.6 |
| `对话` `Chat` `Dialog` `Interaction Icon` | §2.3、§3.7 |
| `店主` `ShopKeeper` `商店触发` `进入范围` | §2.4、§3.8 |
| `PathFollower` `GetPosToGo` `ArrivedPos` `ResetPath` `卡住` `不移动` | §2.5、§3.1 |
| `Awake` `OnEnable` `isKinematic` `时序` | §2.3、§3.7 |
| `没有基类` `脚本清单` `职责划分` | §2.0 |

## 2. 当前实现

### 2.0 三个子域的脚本清单（本域没有统一基类）

九个脚本全部落在全局命名空间，全部直接继承 `MonoBehaviour`，本域没有单位基类，也没有单位通用接口；唯一的共享契约是 `EnemyHealth` 实现的 `IDamageable`，它是全工程唯一实现者。

| 子域 | 类型 | 职责 |
|---|---|---|
| Enemy | `EnemyMovement` | 敌人状态机 Idle/Chasing/Attacking/KnockBack、追击寻路、朝向翻转 |
| Enemy | `EnemyCombat` | 攻击判定，只广播玩家受击通道，不持有玩家引用 |
| Enemy | `EnemyHealth` | 血量、死亡广播、实现 `IDamageable`，扣血与击退一次完成 |
| Enemy | `EnemyKnockBack` | 被击退的位移与硬直计时，回写 `EnemyMovement` 状态机 |
| NPC | `NPCStateController` | 互斥 `enabled` 切换三个 NPC 行为组件，玩家进出触发切 Chat |
| NPC | `NPCWander` | 以初始点为圆心随机游荡 |
| NPC | `NPCPatrol` | 矩形四角巡逻 |
| NPC | `NPCDialogTrigger` | 对话开启与推进，对话期间锁住刚体 |
| ShopKeeper | `ShopKeeper` | 玩家进出触发范围的广播与 logo 动画，附带商品列表的对外只读入口 |

预制体接线：敌人五个组件同挂火炬哥布林预制体根；NPC 四个行为脚本与 `PathFollower` 同挂紫色棋子 NPC 预制体根；店主预制体只有 `ShopKeeper` 一个脚本，没有 `PathFollower`。

### 2.1 敌人状态机与追击（EnemyMovement）

- 状态是私有字段 `enemyState`，切换统一走公开方法 `AnimatorSM(EnemyState)`：先关旧状态布尔、再开新状态布尔。三个布尔名 `isIdle`/`isChasing`/`isAttacking` 与火炬哥布林动画控制器的参数名逐一对应，该控制器只声明这三个 bool 参数，状态为 `Idle`/`Move`/`Attack`。状态机没有 KnockBack 动画位，进 `KnockBack` 时三个布尔全关，视觉退到控制器默认态。
- `Awake` 里用 `GetComponent` 取 `Rigidbody2D` 与 `Animator`，二者都在根物体上；`Start` 里先 `AnimatorSM(Idle)`，再取阈值 `pathFollower.GetThreshold()`。
- `Update` 的第一层条件是 `enemyState != EnemyState.KnockBack`。处于击退态时整个 Update 体被跳过，侦测、攻击冷却、速度写入全部停摆，击退期间的速度由 `EnemyKnockBack` 独占。
- 侦测每 `detectInterval = 0.2f` 秒做一次 `Physics2D.OverlapCircle(detectionPoint.position, playerDetectRange, playerMask)`。命中即缓存 `player = hit.transform`；距离小于 `attackDetectRange` 且冷却结束则进 `Attacking` 并重置冷却；距离更远且当前为 Idle 则进 `Chasing`；侦测不到则清零速度并回 `Idle`。`playerMask` 与 `playerLayer` 在预制体里都是 256，对应层 8 的 `Player`。
- 追击先构造优化起点 `optPos = 归一化(玩家-敌人) * 0.2 + 自身位置`，再调 `pathFollower.GetPosToGo(optPos, startPos, endPos)`。返回 `Vector3.zero` 视为无路，回 `Idle` 并 `return`；速度写入经 `SetVelocity`，距当前路点小于阈值时调 `pathFollower.ArrivedPos()` 弹出路点。
- 速度写入带 0.2 秒节流与方向点积判断。朝向翻转走 `localScale.x` 取反。

### 2.2 敌人攻击、血量与击退（EnemyCombat / EnemyHealth / EnemyKnockBack）

- `EnemyCombat.Attack()` 做一次 `Physics2D.OverlapCircleAll(attackPoint.position, weaponRange, playerLayer)`，判据是 `hits.Length > 0 && hits[0].enabled`，只看第一个碰撞体，然后只广播 `playerDamagedEvent.OnPlayerDamaged(damage, transform, knockBackForce, stunTime)`。它不引用玩家任何组件，扣血、击退、死亡全由订阅方 `PlayerDamageController.OnDamaged` 统一处理。
- 敌人自身数值在预制体上：`damage 1`、`weaponRange 1.6`、`playerLayer 256`、`knockBackForce 2`、`stunTime 0.2`。
- `EnemyHealth` 的 `Start` 把 `currentHealth` 置为 `maxHealth`；`ChangeHealth(int)` 加上限钳制，血量 `<= 0` 时广播 `defeatedEvent.OnEnemyDefeated(expReward, transform)` 并 `Destroy(gameObject)`；`TakeDamage(int, Transform)` 先调 `ChangeHealth(-damage)`，再调 `knockBack.Knockback(...)`。`defeatedEvent` 的订阅方是 `ExperienceController.GainExp`。
- `EnemyKnockBack.Knockback` 写 `AnimatorSM(EnemyState.KnockBack)`，以自身减攻击者的归一化方向乘击退力写 `rb.velocity`，再起协程 `StunTimer(stunTime, knockBackTime)`。协程先等 `knockBackTime` 清零速度，再等 `stunTime` 回 `Idle`。参数顺序是 `(攻击者, 击退力, stunTime, knockBackTime)`，调用点按同序传参。
- 敌人被击退的数值反向取自玩家数值：`EnemyHealth.TakeDamage` 读 `StatsService.Instance.Stats` 的 `KnockBackForce`、`StunTime`、`KnockBackTime` 三个只读属性，玩家数值配置资产现值是 0.5 / 0.2 / 0.2。

### 2.3 NPC：互斥 enabled 切行为 + 触发切对话

- `NPCStateController` 持三个行为组件引用与 `DefaultState`，`DefaultState` 的默认值是 `Patrol`，`Start` 调 `SwitchState(DefaultState)`。`SwitchState` 的实现是三行互斥赋值：`wanderScript.enabled = (state == Wander)`、`dialogTrigger.enabled = (state == Chat)`、`patrolScript.enabled = (state == Patrol)`；同一时刻只有一个行为组件被启用。
- 预制体里三个行为组件出厂即 disabled，首次启用完全依赖 `SwitchState`。`DefaultState = 2` 对应 `MyEnums.NPCState.Patrol`，枚举顺序是 Idle 0、Wander 1、Patrol 2、Chat 3。
- 玩家进入或离开触发区时按 tag 切换：`OnTriggerEnter2D` 切 `Chat`，`OnTriggerExit2D` 切 `DefaultState`。触发体是预制体根上的 `CircleCollider2D`，半径 1 且为 Trigger；根物体另有一个非触发 `CapsuleCollider2D` 与 `Rigidbody2D`。
- `NPCWander` 与 `NPCPatrol` 有同一个时序约定：`Rigidbody2D` 必须在 `Awake` 里取，因为 `OnEnable` 要用它设 `isKinematic = false`。
- 三个行为组件各自在 `OnDisable` 里收尾：`NPCWander` 与 `NPCPatrol` 关 `isWalking`；`NPCDialogTrigger` 退订通道、禁用输入动作、置 `isKinematic = false`、把交互图标播回 `Idle`，并强制结束对话。
- `NPCDialogTrigger` 是 Chat 态的唯一实现。`NPCStateController` 一侧的 `dialogTrigger` 字段带 `FormerlySerializedAs("chatScript")` 兼容标签，带 `chatScript` 的序列化数据仍能解析到该字段。`OnEnable` 订阅 `toggleDialogEvent.toggleCanvasEvent`，把 `rb.velocity` 清零并置 `isKinematic = true`，调 `chatAnimator.Play("Chat")`，启用 `advanceDialogAction`；`Update` 在收到开对话请求且 `!DialogManager.Instance.isDialogActive` 时调 `StartDialog(dialogSO)`，对话激活期间按 `advanceDialogAction.WasPressedThisFrame()` 推进。`chatAnimator` 指的是 Interaction Icon 子物体上的动画控制器，含 `Chat` 与 `Idle` 两个状态；NPC 本体动画在 NPC 本体动画控制器上，含 `isWalking` 参数与 `Idle`/`Walk` 状态。
- 坐标驱动：`NPCWander` 以 `Start` 时的位置为圆心，半径 `patrolRadius`；`NPCPatrol` 以 `Start` 时位置为中心算四角，`clockwise` 决定 `+1` 还是 `+3` 取模。两者都在到达目标点或路点为零时进等待协程。

### 2.4 店主：OnTriggerEnter2D 广播通道，ShopManager 订阅

`ShopKeeper` 对外只有一条接口，即序列化的 `ShopKeeperEventSO` 通道：

- 玩家进入：`OnTriggerEnter2D` 先判 tag，广播 `shopKeeperEvent.RaiseShopKeeperEntered(this)`，再把 `logoAnimator` 的 `playerInRange` 置 true。
- 玩家离开：`OnTriggerExit2D` 同构广播 `RaiseShopKeeperExited(this)`。
- 组件失活：`OnDisable` 再广播一次 `RaiseShopKeeperExited(this)`，用于店主随场景卸载或被销毁时收口。
- 触发体是根物体上的 `CapsuleCollider2D`，size 5×5 且为 Trigger；根物体层为 12 的 `NPC`，tag 为 `Untagged`。判据是进入方的 tag，即 `collider.CompareTag("Player")`，玩家 tag 来自常驻场景。
- 通道定义是 `ShopKeeperEntered` 与 `ShopKeeperExited` 两个 `Action<ShopKeeper>` 事件。事件资产由店主预制体与订阅方共同引用。
- 订阅方只有两个，且都在常驻场景：`ShopManager` 与 `ShopPortraitCamera`。`ShopManager` 用 `activeShopKeeper` 记住当前店主，退出时带 `activeShopKeeper != keeper` 防护，并在开店状态下调 `CloseShop()`。
- 商品数据只经只读属性暴露：`ShopItems`、`ShopWeapon`、`ShopArmor`。预制体里三个列表为空，实际内容由场景实例覆写，Scene1 场景实例填了 2 条 `shopItems` 与 1 条价 50 的 `shopWeapon`。买卖结算与 `IShopInteractable` 面板逻辑属 InventoryShop，本域不涉及。
- 实例分布：`ShopKeeper` 在 Scene1 有 2 个、StartingMenu 有 1 个；NPC 在 Scene1 有 2 个紫色加 1 个黄色、Scene2 有 1 个紫色、StartingMenu 有 1 个紫色；敌人在 TestScene 有 62 个、Scene2 有 40 个、StartingMenu 有 4 个、Scene1 有 2 个。

### 2.5 移动约定：单位各自持 PathFollower，驱动同一组四个方法

`PathFollower` 挂在每个单位自己身上，逐单位各持一份实例，服务该单位自己的移动。敌人的行为脚本与两个 NPC 行为脚本经序列化字段 `pathFollower` 引用同一物体上的实例，该字段带 `FormerlySerializedAs("aStarController")` 兼容标签，分别写在 `EnemyMovement`、`NPCWander`、`NPCPatrol` 三处。

单位只用四个方法，调用节奏是硬约定：

| 方法 | 语义 | 单位侧用法 |
|---|---|---|
| `GetPosToGo(optPos, startPos, endPos)` | 返回当前栈顶路点的世界坐标；无路或栈空返回 `Vector3.zero` | 每帧调用；`optPos` 只有敌人用，NPC 一律传 `Vector3.zero` |
| `ArrivedPos()` | 弹出栈顶路点 | 距路点小于阈值时调用 |
| `ResetPath()` | 清空路径、起点终点与 `hasValidPath` | 目标变更或放弃时调用 |
| `GetThreshold()` | `threshold(0.5f) * AStarNodeManager.GetCellSize()`；`AStarNodeManager.Instance == null` 时返回 `0f` | `Start` 里取一次缓存；敌人用原值，两个 NPC 都乘 `0.2f` |

`GetPosToGo` 内部还有一条重算规则：目标点相对上次寻路终点位移超过 `pathRebuildDistance(0.5) * cellSize`，且 `0.5s` 冷却已过时重算。返回值是格心加避障推离量。

寻路链是 `PathFollower` 调 `AStarPathFinder.Instance.FindPath`，`AStarPathFinder` 是 `AStarNodeManager` 的转发层，网格在 `AStarNodeManager.OnSingletonInitialized` 构建。`AStarPathFinder` 的开放列表是二叉最小堆 `AStarOpenHeap`，`FindPath` 用 `heap.Push` 与 `heap.Pop`，并在弹出时按 G 丢弃过期项。单位不接触 `AStarDetails`、`AStarOpenHeap`、`ObjectsMapManager`。

## 3. 约定与硬边界

### 3.1 `GetPosToGo` 返回 `Vector3.zero` 是「无路」的唯一信号

三个行为脚本都写了 `posToGo == Vector3.zero` 分支，零向量不作为合法路点处理。

### 3.2 到点必须调 `ArrivedPos()`，换目标必须先 `ResetPath()`

`PathFollower` 的路点是 `Stack`，只有 `ArrivedPos` 会弹栈。`AStarNodeManager` 的注释记录了这条约定的一处后果：路点跨格时跟随者会在邻格弹出本格节点，每帧重算路径。

### 3.3 `GetThreshold()` 依赖 `AStarNodeManager` 已 Awake

网格管理器未就绪时它返回 `0f`，并以 `Debug.LogWarning` 收尾，该警告仅在编辑器输出。

### 3.4 敌人被击退的数值来自玩家，方向来自攻击者

`EnemyHealth.TakeDamage` 是唯一读 `StatsService` 的单位代码，这里没有 null 守卫，`YSingleton.Instance` 在 Awake 前为 null。敌人自身序列化的 `knockBackForce` 是给玩家的值。

### 3.5 `EnemyMovement` 的 KnockBack 态是全停态

进入后 `Update` 整段不执行，由 `EnemyKnockBack.StunTimer` 协程在 `knockBackTime + stunTime` 后调回 `Idle`。

### 3.6 NPC 行为组件出厂是 disabled 的

`NPCStateController.Start` 的 `SwitchState(DefaultState)` 是唯一的初始启用点，绕过它会绕过互斥，两个行为脚本会同时写 `rb.velocity`。

### 3.7 `NPCDialogTrigger` 在 Chat 期间把刚体设成 `isKinematic = true` 并写死 `velocity = 0`

退出时只恢复 `isKinematic = false`；速度接管落在下一个行为组件的 `OnEnable` 上，新增行为组件必须自己清速度。

### 3.8 店主与玩家的接口只有 `ShopKeeperEventSO` 一条

`ShopKeeper` 不实现 `IShopInteractable`，该接口由 `ShopManager` 实现；增开一个店主的做法是挂同一套预制体并接同一个通道资产，代码不变。`ShopManager` 的 `activeShopKeeper` 是当前店主的唯一记录点。

### 3.9 商品列表按场景实例覆写，落在场景实例上

预制体三个列表为空，加店必须在场景实例里填，示例见 Scene1 场景实例。
