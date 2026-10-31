# 红点框架方案：树形计数红点与接入规范

状态：`草稿（未实施）`
最后更新：`2026-10-19`
文档性质：方案草稿，存放在非权威参考区 `Docs/Plans/`，**不属于** `Y_MultipleAgentWorkflow/` 的权威文档；未落地前不参与该工作流的路由与维护计数。
适用范围：红点的**节点模型、父子汇总语义、变更广播、视图接入规范、测试与验收**。红点的业务条件语义（什么算未读、什么算可领）归各自玩法域；画布开关、焦点栈与层级排序归 Architecture 与 Assets 域；邮件的下发与过期规则归邮件域。工程内目前尚无任何红点实现。
引用约定：文中 `Xxx_Guide` 与 `Xxx_Guide §n` 均指 `Y_MultipleAgentWorkflow/` 下的权威文档。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `红点`、`角标`、`badge`、`未读` | §2 现状、§3 设计 |
| `红点树`、`父子汇总`、`计数` | §3.3 拓扑、§3.4 语义 |
| `新增红点`、`接入红点`、`RedDotKey` | §3.3、§6.1 |
| `红点不显示`、`红点不消失`、`翻页后丢了` | §3.6 三个坑 |
| `要不要存档`、`已读`、`跨档` | §4.5 |
| `为什么不用 SO 事件通道` | §3.7 |
| `面试怎么讲`、`为什么这么设计` | §7 |

## 2. 现状取证

### 2.1 红点：全仓 0 处

对 `Assets/Scripts`、`Assets/Editor`、`Assets/Tests` 全量检索 `RedDot|RedPoint|Badge|Unread|Notice`，命中 **0**。工程内不存在任何角标组件、红点服务或通知数据。

所以本域不是「接一套已有框架」，而是**新建**。但落点与宿主全部现成，见 §2.2。

### 2.2 可直接复用的既有事实

| 事实 | 证据 | 对本方案的意义 |
|---|---|---|
| 关键单例全部在常驻场景 | `UIManager`、`QuestManager`、`IntegratedUICanvasManager`、`StatsService`、`SkillTreeManager` 的脚本 GUID 在 `Assets/Scenes/GameScene/PersistentScene.unity` 中命中 | 红点服务与视图常驻，**无需处理跨场景生命周期** |
| 枚举即身份是既有惯例 | `MyEnums.CanvasToToggle`（`Assets/Scripts/Contracts/MyEnums.cs:56-72`），`UIManager.FindToggleEvent` 按值匹配（`UIManager.cs:185-194`） | `RedDotKey` 用枚举而非字符串，与既有做法一致 |
| 纯 C# 逻辑层有先例与测试范式 | `CanvasFocusStack` 为纯 C#，配 `CanvasFocusStackTests`（15 例） | 红点树做成纯 C#，可同样拿 EditMode 测试 |
| 事件驱动刷新是既有风格 | `StatsService.Stats` 暴露 4 个事件（`IPlayerStatsReadOnly.cs`），`SkillSlot` 用静态事件 | 红点变更走 C# 事件，不引入新机制 |
| 集成面板是分页按钮网格 | `IntegratedUICanvasManager.InitiatePage`（`IntegratedUICanvasManager.cs:121-149`），`integratedButtons[i]` 对应 `canvasToToggle[startNum + i]` | 红点的天然落点，父子层级天然对应页面→按钮 |
| 单例基类与存档身份 | `YSingleton<T>`（`YSingleton.cs`）、`SaveableService<TSelf>` | 服务继承前者；需要存档时继承后者 |

## 3. 设计

### 3.1 数据流

```
Gameplay（条件源）              Pipeline/RedDot（基建）             UI（落点）
─────────────────              ──────────────────────             ──────────
SkillPointRedDot ──┐           RedDotService (YSingleton)         RedDotView ×N
QuestRedDot      ──┼ SetCount → RedDotTree（纯 C#，父级求和）→ 事件 → 显隐 + 数字
MailRedDot(阶段4) ─┘           RedDotConfigSO（父子登记，启动期自检）
```

单向：条件源只写**叶子**计数；框架负责向上汇总；视图只读，不回写。

### 3.2 类型清单

**新增 5 个**（目录 `Assets/Scripts/Pipeline/RedDot/`）

