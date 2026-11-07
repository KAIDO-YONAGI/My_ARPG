# 技能树权威指南：Gameplay.Skills

文档 ID：`GP-SKILLS-GUIDE`
状态：`Active`
最后更新：`2026-11-07`
## 维护规则

本文件对应的实现、约束或规则发生变化时，按已核验事实更新相关章节，证据记录在[开发日志](DeveloperLog.md)。修改小标题时同步文档内目录；计数与维护周期遵循[工作流维护规则](../../Workflow/Workflow_Guide.md#维护规则)。

## 文档内目录

- [1. 触发线索](#1-触发线索)
- [2. 当前实现](#2-当前实现)
  - [2.1 技能树数据结构与解锁条件](#21-技能树数据结构与解锁条件)
  - [2.2 技能点来源与收支](#22-技能点来源与收支)
  - [2.3 SkillTreeManager 的职责](#23-skilltreemanager-的职责)
  - [2.4 界面与状态同步](#24-界面与状态同步)
- [3. 约定与硬边界](#3-约定与硬边界)
  - [3.1 升级校验与扣点由管理器命令处理](#31-升级校验与扣点由管理器命令处理)
  - [3.2 状态键由层级路径决定](#32-状态键由层级路径决定)
  - [3.3 满级事件随视图开关成对订阅](#33-满级事件随视图开关成对订阅)
  - [3.4 根技能由场景预先解锁](#34-根技能由场景预先解锁)
  - [3.5 前置条件要求前置槽位满级](#35-前置条件要求前置槽位满级)
  - [3.6 同一个 `SkillSO` 可被多个槽位复用且各自计数](#36-同一个-skillso-可被多个槽位复用且各自计数)
  - [3.7 技能等级与解锁不入档，技能点入档](#37-技能等级与解锁不入档技能点入档)
  - [3.8 技能点写入经技能管理器同步状态](#38-技能点写入经技能管理器同步状态)
  - [3.9 按钮回调随槽位启停成对管理](#39-按钮回调随槽位启停成对管理)

先读目录，再按任务读取相关章节，避免整份指南载入上下文。

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
| `OnMaxSkillLevel` / 升级命令 | §2.3、§3 |

## 2. 当前实现

### 2.1 技能树数据结构与解锁条件

- 配置类型 `SkillSO` 只有 `skillName`、`maxLevel`、`skillIcon` 三个字段，`maxLevel` 默认为 5。它的标识来自技能名与资产名；稳定 id 由 `GuidSO` 提供，当前使用者是 `CharacterSO`。
- 技能运行状态由 `SkillTreeManager` 持有，槽位按相对画布根节点的路径登记等级与解锁状态。`SkillSlot` 持有配置、前置槽位引用和显示副本，预制体实例重建时重新拉取已有状态。
- 解锁条件：`CanUnlockSkill` 要求前置列表中每个槽位都 `isUnlocked`，并且 `currentLevel` 达到该槽位 `skillSO` 的 `maxLevel`；前置列表为空时结果为 true。解锁动作只有 `Unlock()` 一个入口。
- 升级规则：槽位把技能键、等级上限与初始 `SkillState` 交给 `SkillTreeManager.TryUpgradeSkill`。管理器优先读取已保存状态，校验已解锁、未满级和技能点余额，成功时保存新等级、扣一点并同步技能点状态。槽位读取返回值刷新显示，满级后广播 `OnMaxSkillLevel`。
- `UpdateUI` 按解锁态切换按钮可交互、文本与图标颜色：未解锁显示 `Locked`，已解锁显示当前等级与上限。满级广播供技能画布检查前置依赖。
- `SkillsCanvasView` 的槽位数组、技能点文本以及各槽位的前置依赖均在预制体上序列化接线；管理器通过稳定的 `CurrentState` 提供技能点、`SkillsVersion`、`AbilityPointsVersion` 和按键读取的技能状态。视图分别比较技能与点数版本，成功绘制后记录到自己的 `VersionReader`。
- 初始解锁态：技能按钮预制体的 `isUnlocked` 默认为 0，前置列表为空，`skillSO` 为空引用；场景里有 3 个实例把 `isUnlocked` 设为 1，分别是 `MaxHealthBoost` 两个与 `CombatUnlock` 一个。技能树从预先解锁的根往外生长。
- 技能等级与解锁状态不入档：`SaveData` 顶层字段里没有技能相关字段，技能点数随 `PlayerStatsData.skillPoints` 入档。

### 2.2 技能点来源与收支

- 技能点属于玩家数值域：字段是 `PlayerStatsModel.skillPoints`，写命令 `UpdateSkillPoints` 不广播事件，经 `StatsService.UpdateSkillPoints` 转发。
- 来源是升级：`PlayerStatsModel.AddExp` 结算完升级后广播 `LevelUp(levelsGained)`。
- 发放：`SkillTreeManager.UpdateAbilityPoints(amount)` 调 `StatsService.UpdateSkillPoints(amount)`；技能点文本由 `SkillsCanvasView` 读取 `State.AbilityPointsValue` 刷新，管理器不碰控件。一次升多级就发多点，读档不补发。
- 支出：`SkillSlot.OnEnable` 绑定升级按钮，点击后直接调用管理器的数据命令。校验失败时技能等级、点数及其版本均保持原值。
- 管理器在 `Start` 通过 `RefreshAbilityPoints()` 同步当前技能点；升级事件经 `UpdateAbilityPoints` 发放点数，技能命令扣点后同步 State。技能点变化本身不发事件，跳过管理器直接写 `StatsService` 会让 State 中的点数变旧。

### 2.3 SkillTreeManager 的职责

`SkillTreeManager` 订阅 `PlayerStatsModel.LevelUp` 发放技能点，并通过数据命令负责升级校验、点数支出与状态保存。管理器接收技能键和数值，`State` 提供只读数据及版本；文本、按钮和槽位引用由视图管理。升级事件在 `OnEnable` 订阅、`OnDisable` 退订。

- 满级解锁：`SkillsCanvasView` 在 `OnOpen` 订阅 `SkillSlot.OnMaxSkillLevel`，在 `OnClose` 退订；其 `HandleSkillMaxed` 遍历全部槽位，把所有未解锁且前置满足的槽位一次性 `Unlock()`，按前置图整体推进。
- 可重试订阅：`StatsService` 是 `Awake` 里注册的单例，`SkillTreeManager.OnEnable` 可能先于它执行，此时 `StatsService.Instance` 为 null。订阅因此写在 `TrySubscribeLevelUp()` 里，用 `listeningToLevelUp` 标记幂等，并在 `OnEnable` 与 `Start` 各调一次。`UnsubscribeLevelUp` 先用标记短路，`StatsService.Instance` 为空时直接返回。

### 2.4 界面与状态同步

`SkillsCanvasView` 由 `UICacheManager` 管理实例生命周期，开关、焦点与场景切换复位由缓存与 `UIManager` 协同处理。打开时绑定各槽位的状态根，使本视图的两个读取记录失效，再从管理器恢复等级、解锁状态与技能点。技能升级命令保存等级，槽位解锁经 `SaveSkillState` 保存。绘制前捕获版本，成功后记录本次版本，绘制期间发生的变化留给下次读取。

## 3. 约定与硬边界

### 3.1 升级校验与扣点由管理器命令处理

升级入口为 `SkillTreeManager.TryUpgradeSkill`。保存的技能状态优先于调用方的初值；余额不足、未解锁或达到上限时返回 false。成功结果包含新等级与解锁状态，槽位据此更新显示。

### 3.2 状态键由层级路径决定

`SkillSlot` 首次取键或绑定不同画布根时，从槽位向上记录节点名与同级序号并缓存路径字符串。对同一根重复绑定、常规刷新和点击复用该字符串。键以静态预制体层级为准；调整层级或改名后，新实例生成的键会改变，对应状态回落到初始值。

### 3.3 满级事件随视图开关成对订阅

`OnMaxSkillLevel` 是槽位之间检查前置关系的 static 事件。`SkillsCanvasView` 在 `OnOpen` 订阅、`OnClose` 退订；Domain Reload 关闭时仍需成对管理，以免跨局保留已销毁的视图。

### 3.4 根技能由场景预先解锁

`isUnlocked` 是序列化字段，技能按钮预制体默认为 0，初始解锁来源是场景与预制体实例上的赋值，代码里没有开局解锁根节点的逻辑。

### 3.5 前置条件要求前置槽位满级

`CanUnlockSkill` 要求前置槽位的 `currentLevel` 达到其 `maxLevel`；只解锁不点满不会放行后续节点。

### 3.6 同一个 `SkillSO` 可被多个槽位复用且各自计数

`currentLevel` 在槽位上，场景里 23 个槽位共用 `MaxHealthBoost`，因此等级上限按槽位各自计算。

### 3.7 技能等级与解锁不入档，技能点入档

读档后技能树回到场景接线初值，技能点恢复存档值。

### 3.8 技能点写入经技能管理器同步状态

技能点发放走 `UpdateAbilityPoints`，支出走 `TryUpgradeSkill`。两条路径都更新 `StatsService` 并同步 `AbilityPointsVersion`；直接写数值服务时，技能树的显示状态不会同步。

### 3.9 按钮回调随槽位启停成对管理

`SkillSlot.OnEnable` 使用 `AddListener(TryUpgradeSkill)` 绑定按钮，`OnDisable` 使用 `RemoveListener(TryUpgradeSkill)` 移除同一回调，槽位重新启用时重新绑定。
