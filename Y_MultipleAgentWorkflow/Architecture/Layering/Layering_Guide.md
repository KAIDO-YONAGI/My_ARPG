# 分层边界与读写约定 Guide

文档 ID：`ARCH-LAYERING-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责三层目录（Contracts / Gameplay / Pipeline）的真实含义、跨层依赖方向、全局命名空间与程序集边界、以及"唯一写入口 + 只读接口"这条读写约定的判定标准；各玩法域的内部实现归对应玩法域的 Guide，存档格式与场景流程细节由各自文档负责。
上游来源：
- `My_ARPG_MVCS项目现状`（旧架构权威文档，仅作线索）
- 契约层源码
- 玩法层源码
- 管线层源码
- 编辑器侧测试源码

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `分层` `Layering` `三层` | 本文 §2.1 |
| `Contracts` `契约层` | 本文 §2.1、§3.4 |
| `Pipeline` `管线层` | 本文 §2.1、§3.3 |
| `依赖方向` `反向依赖` | 本文 §2.2 |
| `asmdef` `程序集` `Assembly-CSharp` | 本文 §2.5、§3.1 |
| `命名空间` `namespace` `全局命名空间` | 本文 §2.4、§3.2 |
| `MVCS` `Model` `View` `Controller` `Service` | 本文 §2.3 |
| `唯一写入口` `只读接口` `StatsService` | 本文 §2.3、§3.5 |
| `YSingleton` `单例基类` | 本文 §2.6 |
| `测试` `[Test]` `回归网` | 本文 §2.7 |

## 2. 当前实现

### 2.1 三层目录的真实含义

`Contracts`、`Pipeline`、`Gameplay` 是三个平级目录，按"变化原因"切分：

| 目录 | 文件数 | 真实含义 | 判定标准：新文件落到哪 |
|---|---|---|---|
| `Contracts` | 8 | 跨域共享的接口、基类与枚举 | 类型被两个以上域直接引用，且其中不含具体业务规则 |
| `Pipeline` | 57 | 引擎级基建：场景切换、寻路、对象池、UI 系统基建、SO 配置与事件通道 | 与具体玩法无关，换一个游戏也成立 |
| `Gameplay` | 59 | 玩法域，按 `Player` `Quest` `Dialog` `Inventory` `Shop` `Skills` `Units` `Save` `Grid` 九个目录平铺 | 带玩法语义，换一套规则就得改 |

`Contracts` 里只有 `MyEnums` 带 namespace，其余 7 个类型 `ICanvasManager` `IDamageable` `ISaveable` `IShopInteractable` `SaveableService` `SaveRegistry` `YSingleton` 全部落在全局命名空间——它们是全工程可无 using 直接引用的基建。

### 2.2 跨层依赖方向（实测）

按 `using` 指令实测（非推测）：

- `Contracts` 引用 `Gameplay` 或 `Pipeline`：**0 处**。契约层不反向依赖任何实现。
- `Gameplay` 引用 `Pipeline`：**0 处**。玩法域不 using 管线（管线类型在全局命名空间，可达但未使用）。
- `Pipeline` 引用 `Gameplay`：**1 处**，也是全工程唯一的跨层反向依赖——`PlayerStatsSO` 顶部的 `using Gameplay.Player.Models;`。

也就是说管线层唯一"知道"玩法域的地方，是配置资产要用的数值传输类型。见下文缺陷 D1。

### 2.3 玩家数值线：本工程唯一成型的 MVCS 四层

九个玩法域里只有 `Gameplay` 的 `Player` 局部的数值线拆成了四层，其余域是整体 Manager 形态（一个类叠数据 + 规则 + UI 刷新）：

| 层 | 目录 | 类型 | 命名空间 | 实例化方式 |
|---|---|---|---|---|
| Model | `Gameplay/Player/Models` | `PlayerStatsModel` `IPlayerStatsReadOnly` `PlayerStatsData` | `Gameplay.Player.Models` | 纯 C#，`new PlayerStatsModel(PlayerStatsData)` |
| Service | `Gameplay/Player/Services` | `StatsService` | `Gameplay.Player.Services` | `SaveableService<StatsService>` 单例 |
| Controller | `Gameplay/Player/Controllers` | `ExperienceController` `HealthController` `StatsPanelController`（纯 C#）；`PlayerDamageController`（MonoBehaviour） | `Gameplay.Player.Controllers` | 三个由 View 托管 `new`，一个挂场景 |
| View | `Gameplay/Player/Views` | `ExperiencePanelView` `HealthView` `StatsPanelView` | `Gameplay.Player.Views` | 普通 MonoBehaviour，不挂单例 |

`Gameplay/Player` 目录下 18 个产品代码文件里，带命名空间的共 12 个；另有 6 个类型 `Arrow` `PlayerAnimationEventRelay` `PlayerBow` `PlayerCombat` `PlayerMovement` `ShiftEquipment` 留在全局命名空间且未分层——移动、战斗、弓箭这条线是整体 Manager 形态。12 个带命名空间的类型中包含 `PlayerLocator`（命名空间 `Gameplay.Player`）：它是存档域专用的坐标查询单例，不归属上面四层中的任何一层，其自身注释写明它担当坐标查询。

### 2.4 唯一写入口与只读接口的约定

判定标准（可直接拿去审新代码）：

1. **外部读**：只经 `StatsService.Instance.Stats`，静态类型是 `IPlayerStatsReadOnly`。该接口暴露 4 个事件与 15 个只读属性，其成员集合里没有任何写方法。
2. **外部写**：只经 `StatsService` 的 6 个转发命令 `UpdateMaxHealth` `UpdateHealth` `UpdateSpeed` `UpdateDamage` `UpdateSkillPoints` `AddExperience`，每个命令都是单行转发到 Model；另有第 7 个公开入口 `Respawn()`，它例外地带业务规则，守卫 `if (model.CurrentHealth <= 0)` 位于 Service 内部，是「规则只在 Model」的唯一公开例外。
3. **规则只在 Model**：钳制、经验结算、事件广播都在 `PlayerStatsModel` 的写方法内部。Service 逐条转发，自身不复制规则——`Respawn` 除外，其守卫是 Service 独有的服务层规则。
4. **Model 保持在 Service 内部**：`StatsService` 的 `model` 字段是 private，`GetStats` 与 `LoadStats` 也是 private。测试钉住了这条：断言 `StatsService` 有 `Stats` 属性、且其公开成员里没有 `Model` 属性。

实测绕开约定的情况只有一处：`SaveData.playerStatsData` 字段类型是具体的 `PlayerStatsData`，存档域因此直接持有 Model 的传输格式，这条读路径绕开了只读接口。

### 2.5 命名空间约定（当前分布）

- 产品代码 124 个文件里，**只有 13 个带 namespace**：`MyEnums`（1 个）+ `Gameplay.Player` 系列（12 个，其中 11 个在四个层目录内，另有 `PlayerLocator` 位于 `Player/` 根、命名空间是 `Gameplay.Player`）。其余 **111 个落在全局命名空间**。测试侧 5 个测试文件均以 `namespace Gameplay.Tests` 开头（见测试章节 §2.7），编辑器侧 1 个编辑器脚本落在全局命名空间，故工程内产品代码、测试与编辑器脚本合计 130 个。
- 带命名空间的目录名与命名空间严格对应：`Models` → `Gameplay.Player.Models`，`Controllers` → `Gameplay.Player.Controllers`，`Views` → `Gameplay.Player.Views`，`Services` → `Gameplay.Player.Services`；`Player/` 根目录本身对应 `Gameplay.Player`。
- 跨命名空间调用靠 `using`；全局命名空间的类型谁都能**直接引用**。

### 2.6 单例基类与"写入口"的关系

`YSingleton<T>` 提供 `Instance` 只读访问、重复实例自毁、`OnDestroy` 复位。工程里 **18 个具体类**直接继承它，加一个抽象中间层 `SaveableService<TSelf>`。

`SaveableService` 是"单例 + 存档身份"的合并基类：继承即自动注册进 `SaveRegistry`、销毁自动注销、`GetDataID()` 固定返回 `null` 表示走固定槽位存档。`StatsService` 是它当前**唯一**的子类，也是「唯一写入口」这条约定的落地处。

### 2.7 边界的实际防线是测试

编辑器侧测试共 **5 个文件、合计 52 个 `[Test]`**（实测按特性计数，工程内暂无 `[UnityTest]`）：

| 文件 | 用例数 | 守的是哪条边界 |
|---|---|---|
| `PlayerStatsModelTests` | 24 | Model 规则、事件语义、拷贝语义、`StatsService` 不暴露 Model |
| `CanvasFocusStackTests` | 15 | 纯 C# 焦点栈 `CanvasFocusStack` 的入栈出栈语义 |
| `AStarOpenHeapTests` | 6 | 寻路的开放列表堆 |
| `ObjectPoolTests` | 5 | 对象池 |
| `PlayerStatsSOTests` | 2 | 配置资产的拷贝语义 |

这 5 个文件都位于编辑器侧测试目录且**没有 asmdef**，因此编译进默认程序集，与产品代码同程序集——这也是它们能直接 `new` 内部类型、直接反射 `StatsService` 的原因。

## 3. 约定与硬边界

1. **0 个 asmdef ⇒ 全工程一个程序集。** 改动任何源码都触发整个 `Assembly-CSharp` 重编；分层只靠命名约定与纪律，编译器**不会**拦截跨层引用。后果：往 `Pipeline` 里写 `using Gameplay.*` 不会报错，只会静默加深耦合（D1 就是这么发生的）。
2. **全局命名空间是共用的。** 产品代码里 111 个类的标识符在一个全局池里（编辑器侧那 1 个编辑器脚本同池）；新类型重名会直接编译失败，且失败点可能出现在无关文件。往带命名空间的目录加类时，若类名已在全局池里被引用，混用会出歧义。
3. **`SaveableService` 子类重写 `OnSingletonInitialized` 必须调 `base`。** 注册动作在基类里，漏调 `base.OnSingletonInitialized()` 不报错、不警告，该服务会漏掉 `SaveRegistry` 登记，存读档时被静默跳过。
4. **`Contracts` 不得反向 using `Gameplay` 或 `Pipeline`。** 契约层被所有域引用，一旦反向依赖就形成环；当前实测 0 处，保持住。
5. **`StatsService.Instance.Stats` 是只读视图，不要试图往接口上加写方法。** 接口的存在意义就是让外部拿不到写入口；有测试盯着 `StatsService` 不暴露具体 Model。
6. **改 `PlayerStatsData` 的字段名等于坏档。** 字段名就是存档 JSON 键；类名可以改，字段名不可以。
7. **纯 C# 层（Model、三个显示侧 Controller、`CanvasFocusStack`）不得引用 `MonoBehaviour` 生命周期**，否则 EditMode 测试跑不起来——现有 52 个用例的一半依赖这条。

## 4. 已知缺陷与风险

**D1：`Pipeline` 反向依赖 `Gameplay`（唯一一处）。** `PlayerStatsSO` 引入 `Gameplay.Player.Models`，全工程仅此一处跨层反向引用。后果是管线层无法独立于玩法域编译，`PlayerStatsData` 改名会同时打到管线和存档两处。

**D2：`PlayerStatsSO.Data` 泄漏资产内部引用。** 属性 `Data => stats` 直接返回序列化字段本体，返回的是引用本身。测试为此绕开了它——用 `so.Data.damage = 7` 写配置，再用 `CreateInitialData()` 验拷贝，因此"外部拿到 `Data` 就能改资产"这条没有被任何测试约束。

**D3：`ISaveable` 同时定义数据契约与注册动作。** `RegisterSaveable` 与 `UnRegisterSaveable` 带默认实现直接写静态 `SaveRegistry`，而 `SaveableService` 走的是另一条注册路径（在 `OnSingletonInitialized` 里直接 `SaveRegistry.Add`）。同一件事有两条入口，谁在什么时候调用默认实现没有文档约束。

**D4：`My_ARPG_MVCS项目现状` 文档的 Addressables 用例数仍无法复核。** 该文档记「5 个文件、共 52 个用例」，与实测的 52 个 `[Test]` 一致；同处另记「加上 Addressables 包自带的 1 个，Test Runner 里共 53 个」。仍存疑的是包侧那个「1 个」：实测 Addressables 包内 209 个测试源码含 **993 个 `[Test]`**，远不止 1 个（该包是否有测试 asmdef 被启用、Test Runner 实际列出多少，运行期行为待编辑器实测）。

**D5：`My_ARPG_MVCS项目现状` 的编号标题序号各出现一次。** 该文档的一级编号标题依次是 `### 1.1`、`### 1.2`、`### 1.3`、`### 1.4`、`### 1.5`、`### 1.6`，每个序号各出现一次，顺序连续。