| 文件 | 职责 | 是否依赖 MonoBehaviour |
|---|---|---|
| `RedDotNode.cs` | `Key`、`SelfCount`、`Children`、`Parent`、`Count`、`IsOn` | 否 |
| `RedDotTree.cs` | 注册、置数、向上重算、产出**去重后的**变更列表 | 否 |
| `RedDotService.cs` | `YSingleton`；`SetCount` / `GetCount` / `IsOn` / `event Changed` | 是（单例宿主） |
| `RedDotView.cs` | `[SerializeField] RedDotKey key; GameObject dot; TMP_Text countText;` | 是 |
| `RedDotConfigSO.cs` | `List<{ RedDotKey key, RedDotKey parent }>`；启动期建树 + 自检 | 否（资产） |

**改动 4 处**

| 位置 | 改动 | 阶段 |
|---|---|---|
| `Assets/Scripts/Contracts/MyEnums.cs` | 追加 `enum RedDotKey` | 1 |
| `Assets/Scripts/Gameplay/Player/Models/PlayerStatsModel.cs:129` | 补发 `SkillPointsChanged` | 2 |
| `Assets/Scripts/Gameplay/Player/Models/IPlayerStatsReadOnly.cs` | 追加 `event Action SkillPointsChanged` | 2 |
| `Assets/Scripts/Gameplay/Quest/QuestManager.cs:242` | 方法末尾广播 `QuestStateChangedEvent` | 3 |

### 3.3 `RedDotKey` 与父子拓扑

```csharp
namespace MyEnums
{
    public enum RedDotKey
    {
        Mail,          // 父：未读 + 可领 求和
        MailUnread,    // 子：未读数
        MailClaimable, // 子：可领取数

        SkillPoints,   // 叶：可分配技能点 > 0
        QuestClaimable,// 叶：存在可交付任务
    }
}
```

放在 `Contracts` 层的理由是对齐 `Layering_Guide §2.1` 的准入标准：**被两个以上域直接引用，且不含具体业务规则**——Gameplay 侧写条件、Pipeline 侧写视图，两个域都要引用它。

父子关系**不进代码**，进 `RedDotConfigSO`，理由是照抄 `UIManager.toggleCanvasEvents` 的登记方式（`UIManager.cs:15-18`）：新增红点不改代码，且启动期能做结构自检。

### 3.4 计数语义：为什么是 `int` 不是 `bool`

红点最常见的形态是「有/无」，但真实需求里必然出现「3 封未读」「12 件可领」。计数天然覆盖布尔（`Count > 0`），而布尔无法反向表达计数。所以节点存 `int SelfCount`，`IsOn => Count > 0`，父节点 `Count = SelfCount + Σ children.Count`。

视图层由此得到两种表现：只有圆点（当 `Count == 1` 且配置为不显示数字），或圆点 + 数字。**这一条是后面邮件双层红点的前提**——父节点 `Mail` 要显示"未读 + 可领"的合计。

### 3.5 变更传播与去重

```csharp
// 伪代码：只广播"汇总值真的变了"的节点
void SetCount(RedDotKey key, int count)
{
    var node = nodes[key];
    if (node.SelfCount == count) return;        // 同值不入队
    node.SelfCount = count;

    for (var n = node; n != null; n = n.Parent)
    {
        int before = n.CachedCount;
        n.CachedCount = Recalc(n);
        if (before == n.CachedCount) break;      // 父级没变，更上层必然没变
        changed.Add((n.Key, n.CachedCount));
    }
    foreach (var (k, c) in changed) Changed?.Invoke(k, c);
}
```

三点设计意图：

1. **同值短路**：条件源经常在每帧或每次事件里重复写同一个值，不短路会让 UI 每秒刷新几十次。
2. **向上提前终止**：父级汇总未变时，更上层不可能变，避免无意义的全链重算。
3. **广播与计算分离**：先算完再统一广播，避免订阅者在重算中途读到中间态。

### 3.6 视图接入规范与三个已验证的坑

红点挂在 `IntegratedUICanvasManager` 的分页按钮上（`canvasToToggle` 列表加入新面板即自动出现按钮）。三个坑都来自实测代码，不是推测：

| 坑 | 证据 | 规范 |
|---|---|---|
| **分页会反复 `SetActive`** | `IntegratedUICanvasManager.cs:128` 每次 `InitiatePage` 先 `button.gameObject.SetActive(false)`；红点作为子节点会跟着 `OnDisable/OnEnable` | `RedDotView.OnEnable` 必须**先向服务重读一次**当前值，不能只用缓存的事件值 |
| **`onClick` 会被清空** | 同文件 `:129` `button.onClick.RemoveAllListeners()` | 禁止用「点击清红点」实现；红点只由条件源驱动熄灭 |
| **Key 重复静默失效** | `UIManager.FindToggleEvent` 取列表中第一个匹配项（`:185-194`），同值后者的广播永远收不到 | `RedDotConfigSO` 启动期校验 Key 唯一、父节点存在、无环；重复直接 `LogError` |

