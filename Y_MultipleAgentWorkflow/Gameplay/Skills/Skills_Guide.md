# 技能树权威指南：Gameplay.Skills

文档 ID：`GP-SKILLS-GUIDE`
状态：`Active`
最后更新：`2026-11-01`
权威范围：本域负责技能槽位状态与解锁依赖、技能点收支、技能效果分发这条链路；技能点的存取字段与升级事件归 `Gameplay.PlayerStats`，武器在战斗中的实际行为归战斗域的 `PlayerCombat` 与 `ShiftEquipment`，面板显隐基建归 UI 域的 `ICanvasManager` 与 `UIManager`。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `技能树` / `技能面板` / `SkillTree` | §2.1、§2.3、§2.4 |
| `技能点` / `SkillPoints` / `pointsText` | §2.2 |
| `解锁` / `isUnlocked` / `CanUnlockSkill` / `前置` | §2.1 |
| `升级技能` / `TryUpgradeSkill` / `maxLevel` | §2.1 |
| `SkillSlot` / `MaxHealthBoost` / `CombatUnlock` / `技能没效果` | §2.3、§2.4 |
| `SkillSO` / 技能改名 | §3.2 |
| `OnAbilityPointSpent` / `OnMaxSkillLevel` | §2.3、§3 |

## 2. 当前实现

### 2.1 技能树数据结构与解锁条件

- 配置类型 `SkillSO` 只有 `skillName`、`maxLevel`、`skillIcon` 三个字段，`maxLevel` 默认为 5。它的标识来自技能名与资产名；稳定 id 由 `GuidSO` 提供，当前使用者是 `CharacterSO`。
- 技能运行状态由 `SkillTreeManager` 持有，槽位按相对画布根节点的路径登记等级与解锁状态。`SkillSlot` 持有配置、前置槽位引用和显示副本，预制体实例重建时重新拉取已有状态。
- 解锁条件：`CanUnlockSkill` 要求前置列表中每个槽位都 `isUnlocked`，并且 `currentLevel` 达到该槽位 `skillSO` 的 `maxLevel`；前置列表为空时结果为 true。解锁动作只有 `Unlock()` 一个入口。
- 升级规则：`TryUpgradeSkill` 要求 `isUnlocked` 为真且 `currentLevel` 小于 `skillSO` 的 `maxLevel`，随后 `currentLevel` 自增、广播 `OnAbilityPointSpent`，满级再广播 `OnMaxSkillLevel`，最后 `UpdateUI`。技能点的扣减由订阅方完成，见 §2.2。
- 两个 static 事件是唯一的状态广播通道。`UpdateUI` 按解锁态切换按钮可交互、文本与图标颜色：未解锁显示 `Locked`，已解锁显示当前等级与上限。
- `SkillsCanvasView` 的槽位数组、技能点文本以及各槽位的前置依赖均在预制体上序列化接线；管理器通过稳定的 `CurrentState` 提供技能点、脏标记和按键读取技能槽位状态。视图按需查询并在绘制后调用 `State.ClearDirty()`，不创建刷新快照。
- 初始解锁态：技能按钮预制体的 `isUnlocked` 默认为 0，前置列表为空，`skillSO` 为空引用；场景里有 3 个实例把 `isUnlocked` 设为 1，分别是 `MaxHealthBoost` 两个与 `CombatUnlock` 一个。技能树从预先解锁的根往外生长。
- 技能等级与解锁状态不入档：`SaveData` 顶层字段里没有技能相关字段，技能点数随 `PlayerStatsData.skillPoints` 入档。

### 2.2 技能点来源与收支

- 技能点属于玩家数值域：字段是 `PlayerStatsModel.skillPoints`，写命令 `UpdateSkillPoints` 不广播事件，经 `StatsService.UpdateSkillPoints` 转发。
- 来源是升级：`PlayerStatsModel.AddExp` 结算完升级后广播 `LevelUp(levelsGained)`。
- 发放：`SkillTreeManager.UpdateAbilityPoints(amount)` 调 `StatsService.UpdateSkillPoints(amount)`；技能点文本由 `SkillsCanvasView` 读取 `State.AbilityPointsValue` 刷新，管理器不碰控件。一次升多级就发多点，读档不补发。
- 支出：点击槽位由 `SkillSlot` 自己在 `OnEnable` 绑定 `TryUpgradeSkill`；升级成功广播 `OnAbilityPointSpent`，`SkillTreeManager.HandleAbilityPointSpent` 再查一次余额并 `UpdateAbilityPoints(-1)`。
- 管理器在 `Start` 通过 `RefreshAbilityPoints()` 同步当前技能点，升级事件与扣点回调通过 `UpdateAbilityPoints` 写入数值并更新 State。技能点变化本身不发事件，跳过管理器直接写 `StatsService` 会让 State 中的点数变旧。

