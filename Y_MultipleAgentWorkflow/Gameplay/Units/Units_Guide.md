# 单位行为线（Gameplay.Units：敌人 / NPC / 店主）

文档 ID：`GP-UNITS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本域只负责 `Assets/Scripts/Gameplay/Units/**` 九个脚本（敌人 4、NPC 4、店主 1）的当前实现，以及它们通过 `PathFollower` 驱动移动、通过事件通道对外广播的接口；不负责寻路算法本身（`Pipeline/Pathfinding/**`）、不负责商店买卖与库存结算（属 Gameplay.InventoryShop）、不负责对话推进规则（属 Gameplay.Dialog）、不负责玩家数值规则（属 Gameplay.PlayerStats）。
上游来源：

- 代码 `Assets/Scripts/Gameplay/Units/Enemy/**`、`NPC/**`、`ShopKeeper/**`
- 代码 `Assets/Scripts/Pipeline/Pathfinding/PathFollower.cs`（单位移动的唯一依赖面）、`AStarNodeManager.cs`、`AStarPathFinder.cs`、`AStarOpenHeap.cs`（本轮新增的开放列表堆，仅供 `AStarPathFinder` 内部使用）
- 代码 `Assets/Scripts/Gameplay/Player/PlayerCombat.cs`、`Assets/Scripts/Gameplay/Player/Controllers/PlayerDamageController.cs`、`Services/StatsService.cs`、`Player/PlayerMovement.cs`
- 通道 `Assets/Scripts/Pipeline/SO/Events/{PlayerDamagedEventSO,EnemyDefeatedEventSO,ShopKeeperEventSO,ToggleCanvasEventSO}.cs`、`Contracts/{IDamageable,MyEnums,YSingleton}.cs`
- 预制体 `Assets/Prefabs/Units/TorchGoblin_Red.prefab`、`Assets/Prefabs/Units/NPCs/{PurplePawn_NPC,YellowPawn_NPC,ShopKeeper}.prefab`；动画控制器 `Assets/Animation/Units/**`
- 旧文档 `Docs/My_ARPG_MVCS项目现状.md`（1.2 节两条数据流与代码一致）、`Docs/UML/03_Enemy_NPC_System.puml`（已过期，见 §4 D8）

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `敌人` `Enemy` `怪物` `追击` `Chase` | §2.1、§2.2、§3.1 |
| `击退` `KnockBack` `硬直` `stun` `knockBackForce` | §2.2、§2.5、§3.4 |
| `受击` `扣血` `死亡` `expReward` `IDamageable` | §2.2、§2.4 |
| `NPC` `巡逻` `Patrol` `游荡` `Wander` `挂机` | §2.3、§3.2 |
| `对话` `Chat` `Dialog` `Interaction Icon` | §2.3、§3.3 |
| `店主` `ShopKeeper` `商店触发` `进入范围` | §2.4、§3.5 |
| `PathFollower` `GetPosToGo` `ArrivedPos` `ResetPath` `卡住` `不移动` | §2.5、§3.1 |
| `Awake` `OnEnable` `isKinematic` `时序` | §2.3、§3.2 |
| `没有基类` `脚本清单` `职责划分` | §2.0 |

## 2. 当前实现

### 2.0 三个子域的脚本清单（本域没有统一基类）

九个脚本全部在全局命名空间（无 `namespace`）、全部直接继承 `MonoBehaviour`，**不存在**单位基类或单位通用接口；唯一的共享契约是 `EnemyHealth` 单方面实现的 `IDamageable`（`Enemy/EnemyHealth.cs:6`，全工程唯一实现者）。

| 子域 | 文件 | 职责 |
|---|---|---|
| Enemy | `Enemy/EnemyMovement.cs` | 敌人状态机（Idle/Chasing/Attacking/KnockBack）+ 追击寻路 + 朝向翻转 |
| Enemy | `Enemy/EnemyCombat.cs` | 攻击判定，只广播玩家受击通道，不持有玩家引用 |
| Enemy | `Enemy/EnemyHealth.cs` | 血量、死亡广播、实现 `IDamageable`（扣血 + 击退一次完成） |
| Enemy | `Enemy/EnemyKnockBack.cs` | 被击退的位移与硬直计时，回写 `EnemyMovement` 状态机 |
| NPC | `NPC/NPCStateController.cs` | 互斥 `enabled` 切换三个 NPC 行为组件 + 玩家进出触发切 Chat |
| NPC | `NPC/NPCWander.cs` | 以初始点为圆心随机游荡 |
| NPC | `NPC/NPCPatrol.cs` | 矩形四角巡逻 |
| NPC | `NPC/NPCDialogTrigger.cs` | 对话开启/推进，对话期间锁住刚体 |
| ShopKeeper | `ShopKeeper/ShopKeeper.cs` | 玩家进出触发范围的广播与 logo 动画，附带商品列表的对外只读入口 |

预制体接线（证据取自 prefab 内 `m_Script` 的 GUID 匹配）：敌人五个组件同挂 `TorchGoblin_Red` 根（`TorchGoblin_Red.prefab:242-323`）；NPC 四个行为脚本 + `PathFollower` 同挂预制体根 `fileID 1108407708067783558`（`PurplePawn_NPC.prefab:362-431`）；`ShopKeeper` 只有一个脚本，无 `PathFollower`（`ShopKeeper.prefab:215-232`）。

### 2.1 敌人状态机与追击（EnemyMovement）

- 状态是私有字段 `enemyState`（:16），切换统一走公开方法 `AnimatorSM(EnemyState)`（:39-57）：先关旧状态布尔、再开新状态布尔。三个布尔名 `isIdle/isChasing/isAttacking`（:7-9）与控制器 `TorchGoblin_Red.controller` 的参数名逐一对应（该文件只声明这三个 bool 参数，状态为 `Idle/Move/Attack`）；**状态机没有 KnockBack 动画位**，进 `KnockBack` 时三个布尔全关，视觉退到控制器默认态。
- `Awake` 里 `GetComponent` 取 `Rigidbody2D` 与 `Animator`（:59-61，二者都在根物体上，`TorchGoblin_Red.prefab:159,221`）；`Start` 里先 `AnimatorSM(Idle)` 再取阈值 `pathFollower.GetThreshold()`（:65-66）。
- `Update` 的第一层条件是 `enemyState != EnemyState.KnockBack`（:70）：处于击退态时**整个 Update 体被跳过**，侦测、攻击冷却、速度写入全部停摆，击退期间的速度由 `EnemyKnockBack` 独占。
- 侦测：每 `detectInterval = 0.2f` 秒做一次 `Physics2D.OverlapCircle(detectionPoint.position, playerDetectRange, playerMask)`（:72-77、:93）。命中即缓存 `player = hit.transform`（:97）；距离小于 `attackDetectRange` 且冷却结束 → `Attacking` 并重置冷却（:98-103）；距离更远且当前 Idle → `Chasing`（:104-107）；侦测不到 → 清零速度并回 `Idle`（:110-113）。`playerMask`/`playerLayer` 在预制体里是 `m_Bits: 256`（层 8 = `Player`，`ProjectSettings/TagManager.asset:20`）。
- 追击（:117-147）：先构造优化起点 `optPos = 归一化(玩家-敌人) * 0.2 + 自身位置`（:127），再 `pathFollower.GetPosToGo(optPos, startPos, endPos)`（:130）；返回 `Vector3.zero` 视为无路，回 `Idle` 并 `return`（:132-139）；速度写入经 `SetVelocity`（:141），距当前路点小于阈值时 `pathFollower.ArrivedPos()` 弹出路点（:143-146）。
- 速度写入带 0.2 秒节流与方向点积判断（:28-29、:148-164），朝向翻转改 `localScale.x` 取反而非乘 `facingDirec`（:165-170，:169 注释自陈原因）。

### 2.2 敌人攻击、血量与击退（EnemyCombat / EnemyHealth / EnemyKnockBack）

- `EnemyCombat.Attack()`（:15-25）做一次 `Physics2D.OverlapCircleAll(attackPoint.position, weaponRange, playerLayer)`，判据是 `hits.Length > 0 && hits[0].enabled`（:20，只看第一个碰撞体），然后只广播 `playerDamagedEvent.OnPlayerDamaged(damage, transform, knockBackForce, stunTime)`（:23）。**它不引用玩家任何组件**，扣血/击退/死亡全由订阅方 `PlayerDamageController.OnDamaged` 统一处理（`Player/Controllers/PlayerDamageController.cs:31-46`），这与 `Docs/My_ARPG_MVCS项目现状.md:39` 的描述一致。
- 敌人自身数值在预制体上：`damage 1`、`weaponRange 1.6`、`playerLayer 256`、`knockBackForce 2`、`stunTime 0.2`（`TorchGoblin_Red.prefab:254-262`）。
- `EnemyHealth`（:6-51）：`Start` 把 `currentHealth = maxHealth`（:19-22）；`ChangeHealth(int)` 加上限钳制，`<= 0` 时广播 `defeatedEvent.OnEnemyDefeated(expReward, transform)` 并 `Destroy(gameObject)`（:30-34）；`TakeDamage(int, Transform)` 先 `ChangeHealth(-damage)` 再调 `knockBack.Knockback(...)`（:40-51）。`defeatedEvent` 的订阅方是 `ExperienceController.GainExp`（`Player/Controllers/ExperienceController.cs:25,43`）。
- `EnemyKnockBack.Knockback`（:15-21）：写 `AnimatorSM(EnemyState.KnockBack)`、以 `(自身-攻击者)` 归一化方向乘击退力写 `rb.velocity`、起协程 `StunTimer(stunTime, knockBackTime)`；协程先等 `knockBackTime` 清零速度，再等 `stunTime` 回 `Idle`（:22-28）。参数顺序是 `(攻击者, 击退力, stunTime, knockBackTime)`，调用点按同序传参（`EnemyHealth.cs:47-49`）。
- 敌人被击退的数值**反向取自玩家数值**：`EnemyHealth.cs:47-49` 读 `StatsService.Instance.Stats.KnockBackForce / StunTime / KnockBackTime`（`Player/Models/IPlayerStatsReadOnly.cs:18-20`），资产现值 0.5 / 0.2 / 0.2（`Assets/GameSO/PlayerStatsSO.asset:18-20`）。这是本域唯一的"反向读玩家"耦合，见 §3.4。

### 2.3 NPC：互斥 enabled 切行为 + 触发切对话

- `NPCStateController` 持三个行为组件引用与 `DefaultState`（默认 `Patrol`，:14），`Start` 调 `SwitchState(DefaultState)`（:17-20）。`SwitchState` 的实现就是三行互斥赋值（:21-27）：`wanderScript.enabled = (state == Wander)`、`dialogTrigger.enabled = (state == Chat)`、`patrolScript.enabled = (state == Patrol)`；同一时刻只有一个行为组件被启用。
- 预制体里三个行为组件**出厂即 disabled**（`PurplePawn_NPC.prefab:388,406,423` 的 `m_Enabled: 0`），首次启用完全依赖 `SwitchState`；`DefaultState = 2` 对应 `MyEnums.NPCState.Patrol`（`Contracts/MyEnums.cs:39-45`，Idle=0/Wander=1/Patrol=2/Chat=3）。
- 玩家进入/离开触发区时按 tag 切换：`OnTriggerEnter2D` → `Chat`，`OnTriggerExit2D` → `DefaultState`（:28-41）。触发体是预制体根上的 `CircleCollider2D`（radius 1、`m_IsTrigger: 1`，`PurplePawn_NPC.prefab:318-352`），根物体另有一个非触发 `CapsuleCollider2D` 与 `Rigidbody2D`（:256-317）。
- `NPCWander` 与 `NPCPatrol` 有一个自陈的时序约定：`Rigidbody2D` 必须在 `Awake` 里取，因为 `OnEnable` 要用它设 `isKinematic = false`。原文注释在 `NPCWander.cs:31`："这里要早点获取组件，因为 Start 可能在 OnEnable 之后被调用，而 OnEnable 里需要用到 rb，避免空引用错误"；`NPCPatrol.cs:37-45` 同构。
- 三个行为组件各自在 `OnDisable` 里收尾：`NPCWander.cs:46-49` 与 `NPCPatrol.cs:66-69` 关 `isWalking`；`NPCDialogTrigger.cs:46-68` 退订通道、禁用输入动作、`isKinematic = false`、把交互图标播回 `Idle`、并强制结束对话。
- `NPCDialogTrigger` 是"Chat 态"的唯一实现（它的旧字段名是 `chatScript`，但 `FormerlySerializedAs` 不写在本文件里，而写在**引用它的一方** `NPCStateController.cs:11-12`：`[FormerlySerializedAs("chatScript")] [SerializeField] private NPCDialogTrigger dialogTrigger;`）：`OnEnable` 订阅 `toggleDialogEvent.toggleCanvasEvent`、把 `rb.velocity` 清零并置 `isKinematic = true`、`chatAnimator.Play("Chat")`、启用 `advanceDialogAction`（:26-44）；`Update` 在收到开对话请求且 `!DialogManager.Instance.isDialogActive` 时 `StartDialog(dialogSO)`（:92-100），对话激活期间按 `advanceDialogAction.WasPressedThisFrame()` 推进（:102-105）。`chatAnimator` 指的是"Interaction Icon"子物体上的控制器（`PurplePawn_NPC.prefab:142-152` → `Assets/Animation/Units/NPC/Interaction Icon.controller`，内含 `Chat`/`Idle` 两个状态）；NPC 本体动画在 `PurplePawn.controller`（含 `isWalking` 参数与 `Idle`/`Walk` 状态）。
- 坐标驱动：`NPCWander` 以 `Start` 时的位置为圆心（:39-40），半径 `patrolRadius`；`NPCPatrol` 以 `Start` 时位置为中心算四角（:114-125），`clockwise` 决定 `+1` 还是 `+3` 取模（:152-159）。两者都在到达目标点或"路点为零"时进等待协程（`NPCWander.cs:77-80`、`NPCPatrol.cs:102-105`）。

### 2.4 店主：OnTriggerEnter2D 广播通道，ShopManager 订阅

`ShopKeeper`（`ShopKeeper/ShopKeeper.cs`）对外只有一条接口——序列化的 `ShopKeeperEventSO` 通道：

- 玩家进入：`OnTriggerEnter2D` 先判 tag（:26），`shopKeeperEvent.RaiseShopKeeperEntered(this)`（:28），再把 `logoAnimator` 的 `playerInRange` 置 true（:30-31）。
- 玩家离开：`OnTriggerExit2D` 同构广播 `RaiseShopKeeperExited(this)`（:34-42）。
- 组件失活：`OnDisable` 再广播一次 `RaiseShopKeeperExited(this)`（:44-47），用于"店主随场景卸载/被销毁"时收口。
- 触发体是根物体上的 `CapsuleCollider2D`（size 5×5、`m_IsTrigger: 1`，`ShopKeeper.prefab:180-214`）；根物体层为 12（`NPC`）、tag 为 `Untagged`（`ShopKeeper.prefab:83-85`）。判据是**进入方**的 tag：`collider.CompareTag("Player")`（玩家 tag 的唯一来源是 `PersistentScene.unity:32287`）。
- 通道定义：`ShopKeeperEntered/ShopKeeperExited` 两个 `Action<ShopKeeper>` 事件（`Pipeline/SO/Events/ShopKeeperEventSO.cs:8-20`）；资产 `Assets/GameSO/Events/ShopKeeperEvent.asset`（guid `5b083df9b77ba3f4c877b0871e81e6bf`），预制体与订阅方引用同一份（`ShopKeeper.prefab:229`）。
- 订阅方只有两个，且都在 `PersistentScene`：`ShopManager`（`Assets/Scenes/GameScene/PersistentScene.unity:23597`，代码 `Gameplay/Shop/ShopManager.cs:41-42,60-71`）与 `ShopPortraitCamera`（`PersistentScene.unity:21804`，代码 `Gameplay/Shop/ShopPortraitCamera.cs:21-22,34-44`）。`ShopManager` 用 `activeShopKeeper` 记住当前店主（:62），退出时带 `activeShopKeeper != keeper` 防护并在开店状态下 `CloseShop()`（:65-71）。
- 商品数据只经只读属性暴露：`ShopItems/ShopWeapon/ShopArmor`（:16-18）；预制体里三个列表为空（`ShopKeeper.prefab:230-232`），实际内容由**场景实例覆写**（`Assets/Scenes/GameScene/Scene1.unity:281-311`：shopItems 2 条、shopWeapon 1 条价 50）。买卖结算、`IShopInteractable` 面板逻辑属 InventoryShop，本域不涉及。
- 实例分布（按 `m_SourcePrefab` 计数）：ShopKeeper 在 `Scene1` 2 个、`StartingMenu` 1 个；NPC 在 `Scene1` 2（紫）+1（黄）、`Scene2` 1（紫）、`StartingMenu` 1（紫）；敌人在 `TestScene` 62、`Scene2` 40、`StartingMenu` 4、`Scene1` 2。

### 2.5 移动约定：单位各自持 PathFollower，驱动同一组四个方法

`PathFollower`（`Pipeline/Pathfinding/PathFollower.cs`）是每个单位自己的组件，不是共享寻路器；敌人/NPC 的行为脚本经序列化字段 `pathFollower`（旧字段名 `aStarController`，`FormerlySerializedAs` 见 `EnemyMovement.cs:25-26`、`NPCWander.cs:14-15`、`NPCPatrol.cs:23-24`）引用同一物体上的实例（`TorchGoblin_Red.prefab:283` 指向同根 `fileID 4744295733257843321`；`PurplePawn_NPC.prefab:398,412` 指向同根 `fileID 7847235826292514568`）。

单位只用四个方法，调用节奏是硬约定：

| 方法 | 语义 | 单位侧用法 |
|---|---|---|
| `GetPosToGo(optPos, startPos, endPos)`（:21-44） | 返回当前栈顶路点的世界坐标；无路或栈空返回 `Vector3.zero`（:28、:41） | 每帧调用；`optPos` 只有敌人用（:130），NPC 一律传 `Vector3.zero` |
| `ArrivedPos()`（:46-52） | 弹出栈顶路点 | 距路点小于阈值时调用（`EnemyMovement.cs:143-146`、`NPCWander.cs:67-71`、`NPCPatrol.cs:91-95`） |
| `ResetPath()`（:53-59） | 清空路径、起点终点与 `hasValidPath` | 目标变更/放弃时调用（`EnemyMovement.cs:87`、`NPCWander.cs:41,104`、`NPCPatrol.cs:61,162`） |
| `GetThreshold()`（:60-72） | `threshold(0.5f) * AStarNodeManager.GetCellSize()`；`AStarNodeManager.Instance == null` 时返回 `0f`（:62-69） | `Start` 里取一次缓存；敌人用原值，两个 NPC 都乘 `0.2f`（`NPCWander.cs:43`、`NPCPatrol.cs:63`） |

`GetPosToGo` 内部还有一条重算规则：目标点相对上次寻路终点位移超过 `pathRebuildDistance(0.5) * cellSize` 且冷却（`0.5s`）已过时重算（:32-38、:8-9、:92-134）。返回值是"格心 + 避障推离量"（`AStarNodeManager.cs:50-95`），所以单位用半径比较而不是精确相等。

寻路链：`PathFollower` → `AStarPathFinder.Instance.FindPath`（:78-79、:103），`AStarPathFinder` 是 `AStarNodeManager` 的转发层（`AStarPathFinder.cs:15-19`），网格在 `AStarNodeManager.OnSingletonInitialized` 构建（`AStarNodeManager.cs:10-14`）。本轮 `AStarPathFinder` 的开放列表实现换成了二叉最小堆（新增 `Assets/Scripts/Pipeline/Pathfinding/AStarOpenHeap.cs`，`FindPath` 改为 `heap.Push/Pop` 并在弹出时按 G 丢弃过期项），但这属于寻路内部实现，对单位侧的三个方法签名与语义零影响：单位**不接触** `AStarDetails`/`AStarOpenHeap`/`ObjectsMapManager`。

## 3. 约定与硬边界

1. **`GetPosToGo` 返回 `Vector3.zero` 是"无路"，不是"目标在世界原点"**。三个行为脚本都写了 `posToGo == Vector3.zero` 分支（`EnemyMovement.cs:132`、`NPCWander.cs:61-63,77`、`NPCPatrol.cs:83-86,102`）；谁把"零向量"当成合法路点，谁就会得到静止 + 反复等待的行为。
2. **到点必须调 `ArrivedPos()`，换目标必须先 `ResetPath()`**。`PathFollower` 的路点是 `Stack`，只有 `ArrivedPos` 会弹栈（:48-51）；漏调则永远停在同一个路点上打转，多调则跨格跳过路点、到不了目标（`AStarNodeManager.cs:83-84` 注释警告：路点跨格时跟随者会在邻格 pop 掉本格节点，导致每帧重算路径且实体到不了目标）。
3. **`GetThreshold()` 依赖 `AStarNodeManager` 已 Awake**。网格管理器未就绪时它返回 `0f`（`PathFollower.cs:62-69`），单位侧的"小于阈值"判断恒为 false → 永不 `ArrivedPos`、永不换目标，表现为原地播放走路动画（§4 D2）。
4. **敌人被击退的数值来自玩家，方向来自攻击者**。`EnemyHealth.TakeDamage` 是唯一读 `StatsService` 的单位代码（`EnemyHealth.cs:47-49`），且**没有 null 守卫**：`StatsService` 单例未初始化（`Contracts/YSingleton.cs:13,15-25`，`Instance` 在 Awake 前为 null）时会 NRE。改玩家击退/硬直数值等价于改敌人手感，改敌人 `knockBackForce` 不生效（那是给玩家的值）。
5. **`EnemyMovement` 的 KnockBack 态是"全停"态**。进入后 `Update` 整段不执行（:70），必须由 `EnemyKnockBack.StunTimer` 协程在 `knockBackTime + stunTime` 后调回 `Idle`（`EnemyKnockBack.cs:22-28`）；协程所在协程体如果随物体销毁而中断，状态就永久停在 KnockBack。
6. **NPC 行为组件出厂是 disabled 的**，`NPCStateController.Start` 的 `SwitchState(DefaultState)` 是唯一的初始启用点（`NPCStateController.cs:17-20`、`PurplePawn_NPC.prefab:388,406,423`）。绕过 `SwitchState` 手改 `enabled` 会绕过互斥，出现两个行为脚本同时抢 `rb.velocity`。
7. **`NPCDialogTrigger` 在 Chat 期间把刚体设成 `isKinematic = true` 并写死 `velocity = 0`**（:32-36），退出时只恢复 `isKinematic = false`（:54-57）；它假设"下一个行为组件的 `OnEnable` 会接管速度"。新增行为组件时必须自己清速度。
8. **店主与玩家的接口只有 `ShopKeeperEventSO` 一条**（`ShopKeeper.cs:28,38,46`）。`ShopKeeper` 不实现 `IShopInteractable`（该接口由 `ShopManager` 实现，`Contracts/IShopInteractable.cs:8`、`Shop/ShopManager.cs:4`），因此"新增一个店"只需挂预制体 + 接同一个通道资产，不需要改代码；反过来，任何直接 `GetComponent<ShopKeeper>()` 的做法都会绕过 `activeShopKeeper` 的唯一性。
9. **商品列表按场景实例覆写，不在预制体上**。预制体三个列表为空（`ShopKeeper.prefab:230-232`），加店必须在场景实例里填（示例 `Scene1.unity:281-311`）；只改预制体会得到一家空店，且没有报错。

## 4. 已知缺陷与风险

- **D1** `NPCWander.WaitAndContinue` 的 `do { … } while (posToGo == Vector3.zero)`（:96-106）没有重试上限，而循环体是同步执行的：一旦该 NPC 在 `patrolRadius` 内所有随机点都取不到路径（例如 `AStarPathFinder.Instance` 为空导致 `GetPosToGo` 恒返回零向量（`PathFollower.cs:28,78-79`），循环永不退出，主线程卡死。`NPCPatrol` 的同类逻辑不含该循环（:144-166），所以这条风险只存在于 `NPCWander`。
- **D2** `PathFollower.GetThreshold()` 在 `AStarNodeManager.Instance == null` 时返回 `0f`（:62-69），并以 `Debug.LogWarning` 收尾（仅编辑器）。单位侧的阈值比较随之恒假 → `ArrivedPos` 永不调用，实体停在第一个路点且持续播走路动画。同一份代码在敌人与两个 NPC 上都这样用（`EnemyMovement.cs:66`、`NPCWander.cs:43`、`NPCPatrol.cs:63`）。
- **D3** `EnemyMovement.Start` 直接使用 `pathFollower`（:66）而不做空检查，配套的 `Chase` 里才有 `if (pathFollower == null) return;`（:119-120）。对照：`NPCPatrol.Start` 做了空检查并 `LogError` + 自禁用（:49-54）。漏接预制体引用时敌人会在 `Start` 抛 NRE，而不是给出可读错误。
- **D4** 击退数值双向来源不对称：敌人打玩家用**敌人自身**序列化的 `knockBackForce 2 / stunTime 0.2`（`EnemyCombat.cs:23`、`TorchGoblin_Red.prefab:260-261`），玩家打敌人用**玩家**的 `KnockBackForce 0.5 / StunTime 0.2 / KnockBackTime 0.2`（`EnemyHealth.cs:47-49`、`PlayerStatsSO.asset:18-20`）。调"击退手感"时改哪一个生效取决于方向，没有单一旋钮。
- **D5** `EnemyHealth.ChangeHealth` 在血量归零时先广播再 `Destroy(gameObject)`（:32-33），而 `TakeDamage` 在它之后继续调 `knockBack.Knockback(...)`（:45-50）。`Destroy` 延迟到帧末，所以死亡当帧仍会设 `AnimatorSM(KnockBack)` 并起协程，协程随对象销毁静默中断——功能上无害，但"死亡瞬间进入击退动画"是可见的。
- **D6** NPC 的 Chat 切换只依赖 tag，没有计数/去重（`NPCStateController.cs:28-41`）。玩家同时压在两个 NPC 的触发圈里时，两个 NPC 都会进 Chat 并各自 `StartDialog` 竞争（`NPCDialogTrigger.cs:96-99` 只做 `!isDialogActive` 判断）；退出顺序决定谁恢复到 `DefaultState`，存在状态互踩。
- **D7** 敌人在 KnockBack 态整段跳过 `Update`（`EnemyMovement.cs:70`），因此 `attackCoolDownTimer`（:79-80）与侦测计时（:72-77）在硬直期间全部冻结；连续击退会延长冷却恢复时间。
- **D8** 旧 UML 与代码冲突：`Docs/UML/03_Enemy_NPC_System.puml` 记为 `NPCChat`（实为 `NPCDialogTrigger`，:97）、`MovementController`（实为 `PathFollower`，:36,84,93,116,126-127）、`EnemyCombat.konckBackForce`（实为 `knockBackForce`，:52）、`EnemyHealth.OnDefeated : MonsterDefeated`（实为序列化 `EnemyDefeatedEventSO`，:61）、`EnemyCombat --> PlayerHealth/PlayerMovement` 与 `EnemyHealth --> ExpManager`（:117-120，代码里敌人不引用玩家组件也不引用 ExpManager，且 `ExpManager`/`MonsterDefeated`/`PlayerHealth`/`NPCChat`/`MovementController` 五个类名在 `Assets/**/*.cs` 中零命中）；`Docs/开发日志.txt:51` 与 `Docs/UML/06_Dialog_Inventory.puml:136,182` 仍称 `ShopPortraitCameraController`，实际类名是 `ShopPortraitCamera`（`Shop/ShopPortraitCamera.cs:4`）。

## 5. 未核验事项

- 假设：`NPCStateController.Start` 的 `SwitchState(DefaultState)` 早于玩家可能进入触发区（未运行 Unity 验证；若玩家出生点与会话起点重叠，首帧行为取决于 Unity 的组件启用顺序）。
- 假设：预制体里 `m_Enabled: 0` 的行为组件在 `SwitchState` 首次启用时，其 `Awake` 会先于 `OnEnable` 执行（`NPCWander.cs:31` 注释这样假定；未运行 Unity 验证 Unity 对"物体激活但组件禁用"的 Awake 时机）。
- 假设：`StatsService.Instance` 在敌人首次 `TakeDamage` 时已就绪（未运行 Unity 验证；代码在 `EnemyHealth.cs:47-49` 无空守卫）。
- 假设：`TorchGoblin_Red.prefab` 的 `EnemyMovement.animator`、`detectionPoint`、`attackPoint` 与两个 NPC 预制体的 `animator`/`chatAnimator` 引用均无丢引用（仅按 prefab 内 `fileID` 交叉比对，未在编辑器打开确认）。
- 假设：敌人 `Attack()` 的调用来自动画事件而非代码（`Assets/Scripts/Gameplay/Units/Enemy/**` 内无 `Attack()` 调用点；未核验 `TorchGoblin_Red.controller` 或动画剪辑里的事件绑定）。
- 假设：店主预制体在 `Scene1` 与 `StartingMenu` 的实例都用预制体自带的事件资产（未逐一打开实例确认覆写；仅确认预制体字段引用 guid `5b083df9b77ba3f4c877b0871e81e6bf`）。
- 假设：`Physics2D` 层碰撞矩阵允许敌人 `playerMask`/`playerLayer`（层 8）与玩家层相交（未核验 `ProjectSettings/Physics2DSettings.asset` 的碰撞矩阵）。
- 说明：`Docs/My_ARPG_重构优化清单_已解决.md` 与 `Docs/My_ARPG_重构优化清单_未解决.md` 已由用户在 2026-10-05 删除（内容见 `git HEAD:Docs/My_ARPG_重构优化清单_已解决.md` / `git HEAD:Docs/My_ARPG_重构优化清单_未解决.md`）。本 Guide 的结论全部取自工作区代码与资产，不再以这两份文件为证据。