**D6：`ICanvasManager` 拼写不一致。** 同一接口里是 `SetCanvaInactive` `SetCanvaState` `RefreshCanvaOrder`，而 `StatsPanelView` 里用 `((ICanvasManager)this).SetCanvaInactive(...)` 显式转型调用。改名会同时打到所有面板实现类。

**D7：`ICanvasManager` 默认实现直接依赖具体 `UIManager`。** 接口的默认方法体里调 `UIManager.Report(...)` 与 `UIManager.Instance`，契约层因此隐式绑定到 `Pipeline` 的具体单例，与 §3.4"契约层不依赖实现"的纪律相悖（当前靠"类型在全局命名空间、无需 using"绕过，实测 `Contracts` 里 0 条 `using`）。

## 5. 未核验事项

- 假设：52 个 `[Test]` 在 Unity Test Runner 里全部通过，且失败数为 0。运行期行为待编辑器实测。
- 假设：编辑器侧测试缺少 asmdef 时仍能被 Test Runner 的 EditMode 平台发现并执行。运行期行为待编辑器实测；`My_ARPG_MVCS项目现状` 称"Test Runner 里共 53 个"，该数字含 Addressables 包侧用例。
- 假设：`PlayerStatsSO` 是全工程唯一 `Pipeline → Gameplay` 引用，没有通过反射、字符串或 Addressables 资产路径形成的隐式反向依赖。待编辑器实测；当前覆盖范围是 `using` 指令的静态文本检索。
- 假设：`Assembly-CSharp` 是唯一产品程序集，编辑器侧测试也编译其中。待编辑器实测；依据是工程内 asmdef 实测计数为 0，尚未通过工程设置与包清单交叉确认。
- 假设：全局命名空间的 111 个类之间当前没有标识符冲突（能编译即无冲突，实际编译待编辑器实测）。
- 假设：`SaveableService` 当前只有 `StatsService` 一个子类（依据是 `: SaveableService<` 的文本检索仅命中声明行与 `StatsService`）。待编辑器实测。
- 假设：`StatsService` 的 `[SerializeField] private PlayerStatsSO statsConfig` 在所有场景里都已接线，否则其 `OnSingletonInitialized` 会抛空引用。待打开场景与预制体验证。
- 假设：`Gameplay/Player` 下 6 个全局命名空间类（移动、战斗、弓箭线）确实走整体 Manager 形态，MVCS 四层只覆盖数值线一条。依据是目录实测，尚未逐个通读其内部实现。
