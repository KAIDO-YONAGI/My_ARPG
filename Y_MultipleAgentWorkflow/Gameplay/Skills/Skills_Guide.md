# 技能树（Gameplay.Skills）权威指南

文档 ID：`GP-SKILLS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只对「技能槽位状态与解锁依赖、技能点收支、技能效果分发」这条链路负责；技能点的存取字段与升级事件归 `Gameplay.PlayerStats`，武器在战斗中的实际行为归战斗域（`PlayerCombat`/`ShiftEquipment`），面板显隐基建归 UI 域（`ICanvasManager`/`UIManager`）。
上游来源：
- `Assets/Scripts/Gameplay/Skills/{SkillSlot,SkillTreeManager,SkillManager,SkillTreeCanvasManager}.cs`
- `Assets/Scripts/Pipeline/SO/SkillSO.cs`、`Assets/GameSO/UI SO/SkillButtonSO/{MaxHealthBoost,CombatUnlock}.asset`
- `Assets/Scripts/Gameplay/Player/{Models/PlayerStatsModel.cs,Models/PlayerStatsData.cs,Services/StatsService.cs,PlayerCombat.cs,ShiftEquipment.cs}`
- 场景接线：`Assets/Scenes/GameScene/PersistentScene.unity`、`Assets/Prefabs/UI/Buttons/SkillButton.prefab`
- 旧文档线索：`Docs/My_ARPG_MVCS项目现状.md` §1.6、§2.2（101、112、126-132、156、160、166 行）

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
| `保留的验证点` / `SkillService` | §2.5 |

## 2. 当前实现

### 2.1 技能树数据结构与解锁条件

- 配置类型 `SkillSO` 只有三个字段：`skillName`、`maxLevel`（默认 5）、`skillIcon`（`Assets/Scripts/Pipeline/SO/SkillSO.cs:7-13`）。它**没有**继承 `GuidSO`（项目里 `CharacterSO` 才是使用者，`Assets/Scripts/Pipeline/SO/CharacterSO.cs:5`），因此没有稳定 id。
- 状态在槽位上，不在技能上，也没有独立数据类：`SkillSlot` 持有 `skillSO`、私有 `currentLevel`、公开 `isUnlocked`，以及场景接线的前置列表 `preRiquriedForSkillUnlock_List`（`Assets/Scripts/Gameplay/Skills/SkillSlot.cs:9-16`）。**等级是 per-slot 的**：`currentLevel` 存在槽位实例里，同一个 `SkillSO` 被多个槽位引用时各自独立计数。
- 解锁条件：`CanUnlockSkill` 要求前置列表中每个槽位都 `isUnlocked` **且** `currentLevel >= slot.skillSO.maxLevel`（`SkillSlot.cs:65-75`）；前置列表为空即恒为 true。解锁动作只有 `Unlock()` 一个入口（:59-63）。
- 升级规则：`TryUpgradeSkill` 要求 `isUnlocked && currentLevel < skillSO.maxLevel`，然后 `currentLevel++`、广播 `OnAbilityPointSpent`，到满级再广播 `OnMaxSkillLevel`，最后 `UpdateUI`（`SkillSlot.cs:45-58`）。**它自己不扣技能点**，扣减由订阅方完成（§2.2）。
- 两个 static 事件是唯一的状态广播通道（`SkillSlot.cs:18-19`）；`UpdateUI` 按解锁态切按钮可交互、文本（`当前/上限` 或 `Locked`）与图标颜色（:27-44）。
- 场景实例规模（静态读场景文件）：`PersistentScene` 里 `SkillTreeManager.skillSlots` 接了 **24 个** `SkillSlot`（`Assets/Scenes/GameScene/PersistentScene.unity:36337-36361`），其中 23 个指向 `MaxHealthBoost.asset`、1 个指向 `CombatUnlock.asset`（同一文件中 `propertyPath: skillSO` 的 24 处 objectReference）。前置依赖同样接在场景的预制体实例修改项上，数组长度为 1~3（例如 :3287-3293 为 1，:19677-19691 为 3）。
- 初始解锁态：预制体默认 `isUnlocked: 0`（`Assets/Prefabs/UI/Buttons/SkillButton.prefab:143-144`，其 `preRiquriedForSkillUnlock_List: []`、`skillSO: {fileID: 0}`），场景里只有 3 个实例把它改成 1（`PersistentScene.unity:1588-1591`、:7662-7665、:17712-17715，分别指向 MaxHealthBoost ×2 与 CombatUnlock ×1）。树只能从预先解锁的根往前长。
- 技能等级与解锁状态**不入档**：`SaveData` 顶层没有技能字段（`Assets/Scripts/Gameplay/Save/SaveData.cs:9-11`），只有技能点数随 `PlayerStatsData.skillPoints` 入档（`Assets/Scripts/Gameplay/Player/Models/PlayerStatsData.cs:23`）。

### 2.2 技能点来源与收支

- 技能点属于玩家数值域：`PlayerStatsModel.skillPoints`，写命令是 `UpdateSkillPoints`，**不广播事件**（`Assets/Scripts/Gameplay/Player/Models/PlayerStatsModel.cs:54,128-129`），经 `StatsService.UpdateSkillPoints` 转发（`Services/StatsService.cs:48`）。
- 来源：升级。`PlayerStatsModel.AddExp` 结算完升级后广播 `LevelUp(levelsGained)`（`PlayerStatsModel.cs:160`，事件声明 :22-23）。
- 发放：`SkillTreeManager` 在 `UpdateAbilityPoints(amount)` 里 `StatsService.UpdateSkillPoints(amount)` 后刷新 `pointsText`（`SkillTreeManager.cs:77-81`）。一次升多级就发多点，读档不补发（`PlayerStatsModel.LoadFrom` 不发 `LevelUp`，:72-82）。
- 支出：点击槽位 → `SkillTreeManager.Start` 注册的 onClick 闭包先查 `SkillPoints > 0` 才调 `slot.TryUpgradeSkill()`（`SkillTreeManager.cs:68-74`）；升级成功广播 `OnAbilityPointSpent` → `SkillTreeManager.HandleAbilityPointSpent` 再查一次余额并 `UpdateAbilityPoints(-1)`（:48-54）。
- 界面刷新点只有这三处：`Start` 结束时 `UpdateAbilityPoints(0)`（:75）、升级事件（:36）、扣点回调（:52）。技能点变化本身不触发事件，因此**跳过 `UpdateAbilityPoints` 直接写 `StatsService` 会让点数文本变旧**。

### 2.3 SkillTreeManager 与 SkillManager 的分工

| 类 | 订阅 | 职责 |
|---|---|---|
| `SkillTreeManager` | `SkillSlot.OnAbilityPointSpent`、`SkillSlot.OnMaxSkillLevel`、`PlayerStatsModel.LevelUp`（`SkillTreeManager.cs:16-18,36`） | 点数收支、文本刷新、满级后按前置条件批量解锁（:56-63）、给每个槽位挂升级按钮回调（:68-74） |
| `SkillManager` | `SkillSlot.OnAbilityPointSpent`（`SkillManager.cs:11`） | 按 `skillSO.skillName` 分发技能效果，**不碰技能点**（:17-32） |

两者互不知情，只共享同一个 static 事件；订阅都在 `OnEnable`、退订都在 `OnDisable`（`SkillTreeManager.cs:14-28`；`SkillManager.cs:9-16`）。

- 满级解锁的实现：`HandleSkillMaxed` 遍历全部槽位，把所有「未解锁且前置满足」的槽位一次性 `Unlock()`（`SkillTreeManager.cs:56-63`）。它不接收触发槽位之外的筛选条件，因此是「按前置图整体推进」。
- 可重试订阅的成因与实现：`StatsService` 是 `Awake` 里注册的单例（`Assets/Scripts/Contracts/YSingleton.cs:15-25`，`StatsService` 经 `OnSingletonInitialized` 建模型，`StatsService.cs:26-30`），而 `SkillTreeManager.OnEnable` 可能先于它执行，此时 `StatsService.Instance` 仍为 null。因此订阅写在 `TrySubscribeLevelUp()` 里，用 `listeningToLevelUp` 标记幂等，并在 `OnEnable`（可能失败）与 `Start`（一定在所有 `Awake` 之后）各调一次（`SkillTreeManager.cs:18,30-37,66`）。退订 `UnsubscribeLevelUp` 先用标记短路，`StatsService.Instance` 为空时直接返回，不会重复退订（:39-46）。

### 2.4 技能效果作用到哪些域

`SkillManager.HandleAbilityPointSpent` 只认两个名字（`SkillManager.cs:17-32`）：

| `skillSO.skillName` | 效果 | 作用域 |
|---|---|---|
| `"MaxHealthBoost"` | `StatsService.UpdateMaxHealth(1)` + `UpdateHealth(1)` | 玩家数值域（`StatsService.cs:40-42`） |
| `"SwordSlash"` | `combat.SetActive(true)` | 战斗域（`Assets/Scripts/Gameplay/Player/PlayerCombat.cs:14-16`） |

现有资产与名字的对应：`MaxHealthBoost.asset:15-16`（`skillName: MaxHealthBoost`、`maxLevel: 5`）、`CombatUnlock.asset:15-16`（**资产名与技能名不同**：`skillName: SwordSlash`、`maxLevel: 1`）。效果是单向的——`SwordSlash` 只置 true，没有反向关闭的代码；关闭/切换由 `ShiftEquipment` 每 0.3 秒冷却的翻转逻辑独立完成（`ShiftEquipment.cs:37-48`）。

`SkillTreeCanvasManager` 只负责技能面板的开关、焦点与场景切换复位（`Assets/Scripts/Gameplay/Skills/SkillTreeCanvasManager.cs:12-36`），不参与任何技能逻辑。

### 2.5 关于「保留的验证点」

旧文档把技能域记为「迁移方案方向已定、待动工」的保留验证点（`Docs/My_ARPG_MVCS项目现状.md:101,134-161`）。**核对代码现状：方案未动工**，目标结构里的 `SkillSystemModel`、`SkillService`、`SkillEffectApplier`、`SkillTreeController` 在 `Assets/Scripts` 下都不存在（全量检索无命中）；`SkillSlot` 仍同时持有状态、规则、static 事件与 UI，`SkillTreeManager` 仍兼管点数扣减与依赖解锁，`SkillManager` 仍按字符串分发（`SkillSlot.cs:9-58`；`SkillTreeManager.cs:48-81`；`SkillManager.cs:17-32`）。`SkillSO` 也仍未接 `GuidSO`。

## 3. 约定与硬边界

1. **扣点发生在事件订阅方**：任何能广播 `OnAbilityPointSpent` 的路径都依赖 `SkillTreeManager` 处于激活且已订阅；它失活时升级会照常加等级、但不扣点（`SkillSlot.cs:45-58`；`SkillTreeManager.cs:24-25,48-54`）。
2. **`skillName` 字符串即协议**：`SkillManager` 用 `switch (skillSO.skillName)` 派发（`SkillManager.cs:21-31`），改技能名或复制资产时漏改字符串会静默失效（旧文档 D2，`Docs/My_ARPG_MVCS项目现状.md:126-128`）。
3. **两个 static 事件必须成对退订**：`OnAbilityPointSpent`/`OnMaxSkillLevel` 是 static（`SkillSlot.cs:18-19`），唯一清空手段是订阅方在 `OnDisable` 退订（`SkillTreeManager.cs:22-28`、`SkillManager.cs:13-16`）；Domain Reload 关闭时残留订阅会跨局呼叫已销毁对象。
4. **根技能必须由场景预先解锁**：`isUnlocked` 是序列化字段，预制体默认 0，唯一初始解锁来源是场景/预制体实例上的赋值（`SkillButton.prefab:144`；`PersistentScene.unity:1588-1591`），代码里没有「开局解锁根节点」的逻辑。
5. **前置条件是「满级」而不是「已解锁」**：`CanUnlockSkill` 要求前置槽位 `currentLevel >= maxLevel`（`SkillSlot.cs:69-72`），只解锁不点满不会放行后续节点。
6. **同一 `SkillSO` 可被多个槽位复用且各自计数**：`currentLevel` 在槽位上（:15），场景里 23 个槽位共用 `MaxHealthBoost`（§2.1），因此等级上限是「每槽位 5 级」而不是「每技能 5 级」。
7. **技能等级/解锁不入档，技能点入档**：读档后技能树回到场景接线初值、技能点恢复存档值（`SaveData.cs:9-11`；`PlayerStatsData.cs:23`）。
8. **`UpdateAbilityPoints` 是点数文本的唯一刷新入口**：绕过它直接写 `StatsService.UpdateSkillPoints` 会让 `pointsText` 与实际值不一致（`SkillTreeManager.cs:77-81`；`PlayerStatsModel.cs:128-129`）。
9. **按钮回调只挂不退订**：`skillButton.onClick.AddListener` 传的是匿名闭包，`OnDisable` 只退订两个 static 事件、不做 `RemoveListener`（`SkillTreeManager.cs:68-74`、:22-28）。`Start` 每个实例只跑一次，正常生命周期下不会叠加；风险在于这些闭包没有任何解除路径，一旦槽位与 manager 的生命周期被拆开（常驻按钮 + 重建 manager）就会留下悬空委托。

## 4. 已知缺陷与风险

1. **点数校验与扣减分裂在两个类、两个时刻**（旧文档 D3，代码一致）：校验在 onClick 闭包与 `HandleAbilityPointSpent`，写入在事件回调里，中间隔着 static 事件广播（`SkillTreeManager.cs:68-74,48-54`）。
2. **技能效果按名字字符串分发**（旧文档 D2，代码一致）：改名或新增同名资产即静默失效（`SkillManager.cs:21-31`）。
3. **未覆盖的技能名静默无效果**：现有 24 个槽位只用了 `MaxHealthBoost` 与 `SwordSlash` 两个名字，都在 `switch` 内；但新增 `SkillSO` 的名字一旦不在名单里，就只会被扣点数、没有任何效果，也没有日志（`SkillManager.cs:17-32`）。
4. **点数不足时点击没有反馈**：`onClick` 里只做 `SkillPoints > 0` 判断，不满足则什么都不发生，`UpdateUI` 也不会提示原因（`SkillTreeManager.cs:70-73`）。
5. **`UnsubscribeLevelUp` 在 `StatsService.Instance == null` 时无法真正退订**：标记已被置回 false，`LevelUp` 委托残留且无法再补退订（`SkillTreeManager.cs:39-46`）。
6. **`SkillSlot.UpdateUI` 无 `skillSO` 空守卫**：`OnValidate` 先判空再调用，但运行时代码路径（`Unlock`/`TryUpgradeSkill`）与预制体默认值 `skillSO: {fileID: 0}` 组合下若被外部调用会 NRE（`SkillSlot.cs:27-44`；`SkillButton.prefab:138`）。
7. **满级解锁是全局扫描**：`HandleSkillMaxed` 遍历全部 24 个槽位并对所有满足条件者解锁（`SkillTreeManager.cs:56-63`），若将来有多棵互不相干的技能树挂在同一个 manager 下，会互相放行。
8. **技能域没有回归网**：`Assets/Tests/Editor/` 下 5 个用例文件（`ObjectPoolTests`、`PlayerStatsSOTests`、`CanvasFocusStackTests`、`PlayerStatsModelTests`、`AStarOpenHeapTests`）没有任何一个覆盖 `SkillSlot`/`SkillTreeManager`/`SkillManager`。

## 5. 未核验事项

- 假设：`SkillTreeManager`、`SkillManager`、24 个 `SkillSlot` 都在同一 `PersistentScene` 且同属一个技能面板分组，因此不存在「两棵树共用一个 manager」的情况（未运行 Unity 验证；仅静态读到 guid 与数组接线）。
- 假设：场景里 3 个 `isUnlocked: 1` 的槽位就是设计上的三个根节点，其余 21 个靠前置满级解锁（未运行 Unity 验证）。
- 假设：`SkillManager.combat` 引用的是玩家身上的 `PlayerCombat`，且 `SwordSlash` 的语义是「解锁剑模式」（未运行 Unity 验证）。
- 假设：`SkillTreeManager` 的 `pointsText` 已接线（场景中为 `{fileID: 432747330827997248}`，未运行 Unity 验证其非空）。
- 假设：`SkillTreeManager.OnEnable` 确实可能早于 `StatsService.Awake`（代码注释与 `Start` 里的补订阅表明作者按此假设设计，但实际执行顺序由场景与脚本顺序决定，未运行 Unity 验证）。
- 假设：技能等级/解锁「不入档、每局重置」是当前设计意图而非遗漏（旧文档把它列为待拍板项，`Docs/My_ARPG_MVCS项目现状.md:160`，未运行 Unity 验证）。
- 假设：`SkillButton.prefab` 上的 `preRiquriedForSkillUnlock_List: []`（:142）不影响场景实例，因为场景实例用预制体实例修改项覆盖（未运行 Unity 验证）。