### 3.7 为什么广播不走 SO 事件通道（一次刻意的偏离）

工程有成熟的 SO 事件通道惯例：12 个通道类、28 个资产，规则见 `Assets/EventChannels_Guide.md`（即 `Y_MultipleAgentWorkflow/Assets/EventChannels_Guide.md`）。本方案**仍然不新增通道资产**，理由：

- SO 通道的组织方式是「一个资产 = 一条语义独立的广播线」（`VoidEventSO` 6 个资产各管一件事）。红点则是「单通道 + 多 Key」的稀疏广播，照搬会变成「每加一个红点建一个资产」。
- SO 通道的核心价值是**让跨场景/跨预制体的接线在 Inspector 里可见**。红点的条件源、服务、视图全部常驻 `PersistentScene`，不存在这个需求。
- 代价：失去 Inspector 可读的事件接线。缓解手段是 `RedDotConfigSO` 把「有哪些 Key」变成可视资产。

这是一个有理由的偏离，不是疏忽——面试被问到时按此回答。

## 4. 落地步骤

### 4.1 阶段 1：框架骨架（不接任何业务）

1. 写 `RedDotNode` / `RedDotTree`（纯 C#）、`RedDotService`、`RedDotView`、`RedDotConfigSO`。
2. `MyEnums.RedDotKey` 先只放 `SkillPoints` / `QuestClaimable` 两个叶子。
3. `PersistentScene` 挂 `RedDotService`，配一个 `RedDotConfigSO` 资产。
4. 写 `Assets/Tests/Editor/RedDotTreeTests.cs`。
5. 验证：`RedDotService` 在 Play 模式下 `SetCount` 能打出广播日志；测试全绿。

**本阶段零业务接线，可独立交付。**

### 4.2 阶段 2：技能点试点（含一处必须动的契约）

**条件**：`StatsService.Instance.Stats.SkillPoints > 0`。

**拦路点（已核实，不能绕过）**：`UpdateSkillPoints` 目前**故意不广播任何事件**——`PlayerStatsModel.cs:128` 注释写明「技能点变化不触发事件，界面由 SkillTreeManager 刷新」，且 `PlayerStatsModelTests.cs:145` 的 `UpdateSkillPoints_AccumulatesWithoutEvents` 明确断言 `statsChangedCount == 0`。

因此**不能**给 `StatsChanged` 补广播：`StatsChanged` 的语义是「攻/防/速面板数值变化」，订阅方是 `StatsPanelController`（`:19`），复用会让语义失真，并且打红既有测试。

**正确做法**：新增**专有事件**。

1. `IPlayerStatsReadOnly` 加 `event Action SkillPointsChanged;`
2. `PlayerStatsModel.UpdateSkillPoints` 由表达式体改为语句体，末尾 `SkillPointsChanged?.Invoke();`
3. 测试改名 `UpdateSkillPoints_FireSkillPointsChangedOnly`，断言新事件计数 == 2、`statsChangedCount == 0`（保持原语义不被污染）。
4. 新增 `Assets/Scripts/Gameplay/Skills/SkillPointRedDot.cs`：订阅 `Stats.SkillPointsChanged`，写入 `RedDotService.SetCount(RedDotKey.SkillPoints, points > 0 ? points : 0)`，挂 `PersistentScene`。
5. 在集成面板 Skills 按钮下加圆点节点，挂 `RedDotView`，`key = SkillPoints`。

**已知分层瑕疵（本轮不改，记录备查）**：升级发放技能点的规则挂在视图层——`SkillTreeManager.cs:36` 订阅 `LevelUp` 后调 `UpdateAbilityPoints`。因 `SkillTreeManager` 与 `StatsService` 同在常驻场景，当前不会漏发；但这是「规则在视图」的味道，日后若技能树改成按需加载就会漏发。改它属独立任务，不在本阶段范围。

### 4.3 阶段 3：任务试点

**条件**：存在 `questState == QuestState.IsToComplete` 的任务。

**有利事实**：所有状态迁移都收敛在 `QuestManager.QuestStateChanged`（`QuestManager.cs:242`），调用点仅 4 处（`:59`、`:127`、`:131`、`:174`）。因此只需在方法末尾广播一次：

