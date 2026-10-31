# 分层边界与读写约定 Guide

文档 ID：`ARCH-LAYERING-GUIDE`
状态：`Active`
最后更新：`2026-10-31`
权威范围：`Contracts`、`Pipeline`、`Gameplay` 三层目录的含义、跨层依赖方向、命名空间与程序集边界、唯一写入口与只读接口的判定标准。各玩法域的内部实现归对应玩法域的 Guide，存档格式与场景流程细节由各自文档负责。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `分层` `Layering` `三层` | 本文 §2.1 |
| `Contracts` `契约层` | 本文 §2.1、§3.4 |
| `Pipeline` `管线层` | 本文 §2.1 |
| `依赖方向` `反向依赖` | 本文 §2.2 |
| `asmdef` `程序集` `Assembly-CSharp` | 本文 §2.7、§3.1 |
| `命名空间` `namespace` `全局命名空间` | 本文 §2.5、§3.2 |
| `MyEnums` `枚举拆开` | 本文 §2.1 |
| `MVCS` `Model` `View` `Controller` `Service` | 本文 §2.3 |
| `唯一写入口` `只读接口` `StatsService` | 本文 §2.4、§3.5 |
| `YSingleton` `单例基类` | 本文 §2.6 |
| `测试` `[Test]` `回归网` | 本文 §2.7 |

## 2. 当前实现

### 2.1 三层目录的含义

`Contracts`、`Pipeline`、`Gameplay` 是三个平级目录，按"变化原因"切分：

| 目录 | 文件数 | 含义 | 判定标准：新文件落到哪 |
|---|---|---|---|
| `Contracts` | 8 | 跨域共享的接口、基类与枚举 | 类型被两个以上域直接引用，且其中不含具体业务规则 |
| `Pipeline` | 57 | 引擎级基建：场景切换、寻路、对象池、UI 系统基建、SO 配置与事件通道 | 与具体玩法无关，换一个游戏也成立 |
| `Gameplay` | 59 | 玩法域，按 `Player` `Quest` `Dialog` `Inventory` `Shop` `Skills` `Units` `Save` `Grid` 九个目录平铺 | 带玩法语义，换一套规则就得改 |

`Contracts` 的 8 个文件全部声明 `namespace Contracts`。原 `MyEnums` 类已拆开：`PlayerState` `EnemyState` `SceneType` `PersistentType` `AStarNodeType` `NPCState` `QuestState` `CanvasToToggle` `SaveType` `SaveLoadPanelType` 现在是 `Contracts` 下的 10 个顶层枚举，文件仍叫 `MyEnums.cs`。引用它们写 `using Contracts;` 后直接用枚举名，不再有 `MyEnums.` 前缀。

### 2.2 跨层依赖方向

按 `using` 指令统计：

- `Contracts` → `Gameplay`：**2 处**，`ISaveable` 与 `SaveableService` 各引一次 `Gameplay.Save`；`Contracts` → `Pipeline`：**3 处**，`ICanvasManager` 引 `Pipeline.SO.Events` 与 `Pipeline.UI.SystemCanvasManagers`，`IShopInteractable` 引 `Pipeline.SO`。
- `Gameplay` → `Contracts`：**29 处**；`Gameplay` → `Pipeline`：**62 处**。
- `Pipeline` → `Contracts`：**22 处**；`Pipeline` → `Gameplay`：**6 处**，分别是 `ContinueButton`、`LootEventSO`、`MobileAttackButton`、`PlayerStatsSO`、`SceneChanger`、`ShopKeeperEventSO` 各 1 处。

方向口径：`Contracts` 被所有域引用是设计意图，所以 `Contracts → Gameplay/Pipeline` 的 5 处算反向依赖，`Pipeline → Gameplay` 的 6 处同样算反向依赖。

这组数字比 2026-10-05 记录的（`Contracts →` 0 处、`Gameplay → Pipeline` 0 处、`Pipeline → Gameplay` 1 处）大了一个量级。差别不来自这轮重构新增了耦合，而来自引用的书写方式：类型原先在全局命名空间，`using` 一行都不用写，依赖是隐式的、统计不到；收进命名空间之后每条跨命名空间依赖都必须显式 `using`，编译器第一次把真实依赖图完整写了出来。旧数字是全局命名空间造成的度量假象，新数字才是实际耦合。

管线层与玩法域的主要接触点仍是配置资产要用的数值传输类型（`PlayerStatsSO` → `Gameplay.Player.Models`），其余 5 处是管线侧的按钮、事件通道与场景切换器直接引用了玩法侧的单例或资产类型。

### 2.3 玩家数值线的 MVCS 四层

九个玩法域里只有 `Gameplay` 的 `Player` 数值线拆成四层，其余域是整体 Manager 形态，一个类叠数据、规则与 UI 刷新：

