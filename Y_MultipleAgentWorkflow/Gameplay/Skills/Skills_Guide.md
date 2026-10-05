# 技能树权威指南：Gameplay.Skills

文档 ID：`GP-SKILLS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
权威范围：本域负责技能槽位状态与解锁依赖、技能点收支、技能效果分发这条链路；技能点的存取字段与升级事件归 `Gameplay.PlayerStats`，武器在战斗中的实际行为归战斗域的 `PlayerCombat` 与 `ShiftEquipment`，面板显隐基建归 UI 域的 `ICanvasManager` 与 `UIManager`。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `技能树` / `技能面板` / `SkillTree` | §2.1、§2.3 |
| `技能点` / `SkillPoints` / `pointsText` | §2.2 |
| `解锁` / `isUnlocked` / `CanUnlockSkill` / `前置` | §2.1 |
| `升级技能` / `TryUpgradeSkill` / `maxLevel` | §2.1 |
| `SkillManager` / `技能没效果` | §2.4 |
| `SkillSO` / 技能改名 | §2.4、§3 |
| `OnAbilityPointSpent` / `OnMaxSkillLevel` | §2.3、§3 |

## 2. 当前实现

### 2.1 技能树数据结构与解锁条件

- 配置类型 `SkillSO` 只有 `skillName`、`maxLevel`、`skillIcon` 三个字段，`maxLevel` 默认为 5。它的标识来自技能名与资产名；稳定 id 由 `GuidSO` 提供，当前使用者是 `CharacterSO`。
- 技能状态存在槽位实例上，没有独立数据类：`SkillSlot` 持有 `skillSO`、私有 `currentLevel`、公开 `isUnlocked`，以及场景接线的前置列表 `preRiquriedForSkillUnlock_List`。等级按槽位独立计数，同一个 `SkillSO` 被多个槽位引用时各自从头累计。
- 解锁条件：`CanUnlockSkill` 要求前置列表中每个槽位都 `isUnlocked`，并且 `currentLevel` 达到该槽位 `skillSO` 的 `maxLevel`；前置列表为空时结果为 true。解锁动作只有 `Unlock()` 一个入口。
- 升级规则：`TryUpgradeSkill` 要求 `isUnlocked` 为真且 `currentLevel` 小于 `skillSO` 的 `maxLevel`，随后 `currentLevel` 自增、广播 `OnAbilityPointSpent`，满级再广播 `OnMaxSkillLevel`，最后 `UpdateUI`。技能点的扣减由订阅方完成，见 §2.2。
- 两个 static 事件是唯一的状态广播通道。`UpdateUI` 按解锁态切换按钮可交互、文本与图标颜色：未解锁显示 `Locked`，已解锁显示当前等级与上限。
- 场景实例规模：常驻场景里 `SkillTreeManager.skillSlots` 接了 24 个 `SkillSlot`，其中 23 个指向 `MaxHealthBoost`，1 个指向 `CombatUnlock`；前置依赖数组长度为 1 到 3。
- 初始解锁态：技能按钮预制体的 `isUnlocked` 默认为 0，前置列表为空，`skillSO` 为空引用；场景里有 3 个实例把它改为 1，分别是 `MaxHealthBoost` 两个与 `CombatUnlock` 一个。技能树从预先解锁的根往外生长。
- 技能等级与解锁状态不入档：`SaveData` 顶层字段里没有技能相关字段，技能点数随 `PlayerStatsData.skillPoints` 入档。

### 2.2 技能点来源与收支

- 技能点属于玩家数值域：字段是 `PlayerStatsModel.skillPoints`，写命令 `UpdateSkillPoints` 不广播事件，经 `StatsService.UpdateSkillPoints` 转发。
- 来源是升级：`PlayerStatsModel.AddExp` 结算完升级后广播 `LevelUp(levelsGained)`。
- 发放：`SkillTreeManager.UpdateAbilityPoints(amount)` 调 `StatsService.UpdateSkillPoints(amount)` 后刷新 `pointsText`；一次升多级就发多点，读档不补发。
- 支出：点击槽位触发 `SkillTreeManager.Start` 注册的 onClick 闭包，闭包先查技能点大于 0 才调 `slot.TryUpgradeSkill()`；升级成功广播 `OnAbilityPointSpent`，`SkillTreeManager.HandleAbilityPointSpent` 再查一次余额并 `UpdateAbilityPoints(-1)`。
- 界面刷新点有三处：`Start` 结束时调 `UpdateAbilityPoints(0)`、升级事件、扣点回调。技能点变化本身不发事件，跳过 `UpdateAbilityPoints` 直接写 `StatsService` 会让点数文本变旧。

### 2.3 SkillTreeManager 与 SkillManager 的分工

| 类 | 订阅 | 职责 |
|---|---|---|
| `SkillTreeManager` | `SkillSlot.OnAbilityPointSpent`、`SkillSlot.OnMaxSkillLevel`、`PlayerStatsModel.LevelUp` | 点数收支、文本刷新、满级后按前置条件批量解锁、给每个槽位挂升级按钮回调 |
| `SkillManager` | `SkillSlot.OnAbilityPointSpent` | 按 `skillSO.skillName` 分发技能效果，技能点收支归 `SkillTreeManager` |

两个类只共享同一个 static 事件，订阅都在 `OnEnable`，退订都在 `OnDisable`。

- 满级解锁：`HandleSkillMaxed` 遍历全部槽位，把所有未解锁且前置满足的槽位一次性 `Unlock()`，按前置图整体推进。
- 可重试订阅：`StatsService` 是 `Awake` 里注册的单例，`SkillTreeManager.OnEnable` 可能先于它执行，此时 `StatsService.Instance` 为 null。订阅因此写在 `TrySubscribeLevelUp()` 里，用 `listeningToLevelUp` 标记幂等，并在 `OnEnable` 与 `Start` 各调一次。`UnsubscribeLevelUp` 先用标记短路，`StatsService.Instance` 为空时直接返回。

### 2.4 技能效果作用到哪些域

`SkillManager.HandleAbilityPointSpent` 认两个名字：

| `skillSO.skillName` | 效果 | 作用域 |
|---|---|---|
| `"MaxHealthBoost"` | `StatsService.UpdateMaxHealth(1)` 与 `UpdateHealth(1)` | 玩家数值域 |
| `"SwordSlash"` | `combat.SetActive(true)` | 战斗域 |

现有资产与名字的对应：`MaxHealthBoost` 的 `skillName` 是 `MaxHealthBoost`、`maxLevel` 取 5；`CombatUnlock` 的资产名与技能名不同，`skillName` 是 `SwordSlash`、`maxLevel` 取 1。效果是单向的：`SwordSlash` 只置 true，关闭与切换由 `ShiftEquipment` 每 0.3 秒冷却的翻转逻辑独立完成。

`SkillTreeCanvasManager` 只负责技能面板的开关、焦点与场景切换复位，不参与技能逻辑。

## 3. 约定与硬边界

1. **扣点发生在事件订阅方**：任何广播 `OnAbilityPointSpent` 的路径都依赖 `SkillTreeManager` 处于激活且已订阅；它失活时升级照常加等级，技能点保持不变。
2. **`skillName` 字符串即协议**：`SkillManager` 用 `switch (skillSO.skillName)` 派发，改技能名或复制资产时漏改字符串会静默失效。
3. **两个 static 事件必须成对退订**：`OnAbilityPointSpent` 与 `OnMaxSkillLevel` 都是 static，清空手段是订阅方在 `OnDisable` 退订；Domain Reload 关闭时残留订阅会跨局呼叫已销毁对象。
4. **根技能由场景预先解锁**：`isUnlocked` 是序列化字段，技能按钮预制体默认为 0，初始解锁来源是场景与预制体实例上的赋值，代码里没有开局解锁根节点的逻辑。
5. **前置条件要求前置槽位满级**：`CanUnlockSkill` 要求前置槽位的 `currentLevel` 达到其 `maxLevel`；只解锁不点满不会放行后续节点。
6. **同一个 `SkillSO` 可被多个槽位复用且各自计数**：`currentLevel` 在槽位上，场景里 23 个槽位共用 `MaxHealthBoost`，因此等级上限按槽位各自计算。
7. **技能等级与解锁不入档，技能点入档**：读档后技能树回到场景接线初值，技能点恢复存档值。
8. **`UpdateAbilityPoints` 是点数文本的唯一刷新入口**：绕过它直接写 `StatsService.UpdateSkillPoints` 会让 `pointsText` 与实际值不一致。
9. **按钮回调只挂不退订**：`skillButton.onClick.AddListener` 传的是匿名闭包，`OnDisable` 只退订两个 static 事件，不做 `RemoveListener`。`Start` 每个实例只跑一次，正常生命周期下不会叠加。