```csharp
public event Action<QuestSO, MyEnums.QuestState> QuestStateChangedEvent;
// QuestStateChanged 末尾：
QuestStateChangedEvent?.Invoke(quest, currentQuestState);
```

新增 `Assets/Scripts/Gameplay/Quest/QuestRedDot.cs`：订阅该事件，遍历 `questProgress` 重算「是否存在可交付」，写入 `RedDotKey.QuestClaimable`。

注意：`questProgress` 不落盘（`SaveData` 只有 3 个字段，见 `SaveData.cs`），所以任务红点天然是运行时态，读档后归零，这符合预期。

### 4.4 阶段 4：邮件双层红点（下一阶段，依赖热更的 JSON 下发）

只写约定，实现归邮件域：

- 叶子：`MailUnread`、`MailClaimable`；父节点 `Mail` 由树自动求和。
- JSON 下发复用热更的远端目录与独立版本号（见同目录 `HotUpdate_Plan.md` §4.3）。
- 过期按**存档累计在线时长**判定，不用 `DateTime.UtcNow`：本工程存档是本地 JSON，无可信服务器时间，用设备时间可被直接改写绕过。累计时长需要新增 `PlayTimeTracker`——现有 `TimeManager`（`Assets/Scripts/Pipeline/TimeManager.cs`）只有引用计数的 `PauseGame/ResumeGame`，没有累计计时。
- `SaveData` 新增字段是向后兼容的（旧档缺字段反序列化为 `null`）；**已有字段名不可改**（`Layering_Guide §3.6`：字段名就是存档 JSON 键）。

### 4.5 存档策略：派生条件不存，已读才存

| 类型 | 例子 | 是否落盘 | 理由 |
|---|---|---|---|
| 派生条件 | 技能点 > 0、任务可交付 | 否 | 由运行时状态唯一推导，落盘反而会产生「存档与状态不一致」 |
| 已读标记 | 新物品「已点开过」 | 是 | 无法从状态推导 |

需要落盘时用 `SaveableService<TSelf>` 子类（`Layering_Guide §2.6`），并且**重写 `OnSingletonInitialized` 必须调 `base()`**，否则会被 `SaveRegistry` 静默跳过（`§3.3`）。

## 5. 验收与测试

### 5.1 EditMode 测试（`Assets/Tests/Editor/RedDotTreeTests.cs`）

| 用例 | 断言 |
|---|---|
| 叶子置数广播 | `SetCount(leaf, 3)` → 收到 1 次 `Changed`，`Count == 3` |
| 同值短路 | 连续两次 `SetCount(leaf, 3)` → 只广播 1 次 |
| 父级求和 | 两个子节点 2 + 3 → 父 `Count == 5` |
| 父级只广播一次 | 同时改两个子节点 → 父节点只广播 1 次 |
| 归零熄灭 | 子节点全部置 0 → 父 `IsOn == false` |
| 冲突登记 | 重复 Key 注册 → 抛错或返回失败（不许静默覆盖） |
| 环检测 | A→B→A → 注册失败 |

对标 `CanvasFocusStackTests` 的粒度（15 例）：**纯 C# 层必须有独立测试**，这是 `Layering_Guide §3.7` 的直接推论。

### 5.2 手工验收

1. 技能点 > 0 → 集成面板 Skills 按钮亮；花完 → 灭。
2. 任务转 `IsToComplete` → Quest 按钮亮；交付 → 灭。
3. **翻页来回切两次**，红点状态不丢（验 §3.6 第一个坑）。
4. 打开面板再关闭、切场景再回来，红点仍正确（验 `OnEnable` 重读）。
5. 存档 → 读档，派生红点按新状态重算，不残留。

## 6. 约定与硬边界

### 6.1 Key 唯一，且枚举插在 `Default` 之前

`MyEnums.cs:70-71` 的注释已规定新画布枚举必须加在 `Default` 之前。`RedDotKey` 若与其他枚举共用同名成员同样会编译失败——全局命名空间是共用的（`Layering_Guide §3.2`），新类型重名会在无关文件报错。

### 6.2 视图不得用 `onClick` 清红点

见 §3.6 第二条。红点是**状态的投影**，不是可手动清除的标记。

### 6.3 纯 C# 层不得引用 MonoBehaviour 生命周期

`RedDotNode` / `RedDotTree` / `RedDotConfigSO` 必须可在 `new` 之后直接测试（`Layering_Guide §3.7`）。把 `Update` 驱动的轮询塞进树里会同时破坏可测性与性能。

### 6.4 订阅必须成对

