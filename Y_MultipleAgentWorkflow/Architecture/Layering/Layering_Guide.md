# 分层边界与读写约定 Guide

文档 ID：`ARCH-LAYERING-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责三层目录（Contracts / Gameplay / Pipeline）的真实含义、跨层依赖方向、全局命名空间与程序集边界、以及"唯一写入口 + 只读接口"这条读写约定的判定标准；不负责各玩法域的内部实现（归 `Gameplay.*` Router 下的 Guide），不负责存档格式与场景流程细节。
上游来源：
- `D:\Unity\Projects\My_ARPG\Docs\My_ARPG_MVCS项目现状.md`（旧架构权威，仅作线索）
- `D:\Unity\Projects\My_ARPG\Assets\Scripts\Contracts\**`（8 个 .cs）
- `D:\Unity\Projects\My_ARPG\Assets\Scripts\Gameplay\**`（59 个 .cs）
- `D:\Unity\Projects\My_ARPG\Assets\Scripts\Pipeline\**`（57 个 .cs）
- `D:\Unity\Projects\My_ARPG\Assets\Tests\Editor\**`（5 个 .cs）

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

`Assets/Scripts/` 下是三个平级目录，不是三个功能域——它们按"变化原因"而非"功能"切分：

| 目录 | 文件数 | 真实含义 | 判定标准（新文件放哪） |
|---|---|---|---|
| `Contracts/` | 8 | 跨域共享的接口、基类与枚举 | 类型被两个以上域直接引用，且不含具体业务规则 |
| `Pipeline/` | 57 | 引擎级基建：场景切换、寻路、对象池、UI 系统基建、SO 配置与事件通道 | 与具体玩法无关、换一个游戏也能用 |
| `Gameplay/` | 59 | 玩法域，按 `Player` `Quest` `Dialog` `Inventory` `Shop` `Skills` `Units` `Save` `Grid` 九个目录平铺 | 有玩法语义，换个规则就得改 |

`Contracts/` 里**只有** `MyEnums.cs` 带 namespace（`MyEnums`），其余 7 个（`ICanvasManager` `IDamageable` `ISaveable` `IShopInteractable` `SaveableService` `SaveRegistry` `YSingleton`）全部落在全局命名空间——它们是全工程可无 using 直接用的基建。

### 2.2 跨层依赖方向（实测）

按 `using` 指令实测（非推测）：

- `Contracts/` → `Gameplay.*` 或 `Pipeline.*`：**0 处**。契约层不反向依赖任何实现。
- `Gameplay/` → `Pipeline.*`：**0 处**。玩法域不 using 管线（管线类型在全局命名空间，可达但未使用）。
- `Pipeline/` → `Gameplay.*`：**1 处**，也是全工程唯一的反向依赖——`Assets\Scripts\Pipeline\SO\PlayerStatsSO.cs:2` 的 `using Gameplay.Player.Models;`。

也就是说管线层唯一"知道"玩法域的地方，是配置资产要用的数值传输类型。见 §4 D1。

### 2.3 玩家数值线：本工程唯一成型的 MVCS 四层

九个玩法域里只有 `Gameplay/Player` 局部的数值线拆成了四层，其余域是整体 Manager 形态（一个类叠数据 + 规则 + UI 刷新）：

| 层 | 目录 | 类型 | 命名空间 | 实例化方式 |
|---|---|---|---|---|
| Model | `Gameplay/Player/Models/` | `PlayerStatsModel` `IPlayerStatsReadOnly` `PlayerStatsData` | `Gameplay.Player.Models` | 纯 C#，`new PlayerStatsModel(PlayerStatsData)` |
| Service | `Gameplay/Player/Services/` | `StatsService` | `Gameplay.Player.Services` | `SaveableService<StatsService>` 单例 |
| Controller | `Gameplay/Player/Controllers/` | `ExperienceController` `HealthController` `StatsPanelController`（纯 C#）；`PlayerDamageController`（MonoBehaviour） | `Gameplay.Player.Controllers` | 三个由 View 托管 `new`，一个挂场景 |
| View | `Gameplay/Player/Views/` | `ExperiencePanelView` `HealthView` `StatsPanelView` | `Gameplay.Player.Views` | 普通 MonoBehaviour，不挂单例 |

`Gameplay/Player` 目录下 18 个 .cs 里只有这 12 个带命名空间，另有 6 个（`Arrow` `PlayerAnimationEventRelay` `PlayerBow` `PlayerCombat` `PlayerMovement` `ShiftEquipment`）仍在全局命名空间且未分层——移动/战斗/弓箭这条线**没有**做 MVCS。第 12 个带命名空间的类是 `PlayerLocator`（`namespace Gameplay.Player`，`Assets\Scripts\Gameplay\Player\PlayerLocator.cs:3`）；它不属于上面四层中的任何一层，是存档域专用的坐标查询单例，自身注释也写明「不是 MVCS 的 Controller」。

### 2.4 唯一写入口与只读接口的约定

判定标准（可直接拿去审新代码）：

1. **外部读**：只经 `StatsService.Instance.Stats`，静态类型是 `IPlayerStatsReadOnly`（`Assets\Scripts\Gameplay\Player\Services\StatsService.cs:24`）。接口只暴露 4 个事件 + 15 个只读属性（`Assets\Scripts\Gameplay\Player\Models\IPlayerStatsReadOnly.cs:11`），没有任何写方法。
2. **外部写**：只经 `StatsService` 的 6 个转发命令（`UpdateMaxHealth` `UpdateHealth` `UpdateSpeed` `UpdateDamage` `UpdateSkillPoints` `AddExperience`），全部是单行转发到 Model（`StatsService.cs:40-50`）；另有第 7 个公开入口 `Respawn()`（`StatsService.cs:32-36`），它**例外地带业务规则**（`if (model.CurrentHealth <= 0)` 才回满血，条件在 Service 里而非 Model 里），是「规则只在 Model」的唯一公开例外。
3. **规则只在 Model**：钳制、经验结算、事件广播都在 `PlayerStatsModel` 的写方法内部（`PlayerStatsModel.cs:97-174`）。Service 不复制规则——`Respawn` 除外，其守卫是 Service 独有的服务层规则。
4. **Model 不对外泄漏**：`StatsService` 里 `model` 字段是 private（`StatsService.cs:21`），`GetStats`/`LoadStats` 也是 private（`StatsService.cs:53,56`）。有测试把这条钉住了：断言 `StatsService` 有 `Stats` 属性、且**没有** `Model` 属性（`Assets\Tests\Editor\PlayerStatsModelTests.cs:171-180`）。

实测绕开约定的情况只有一处：`SaveData.playerStatsData` 字段类型是具体的 `PlayerStatsData`（`Assets\Scripts\Gameplay\Save\SaveData.cs:11`），存档域因此直接持有 Model 的传输格式，而不是只读接口。

### 2.5 命名空间约定（现状，不是理想态）

- `Assets/Scripts/` 下 124 个 .cs（产品代码），**只有 13 个带 namespace**：`MyEnums`（1 个）+ `Gameplay.Player` 系列（12 个，其中 11 个在四个层目录内，另有 `PlayerLocator` 直接位于 `Player/` 根、命名空间是 `Gameplay.Player`）。其余 **111 个在全局命名空间**。工程另一半代码不在 `Assets/Scripts/`：`Assets/Tests/Editor/` 5 个测试文件均以 `namespace Gameplay.Tests` 开头（见 §2.7），`Assets/Editor/` 1 个编辑器脚本在全局命名空间，故 `Assets/` 下 .cs 总数为 130。
- 带命名空间的目录名与命名空间严格对应：`Models/` → `Gameplay.Player.Models`，`Controllers/` → `Gameplay.Player.Controllers`，`Views/` → `Gameplay.Player.Views`，`Services/` → `Gameplay.Player.Services`；`Player/` 根目录本身对应 `Gameplay.Player`。
- 跨命名空间调用靠 `using`；全局命名空间的类型谁都能直接引用，**不需要 using**。

### 2.6 单例基类与"写入口"的关系

`YSingleton<T>`（`Assets\Scripts\Contracts\YSingleton.cs:9`）提供 `Instance` 只读访问、重复实例自毁、`OnDestroy` 复位。工程里 **18 个具体类**直接继承它，加一个抽象中间层 `SaveableService<TSelf>`（`Assets\Scripts\Contracts\SaveableService.cs:11`）。

`SaveableService` 是"单例 + 存档身份"的合并基类：继承即自动注册进 `SaveRegistry`、销毁自动注销、`GetDataID()` 固定返回 `null` 表示走固定槽位存档（`SaveableService.cs:14-25`）。`StatsService` 是它当前**唯一**的子类（`StatsService.cs:16`）。

### 2.7 边界的实际防线是测试，不是编译器

`Assets/Tests/Editor/` 下 **5 个文件、合计 52 个 `[Test]`**（实测按特性计数，无 `[UnityTest]`）：

| 文件 | 用例数 | 守的是哪条边界 |
|---|---|---|
| `PlayerStatsModelTests.cs` | 24 | Model 规则、事件语义、拷贝语义、`StatsService` 不暴露 Model |
| `CanvasFocusStackTests.cs` | 15 | 纯 C# 焦点栈（`Pipeline/UI/SystemCanvasManagers/CanvasFocusStack.cs`） |
| `AStarOpenHeapTests.cs` | 6 | 寻路的开放列表堆 |
| `ObjectPoolTests.cs` | 5 | 对象池 |
| `PlayerStatsSOTests.cs` | 2 | 配置资产的拷贝语义 |

这 5 个文件都在 `Assets/Tests/Editor/` 且**没有 asmdef**，因此编译进默认程序集，与产品代码同程序集——这也是它们能直接 `new` 内部类型、直接反射 `StatsService` 的原因。

## 3. 约定与硬边界

1. **0 个 asmdef ⇒ 全工程一个程序集。** 改动任何 .cs 都触发整个 `Assembly-CSharp` 重编；分层只靠命名约定与纪律，编译器**不会**拦截跨层引用。后果：往 `Pipeline/` 里写 `using Gameplay.*` 不会报错，只会静默加深耦合（§4 D1 就是这么发生的）。
2. **全局命名空间是共用的。** `Assets/Scripts/` 里 111 个类的标识符在一个全局池里（`Assets/Editor/` 的 1 个编辑器脚本同池）；新类型重名会直接编译失败，且失败点可能出现在无关文件。往带命名空间的目录加类时，若类名已在全局池里被引用，混用会出歧义。
3. **`SaveableService` 子类重写 `OnSingletonInitialized` 必须调 `base`。** 注册动作在基类里（`SaveableService.cs:14-17`），漏调 `base.OnSingletonInitialized()` 不报错、不警告，该服务只是**不再进 `SaveRegistry`**，存读档时被静默跳过。
4. **`Contracts/` 不得反向 using `Gameplay.*` / `Pipeline.*`。** 契约层被所有域引用，一旦反向依赖就形成环；当前实测 0 处，保持住。
5. **`StatsService.Instance.Stats` 是只读视图，不要试图往接口上加写方法。** 接口的存在意义就是让外部拿不到写入口；有测试盯着 `StatsService` 不暴露具体 Model。
6. **改 `PlayerStatsData` 的字段名等于坏档。** 字段名就是存档 JSON 键（`PlayerStatsData.cs:6-9`）；类名可以改，字段名不可以。
7. **纯 C# 层（Model / 三个显示侧 Controller / `CanvasFocusStack`）不得引用 `MonoBehaviour` 生命周期**，否则 EditMode 测试跑不起来——现有 52 个用例的一半依赖这条。

## 4. 已知缺陷与风险

**D1：`Pipeline` 反向依赖 `Gameplay`（唯一一处）。** `Assets\Scripts\Pipeline\SO\PlayerStatsSO.cs:2` 引入 `Gameplay.Player.Models`，全工程仅此一处跨层反向引用。后果是管线层无法独立于玩法域编译，`PlayerStatsData` 改名会同时打到管线和存档两处。

**D2：`PlayerStatsSO.Data` 泄漏资产内部引用。** `Assets\Scripts\Pipeline\SO\PlayerStatsSO.cs:14` 的 `Data => stats` 直接返回序列化字段本体，不是拷贝。测试为此绕开了它——用 `so.Data.damage = 7` 写配置，再用 `CreateInitialData()` 验拷贝（`PlayerStatsSOTests.cs:30-38`），因此"外部拿到 `Data` 就能改资产"这条没有被任何测试约束。

**D3：`Contracts/ISaveable.cs` 同时定义数据契约与注册动作。** `RegisterSaveable` / `UnRegisterSaveable` 带默认实现直接写静态 `SaveRegistry`（`Assets\Scripts\Contracts\ISaveable.cs:7-14`），而 `SaveableService` 走的是另一条注册路径（`OnSingletonInitialized` 里直接 `SaveRegistry.Add`）。同一件事有两条入口，谁在什么时候调用默认实现没有文档约束。

**D4：MVCS 现状文档的 Addressables 用例数仍无法复核（原「工程侧数据过期」指控已消失）。** 该文档已随本轮改动同步：`Docs\My_ARPG_MVCS项目现状.md:180` 记「5 个文件、共 52 个用例」，与实测的 52 个 `[Test]` 一致；`:190` 记「加上 Addressables 包自带的 1 个，Test Runner 里共 53 个」。仍存疑的只有包侧那个「1 个」：实测 `Library\PackageCache\com.unity.addressables@1.22.3\Tests\` 下 209 个 .cs 含 **993 个 `[Test]`**，远不止 1 个（该包是否有测试 asmdef 被启用、Test Runner 实际列出多少，未运行 Unity 验证）。原先指向「4 个文件 46 个用例 / 47 个」的过期指控已随该文档更新失效。

**D5：旧版标题序号重复已修复，本条不再成立（保留记录）。** 现行 `Docs\My_ARPG_MVCS项目现状.md` 的编号标题依次是 `### 1.1`（`:13`）、`### 1.2`（`:35`）、`### 1.3`（`:49`）、`### 1.4`（`:59`）、`### 1.5`（`:68`）、`### 1.6`（`:88`），各出现一次，既没有重复的 1.5，也没有 1.4 错序。原先「`:53` 与 `:82` 都是 `### 1.5`、`:62` 的 `### 1.4` 夹在中间」的描述已与文件不符。

