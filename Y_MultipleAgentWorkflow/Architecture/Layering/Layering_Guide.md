# 分层边界与读写约定 Guide

文档 ID：`ARCH-LAYERING-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
权威范围：`Contracts`、`Pipeline`、`Gameplay` 三层目录的含义、跨层依赖方向、全局命名空间与程序集边界、唯一写入口与只读接口的判定标准。各玩法域的内部实现归对应玩法域的 Guide，存档格式与场景流程细节由各自文档负责。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `分层` `Layering` `三层` | 本文 §2.1 |
| `Contracts` `契约层` | 本文 §2.1、§3.4 |
| `Pipeline` `管线层` | 本文 §2.1 |
| `依赖方向` `反向依赖` | 本文 §2.2 |
| `asmdef` `程序集` `Assembly-CSharp` | 本文 §2.7、§3.1 |
| `命名空间` `namespace` `全局命名空间` | 本文 §2.5、§3.2 |
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

`Contracts` 里只有 `MyEnums` 带 namespace，其余 7 个类型 `ICanvasManager` `IDamageable` `ISaveable` `IShopInteractable` `SaveableService` `SaveRegistry` `YSingleton` 全部落在全局命名空间，可无 using 直接引用。

### 2.2 跨层依赖方向

按 `using` 指令统计：

- `Contracts` 引用 `Gameplay` 或 `Pipeline`：**0 处**。
- `Gameplay` 引用 `Pipeline`：**0 处**；管线类型在全局命名空间，可达但未使用。
- `Pipeline` 引用 `Gameplay`：**1 处**，全工程唯一的跨层反向依赖，为 `PlayerStatsSO` 顶部的 `using Gameplay.Player.Models;`。

管线层与玩法域的接触点是配置资产要用的数值传输类型。

### 2.3 玩家数值线的 MVCS 四层

九个玩法域里只有 `Gameplay` 的 `Player` 数值线拆成四层，其余域是整体 Manager 形态，一个类叠数据、规则与 UI 刷新：

| 层 | 类型 | 命名空间 | 形态与实例化方式 |
|---|---|---|---|
| Model | `PlayerStatsModel` `IPlayerStatsReadOnly` `PlayerStatsData` | `Gameplay.Player.Models` | 纯 C#，`new PlayerStatsModel(PlayerStatsData)` |
| Service | `StatsService` | `Gameplay.Player.Services` | `SaveableService<StatsService>` 单例 |
| Controller | `ExperienceController` `HealthController` `StatsPanelController` `PlayerDamageController` | `Gameplay.Player.Controllers` | 前三个纯 C#，由 View 托管 `new`；`PlayerDamageController` 挂场景 |
| View | `ExperiencePanelView` `HealthView` `StatsPanelView` | `Gameplay.Player.Views` | 普通 MonoBehaviour，不挂单例 |

`Player` 相关产品代码文件 18 个，带命名空间的 12 个；另有 6 个类型 `Arrow` `PlayerAnimationEventRelay` `PlayerBow` `PlayerCombat` `PlayerMovement` `ShiftEquipment` 留在全局命名空间，移动、战斗、弓箭这条线是整体 Manager 形态。12 个带命名空间的类型中含 `PlayerLocator`，命名空间为 `Gameplay.Player`，是存档域专用的坐标查询单例，不归属上述四层。

### 2.4 唯一写入口与只读接口的约定

判定标准：

1. **外部读**：只经 `StatsService.Instance.Stats`，静态类型是 `IPlayerStatsReadOnly`。该接口暴露 4 个事件与 15 个只读属性，成员集合里没有写方法。
2. **外部写**：只经 `StatsService` 的 6 个转发命令 `UpdateMaxHealth` `UpdateHealth` `UpdateSpeed` `UpdateDamage` `UpdateSkillPoints` `AddExperience`，每个命令都是单行转发到 Model；另有第 7 个公开入口 `Respawn()`，守卫 `if (model.CurrentHealth <= 0)` 位于 Service 内部，构成规则只在 Model 的公开例外。
3. **规则只在 Model**：钳制、经验结算、事件广播都在 `PlayerStatsModel` 的写方法内部。Service 逐条转发，自身不复制规则；`Respawn` 的守卫是 Service 独有的服务层规则。
4. **Model 保持在 Service 内部**：`StatsService` 的 `model` 字段是 private，`GetStats` 与 `LoadStats` 也是 private。测试断言 `StatsService` 有 `Stats` 属性，且其公开成员里没有 `Model` 属性。

`SaveData.playerStatsData` 字段类型是具体的 `PlayerStatsData`，存档域直接持有 Model 的传输格式。

### 2.5 命名空间分布

- 产品代码 124 个文件里只有 13 个带 namespace：`MyEnums` 1 个，`Gameplay.Player` 系列 12 个，其中 11 个在四个层目录内，`PlayerLocator` 的命名空间是 `Gameplay.Player`；其余 111 个落在全局命名空间。测试侧 5 个测试文件均以 `namespace Gameplay.Tests` 开头，编辑器侧 1 个编辑器脚本落在全局命名空间，产品代码、测试与编辑器脚本合计 130 个。
- 带命名空间的目录，命名空间为 `Gameplay.Player` 加目录名：`Gameplay.Player.Models`、`Gameplay.Player.Controllers`、`Gameplay.Player.Views`、`Gameplay.Player.Services`，`Gameplay.Player` 对应 `Player` 根目录。
- 跨命名空间调用靠 `using`；全局命名空间的类型可直接引用。

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

### 3.2 全局命名空间是共用的

产品代码里 111 个类的标识符在一个全局池里，编辑器侧那 1 个编辑器脚本同池；新类型重名会直接编译失败，失败点可能出现在无关文件。往带命名空间的目录加类时，类名已在全局池里被引用会出歧义。

### 3.3 `SaveableService` 子类重写 `OnSingletonInitialized` 必须调 `base`

注册动作在基类里，漏调 `base.OnSingletonInitialized()` 不报错、不警告，该服务会漏掉 `SaveRegistry` 登记，存读档时被静默跳过。

### 3.4 `Contracts` 不得反向 using `Gameplay` 或 `Pipeline`

契约层被所有域引用，反向依赖会形成环；当前 0 处。

### 3.5 `StatsService.Instance.Stats` 是只读视图，接口上不带写方法

测试断言 `StatsService` 不暴露具体 Model。

### 3.6 改 `PlayerStatsData` 的字段名等于坏档

字段名就是存档 JSON 键；类名可以改，字段名不可以。

### 3.7 纯 C# 层不得引用 `MonoBehaviour` 生命周期

包括 `PlayerStatsModel`、三个显示侧 Controller 与 `CanvasFocusStack`；EditMode 测试依赖这条。