视图 `OnEnable +=` / `OnDisable -=`，与 `EventChannels_Guide §3.1` 一致。`RedDotService` 是常驻单例，视图销毁后退订才是唯一防止悬挂委托的手段。

### 6.5 本域不定义业务条件

「技能点 > 0 算红点」是技能域的规则，「有可交付任务算红点」是任务域的规则。本域只提供表达能力与视图规范，避免红点框架变成第二个业务逻辑中心。

## 7. 面试讲解稿

### 7.1 30 秒版本

> 自研了一套树形红点：节点存的是**计数**而不是布尔，父节点自动汇总子节点；条件源只写自己那一个叶子，UI 只读节点值，两边靠一次**去重的变更广播**连接。特点是任何系统按枚举 Key 接入、不用改框架代码；框架本身是纯 C#，可以脱离 Unity 做单元测试。

### 7.2 五个关键取舍

| 取舍 | 常见做法 | 本方案 | 为什么 | 代价 |
|---|---|---|---|---|
| 计数 vs 布尔 | `bool hasNew` | `int` 计数 | 邮件要显示「3 封未读」，商店要显示「12 件可领」；布尔无法升级为计数 | 视图要处理数字格式化 |
| 树形 vs 扁平 | 每个入口各自算 | 父子树 + 自动汇总 | 红点天然层级（入口→面板→条目）。扁平化会把「汇总」推给每个条件源，耦合爆炸 | 需要一份父子登记 + 启动期自检 |
| 枚举 Key vs 字符串 | `"mail_unread"` | `MyEnums.RedDotKey` | 字符串打错不报错、运行期静默失效；枚举能编译期兜住，还能被自检遍历 | 新增 Key 要改代码（换来的是安全） |
| C# 事件 vs SO 通道 | 复用工程 28 个 SO 资产 | 专用 C# 事件 | 红点是单通道多 Key 的稀疏广播；SO 资产适合「一资产一语义」，且其价值（跨场景可视化接线）在常驻场景里不存在 | 失去 Inspector 可读性，用 `RedDotConfigSO` 部分补回 |
| 广播去重 vs 直接刷新 | 每次置位都 `Refresh()` | 汇总值不变则不广播，向上提前终止 | 条件源常在每帧/每事件重复写同值，不去重会打爆 UI | 多一层缓存值，需要维护一致性 |

### 7.3 常见追问

**Q：红点要不要存档？**
A：分两类。派生条件（技能点>0、有任务可交付）不存，它是状态的函数，存了必然出现「存档说亮、状态说灭」；「已读」这类无法推导的才存，而且要明确存档级还是账号级——本工程单机存档，选存档级。

**Q：红点会不会泄漏内存？**
A：服务的生命周期与场景同长，泄漏只可能来自视图。所以规范是 `OnEnable/OnDisable` 成对订阅，这也是工程 SO 通道既有的规则（`EventChannels_Guide §3.1`）。

**Q：为什么不用 `UnityEvent`？**
A：`UnityEvent` 走反射/序列化，性能与调用顺序不可控，且不能被纯 C# 测试直接验证。用 `Action` 委托 + 纯 C# 树，测试里 `new` 出来就能断言。

**Q：翻页/重建 UI 后红点为什么会丢？**
A：这是本工程实测到的坑——集成面板每次翻页会把按钮 `SetActive(false)` 再启用，红点作为子节点会经历 `Disable/Enable`。如果只在事件里更新，那次 `Disable` 之后就再也没人通知它。所以规定 `OnEnable` 必须主动向服务重读一次当前值。

**Q：条件源怎么知道该写哪一层？**
A：永远只写叶子。父节点由框架算。这条如果破了，红点就会变成「多人抢着算同一个数」，是这类系统最常见的腐化方式。

**Q：新增一个红点要改几处？**
A：加一个枚举成员 + 在 `RedDotConfigSO` 里登记父子关系 + 条件源写一行 `SetCount` + UI 挂一个 `RedDotView`。**框架代码零改动**——这是判断这套设计是否成功的标准。

### 7.4 不能吹的部分（诚实边界）

- 目前是 **Proposal**，代码尚未落地。对外只说「设计并规划了」，不要说「实现了」，除非阶段 1 与阶段 2 各自留下验证证据。
- 没有做「红点排序/优先级」「红点动画」「跨端账号同步」，这些都不在范围内。
- 没有做性能压测。节点规模在数十级时不构成瓶颈；上千节点需要另议（届时可加脏标记 + 分帧广播）。