### 2.3 SkillTreeManager 的职责

`SkillTreeManager` 订阅 `SkillSlot.OnAbilityPointSpent` 与 `PlayerStatsModel.LevelUp`，负责点数收支，并按 `SkillState` 保存各槽位的等级与解锁状态；技能点及脏状态由 `State` 提供只读查询和标记清理方法，文本与按钮由视图负责。订阅在 `OnEnable`，退订在 `OnDisable`。

- 满级解锁：`SkillsCanvasView` 在 `OnOpen` 订阅 `SkillSlot.OnMaxSkillLevel`，在 `OnClose` 退订；其 `HandleSkillMaxed` 遍历全部槽位，把所有未解锁且前置满足的槽位一次性 `Unlock()`，按前置图整体推进。
- 可重试订阅：`StatsService` 是 `Awake` 里注册的单例，`SkillTreeManager.OnEnable` 可能先于它执行，此时 `StatsService.Instance` 为 null。订阅因此写在 `TrySubscribeLevelUp()` 里，用 `listeningToLevelUp` 标记幂等，并在 `OnEnable` 与 `Start` 各调一次。`UnsubscribeLevelUp` 先用标记短路，`StatsService.Instance` 为空时直接返回。

### 2.4 界面与状态同步

`SkillsCanvasView` 由 `UICacheManager` 管理实例生命周期，开关、焦点与场景切换复位由缓存与 `UIManager` 协同处理。打开时遍历槽位调 `RestoreState` 恢复 `currentLevel` 与 `isUnlocked`；槽位在升级、解锁与界面销毁前调 `SaveSkillState` 把状态写回管理器。技能点文本和槽位按需读取 `CurrentState`，刷新后清理 State 脏标记。

## 3. 约定与硬边界

### 3.1 扣点发生在事件订阅方

任何广播 `OnAbilityPointSpent` 的路径都依赖 `SkillTreeManager` 处于激活且已订阅；它失活时升级照常加等级，技能点保持不变。

### 3.2 状态键由层级路径决定

`SkillSlot.GetStateKey()` 从槽位向上走到画布根，逐级记录节点名与同级序号，据此生成状态键。调整槽位层级或改名会让旧状态对不上新键，等级与解锁状态回落到预制体初值。

### 3.3 两个 static 事件必须成对退订

`OnAbilityPointSpent` 与 `OnMaxSkillLevel` 都是 static。`SkillTreeManager` 在 `OnDisable` 退订扣点事件，`SkillsCanvasView` 在 `OnClose` 退订满级事件；Domain Reload 关闭时残留订阅会跨局呼叫已销毁对象。

### 3.4 根技能由场景预先解锁

`isUnlocked` 是序列化字段，技能按钮预制体默认为 0，初始解锁来源是场景与预制体实例上的赋值，代码里没有开局解锁根节点的逻辑。

### 3.5 前置条件要求前置槽位满级

`CanUnlockSkill` 要求前置槽位的 `currentLevel` 达到其 `maxLevel`；只解锁不点满不会放行后续节点。

### 3.6 同一个 `SkillSO` 可被多个槽位复用且各自计数

`currentLevel` 在槽位上，场景里 23 个槽位共用 `MaxHealthBoost`，因此等级上限按槽位各自计算。

### 3.7 技能等级与解锁不入档，技能点入档

读档后技能树回到场景接线初值，技能点恢复存档值。

### 3.8 `UpdateAbilityPoints` 是点数文本的唯一刷新入口

绕过它直接写 `StatsService.UpdateSkillPoints` 会让 `pointsText` 与实际值不一致。

### 3.9 按钮回调随槽位启停成对管理

`SkillSlot.OnEnable` 使用 `AddListener(TryUpgradeSkill)` 绑定按钮，`OnDisable` 使用 `RemoveListener(TryUpgradeSkill)` 移除同一回调，槽位重新启用时重新绑定。