| 层 | 类型 | 命名空间 | 形态与实例化方式 |
|---|---|---|---|
| Model | `PlayerStatsModel` `IPlayerStatsReadOnly` `PlayerStatsData` | `Gameplay.Player.Models` | 纯 C#，`new PlayerStatsModel(PlayerStatsData)` |
| Service | `StatsService` | `Gameplay.Player.Services` | `SaveableService<StatsService>` 单例 |
| Controller | `ExperienceController` `HealthController` `StatsPanelController` `PlayerDamageController` | `Gameplay.Player.Controllers` | 前三个纯 C#，由 View 托管 `new`；`PlayerDamageController` 挂场景 |
| View | `ExperiencePanelView` `HealthView` `StatsPanelView` | `Gameplay.Player.Views` | 普通 MonoBehaviour，不挂单例 |

`Player` 相关产品代码文件 18 个，全部带命名空间，按层分布为 `Gameplay.Player` 7 个、`Gameplay.Player.Controllers` 4 个、`Gameplay.Player.Models` 3 个、`Gameplay.Player.Views` 3 个、`Gameplay.Player.Services` 1 个。`Arrow` `PlayerAnimationEventRelay` `PlayerBow` `PlayerCombat` `PlayerMovement` `ShiftEquipment` 这 6 个类型现在也在 `Gameplay.Player` 里，移动、战斗、弓箭这条线仍是整体 Manager 形态。12 个带命名空间的类型中含 `PlayerLocator`，命名空间为 `Gameplay.Player`，是存档域专用的坐标查询单例，不归属上述四层。

### 2.4 唯一写入口与只读接口的约定

判定标准：

1. **外部读**：只经 `StatsService.Instance.Stats`，静态类型是 `IPlayerStatsReadOnly`。该接口暴露 4 个事件与 15 个只读属性，成员集合里没有写方法。
2. **外部写**：只经 `StatsService` 的 6 个转发命令 `UpdateMaxHealth` `UpdateHealth` `UpdateSpeed` `UpdateDamage` `UpdateSkillPoints` `AddExperience`，每个命令都是单行转发到 Model；另有第 7 个公开入口 `Respawn()`，守卫 `if (model.CurrentHealth <= 0)` 位于 Service 内部，构成规则只在 Model 的公开例外。
3. **规则只在 Model**：钳制、经验结算、事件广播都在 `PlayerStatsModel` 的写方法内部。Service 逐条转发，自身不复制规则；`Respawn` 的守卫是 Service 独有的服务层规则。
4. **Model 保持在 Service 内部**：`StatsService` 的 `model` 字段是 private，`GetStats` 与 `LoadStats` 也是 private。测试断言 `StatsService` 有 `Stats` 属性，且其公开成员里没有 `Model` 属性。

`SaveData.playerStatsData` 字段类型是具体的 `PlayerStatsData`，存档域直接持有 Model 的传输格式。

### 2.5 命名空间分布