**D6：`ICanvasManager` 拼写不一致。** 同一接口里是 `SetCanvaInactive` / `SetCanvaState` / `RefreshCanvaOrder`（`Assets\Scripts\Contracts\ICanvasManager.cs:53,66,85`），而 `StatsPanelView` 里用 `((ICanvasManager)this).SetCanvaInactive(...)` 显式转型调用（`Assets\Scripts\Gameplay\Player\Views\StatsPanelView.cs:61,67,73`）。改名会同时打到所有面板实现类。

**D7：`ICanvasManager` 默认实现直接依赖具体 `UIManager`。** 接口的默认方法体里调 `UIManager.Report(...)` 与 `UIManager.Instance`（`ICanvasManager.cs:75,90-93`），契约层因此隐式绑定到 `Pipeline` 的具体单例，与 §3.4"契约层不依赖实现"的纪律相悖（当前靠"类型在全局命名空间、无需 using"绕过，实测 `Contracts/` 里 0 条 `using`）。

## 5. 未核验事项

- 假设：52 个 `[Test]` 在 Unity Test Runner 里全部通过，且失败数为 0。（未运行 Unity 验证；本任务禁止启动编辑器）
- 假设：`Assets/Tests/Editor/` 缺少 asmdef 时仍能被 Test Runner 的 EditMode 平台发现并执行。（未运行 Unity 验证；`Docs\My_ARPG_MVCS项目现状.md:190` 现称"Test Runner 里共 53 个"，但该数字含 Addressables 包侧用例，本次未运行 Test Runner 复核）
- 假设：`Pipeline/SO/PlayerStatsSO.cs` 是全工程唯一 `Pipeline → Gameplay` 引用，没有通过反射、字符串或 Addressables 资产路径形成的隐式反向依赖。（未运行 Unity 验证；仅覆盖 `using` 指令的静态文本检索）
- 假设：`Assembly-CSharp` 是唯一产品程序集，`Assets/Tests/Editor` 也编译其中。（未运行 Unity 验证；依据是 `Assets/` 下 `*.asmdef` 实测计数为 0，未读取 `ProjectSettings/` 与 `Packages/manifest.json` 交叉确认）
- 假设：全局命名空间的 111 个类之间当前没有标识符冲突（能编译即无冲突，但未实际编译）。
- 假设：`SaveableService` 当前只有 `StatsService` 一个子类（依据是 `: SaveableService<` 的文本检索仅命中声明行与 `StatsService`；未运行 Unity 验证）。
- 假设：`StatsService` 的 `[SerializeField] private PlayerStatsSO statsConfig` 在所有场景里都已接线，否则其 `OnSingletonInitialized` 会在 `StatsService.cs:29` 抛空引用。（未打开场景/预制体验证）
- 假设：`Gameplay/Player` 下 6 个全局命名空间类（移动/战斗/弓箭线）确实没有走 MVCS 四层，而非在别处另有分层实现。（依据是目录实测，未逐个通读其内部实现）