**命名空间一律跟随目录**：命名空间就是文件相对 `Assets\Scripts\` 的目录路径，点号代替反斜杠，目录名原样保留大小写。`Contracts\MyEnums.cs` 是 `Contracts`，`Gameplay\Units\Enemy\EnemyCombat.cs` 是 `Gameplay.Units.Enemy`，`Pipeline\SO\Events\VoidEventSO.cs` 是 `Pipeline.SO.Events`。没有例外，也不再有意外的命名空间名。

25 个命名空间覆盖全部 124 个产品代码文件，**全局命名空间里已经没有任何产品代码**：

| 层 | 命名空间 | 文件数 |
|---|---|---|
| `Contracts` | `Contracts` | 8 |
| `Pipeline` | `Pipeline` | 3 |
| | `Pipeline.Pathfinding` | 7 |
| | `Pipeline.Scene` | 7 |
| | `Pipeline.SO` | 10 |
| | `Pipeline.SO.Events` | 12 |
| | `Pipeline.UI` | 6 |
| | `Pipeline.UI.Buttons` | 6 |
| | `Pipeline.UI.SystemCanvasManagers` | 6 |
| `Gameplay` | `Gameplay.Dialog` | 1 |
| | `Gameplay.Dialog.HistoryManager` | 3 |
| | `Gameplay.Grid` | 2 |
| | `Gameplay.Inventory` | 5 |
| | `Gameplay.Player` | 7 |
| | `Gameplay.Player.Controllers` | 4 |
| | `Gameplay.Player.Models` | 3 |
| | `Gameplay.Player.Services` | 1 |
| | `Gameplay.Player.Views` | 3 |
| | `Gameplay.Quest` | 6 |
| | `Gameplay.Save` | 6 |
| | `Gameplay.Shop` | 5 |
| | `Gameplay.Skills` | 4 |
| | `Gameplay.Units.Enemy` | 4 |
| | `Gameplay.Units.NPC` | 4 |
| | `Gameplay.Units.ShopKeeper` | 1 |

- 合计 124 个产品代码文件；测试侧 5 个文件同样是 `namespace Gameplay.Tests`。
- 全局命名空间只剩 1 个编辑器脚本 `Assets\Editor\InputActionReferenceRebuilder.cs`，它不在 `Assets\Scripts` 下，不受"跟随目录"规则约束。产品代码、测试与编辑器脚本合计 130 个源文件。
- 跨命名空间调用一律靠 `using`；同一命名空间内的类型直接可见。原先"全局类型不用 using"的便利已不存在。

### 2.6 单例基类与写入口

`YSingleton<T>` 提供 `Instance` 只读访问、重复实例自毁、`OnDestroy` 复位。工程里 18 个具体类直接继承它，加一个抽象中间层 `SaveableService<TSelf>`。

`SaveableService` 是单例与存档身份的合并基类：继承即自动注册进 `SaveRegistry`、销毁自动注销、`GetDataID()` 固定返回 `null` 表示走固定槽位存档。`StatsService` 是它当前唯一的子类，也是唯一写入口这条约定的落地处。

### 2.7 边界测试

编辑器侧测试共 5 个文件、合计 52 个 `[Test]`：

| 文件 | 用例数 | 守的边界 |
|---|---|---|
| `PlayerStatsModelTests` | 24 | Model 规则、事件语义、拷贝语义、`StatsService` 不暴露 Model |
| `CanvasFocusStackTests` | 15 | 纯 C# 焦点栈 `CanvasFocusStack` 的入栈出栈语义 |
| `AStarOpenHeapTests` | 6 | 寻路的开放列表堆 |
| `ObjectPoolTests` | 5 | 对象池 |
| `PlayerStatsSOTests` | 2 | 配置资产的拷贝语义 |

这 5 个文件位于编辑器侧测试目录、无 asmdef，编译进默认程序集，与产品代码同程序集，可直接 `new` 内部类型并反射 `StatsService`。

## 3. 约定与硬边界

### 3.1 0 个 asmdef ⇒ 全工程一个程序集

改动任何源码都触发整个 `Assembly-CSharp` 重编；分层只靠命名约定与纪律，编译器不拦截跨层引用。

### 3.2 命名空间按目录分配，重名不再跨域互撞

产品代码已全部进命名空间，标识符分属 25 个命名空间；同名类型只要不在同一命名空间就不会冲突，编译失败点也会落回该命名空间对应的目录。剩下的全局池只有 1 个编辑器脚本。

代价是引用变啰嗦：任何跨目录引用都要 `using`，漏写就是 `CS0246`。新增文件时命名空间必须与所在目录一致，否则该类型既不被同目录代码看见，也会在将来挪动时制造一次无谓的改名。

### 3.3 `SaveableService` 子类重写 `OnSingletonInitialized` 必须调 `base`

注册动作在基类里，漏调 `base.OnSingletonInitialized()` 不报错、不警告，该服务会漏掉 `SaveRegistry` 登记，存读档时被静默跳过。

### 3.4 `Contracts` 不得反向 using `Gameplay` 或 `Pipeline`

契约层被所有域引用，反向依赖会形成环。**这条现在是被违反的，共 5 处**，是命名空间改造的直接后果：

- `ISaveable`、`SaveableService` → `using Gameplay.Save;`：两者签名里直接用到 `SaveData` 与 `SaveDefinition`，而这两个类型住在 `Gameplay\Save\`。
- `ICanvasManager` → `using Pipeline.SO.Events;` 与 `using Pipeline.UI.SystemCanvasManagers;`：接口属性返回 `ToggleCanvasEventSO`、`SceneLoadedEventSO`，默认实现调 `UIManager`。
- `IShopInteractable` → `using Pipeline.SO;`：方法签名接受 `ItemSO`。

改造前这几处不写 `using` 也能编译，因为类型都在全局命名空间；搬进命名空间后依赖被迫显式化，环就暴露出来了。**在收口之前，本条是待偿债务，不是已满足的约束**。收口方向有两条：把契约层实际依赖的类型下沉进 `Contracts`，或让契约层改用不依赖具体类型的抽象（事件基类、`ISaveData` 之类的接口）。

### 3.5 `StatsService.Instance.Stats` 是只读视图，接口上不带写方法

测试断言 `StatsService` 不暴露具体 Model。

### 3.6 改 `PlayerStatsData` 的字段名等于坏档

字段名就是存档 JSON 键；类名可以改，字段名不可以。

### 3.7 纯 C# 层不得引用 `MonoBehaviour` 生命周期

包括 `PlayerStatsModel`、三个显示侧 Controller 与 `CanvasFocusStack`；EditMode 测试依赖这条。
