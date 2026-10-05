# 程序集与热更规划 Proposal

> **本文件是 Proposal：以下全部内容均为「未实施」的设计与评估结论，不得当作当前能力引用、不得据此推断代码已具备该结构。**
> 唯一事实来源仍是源码；与本文件冲突时以代码为准。所有「当前状态」一栏都是对本轮静态核对的记录，作用是把设计锚在真实起点上。

文档 ID：`ARCH-ASSEMBLY-PLAN`
状态：`Proposal`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责记录「程序集拆分 / 热更接入 / 效果数据驱动 / 事件机制演进」五项**未实施**设计的目标、理由、当前状态与前置依赖。不负责已实现架构的事实（见 `Architecture\Composition\Composition_Guide.md` 与各业务域 Guide），也不构成立项或排期承诺。
上游来源：
- `Docs/My_ARPG_重构优化清单_未解决.md`（主要线索，含 xLua 完整方案附录；**该文件已由用户在 2026-10-05 删除**，内容见 `git HEAD:Docs/My_ARPG_重构优化清单_未解决.md`，本文件对它的引用一律标注为 git HEAD 版本）
- 代码核对起点：`Assets/Scripts/Gameplay/Skills/SkillManager.cs`、`Gameplay/Inventory/UseItem.cs`、`Gameplay/Player/PlayerMovement.cs`、`Pipeline/SO/Events/*.cs`、`Assets/**` 全量 asmdef / lua / Addressables 检索

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `asmdef`、`Assembly Definition`、`程序集拆分`、`编译时间` | §2.1 |
| `xLua`、`热更`、`LuaManager`、`LuaScripts` | §2.2 |
| `效果数据驱动`、`状态机基类`、`switch(skillName)` | §2.3 |
| `事件总线`、`EventBus`、`通道集中定义` | §2.4 |
| `事件引用可视化`、`订阅链查看器`、`接线图`、`EventGraph` | §2.5 |

## 2. 当前实现

**本域无当前实现。** 下述五项全部为未实施设计，故本节只写名称与状态索引，实质内容在 §3。

| # | 事项 | 状态 |
|---|---|---|
| 1 | Assembly Definition 拆分 | 未实施（原线索 `git HEAD:Docs/My_ARPG_重构优化清单_未解决.md` 标 ⏸ 已排期；该文件已由用户在 2026-10-05 删除） |
| 2 | xLua 热更新 MVP | 未实施（同源标记随该文件归入 git HEAD，排期未与用户复核） |
| 3 | 技能/物品效果数据驱动 + 状态机基类 | 未实施（同上，用户决策留存） |
| 4 | 事件总线 | 未实施，且当前决策为**维持不采用** |
| 5 | 事件引用可视化插件 | 未实施（仅有可行性探讨，无代码） |

## 3. 未实施设计（逐项：目标 / 理由 / 当前状态 / 前置依赖）

### 3.1 Assembly Definition 拆分

- **目标**：新增 `.asmdef`，把当前全部产品代码所在的预定义程序集 `Assembly-CSharp` 拆为 `Contracts` / `SO` / `Gameplay.Core` / 按功能域 / `UI` / `Units` / `Save` / `Pathfinding`；并为 `Assets/Tests/Editor/` 建测试 asmdef 并引用被测程序集。
- **理由**：程序集边界是唯一能在编译期强制依赖方向的机制；拆分同时缩短增量编译时间。
- **当前状态（未实施）**：`Assets/**` 下 `*.asmdef` 数量为 **0**，`Assets/Tests/**` 下同为 **0**；5 个测试文件（`AStarOpenHeapTests`、`CanvasFocusStackTests`、`ObjectPoolTests`、`PlayerStatsModelTests`、`PlayerStatsSOTests`）仍在预定义程序集里。
- **前置依赖**：
  1. 反向引用先消除，否则拆分会直接编译失败。已识别的候选：`ICanvasManager` 的默认实现直读 `UIManager.Instance`（`Assets/Scripts/Contracts/ICanvasManager.cs:90-93`、`:75`），这是 `Contracts` 层指向 `UI` 层的硬引用，详见 `Architecture\Composition\Composition_Guide.md` §2.6。
  2. 原线索文档记录的旧障碍已自然消失：`git HEAD:Docs/My_ARPG_重构优化清单_未解决.md:21` 记「StatsManager→UI 反向调用」为拆分障碍，而 `StatsManager` 类在当前代码中已不存在（检索 `class StatsManager` 零命中），数值入口现为 `Gameplay.Player.Services.StatsService`（该线索文件已由用户在 2026-10-05 删除）。
  3. 测试 asmdef 需与产品 asmdef 同步建立，否则拆完测试会失去对被测类型的可见性。

### 3.2 xLua 热更新 MVP

- **目标**：接入 xLua，建立 `LuaManager`（单例）+ 桥接层 + `Addressables` 的 `LuaScripts` 分组，先下沉战斗/数值/道具公式，跑通「改 Lua → 重打 Addressables → 客户端拉新字节码 → 数值变化，不发版」的闭环。
- **理由**：数值与公式类改动当前必须发版；热更把这类改动变成资源下发。
- **当前状态（未实施）**：`Assets/xLua/` 不存在、`Assets/LuaScripts/` 不存在、`Assets/**` 下 `.lua` 文件数为 **0**、`LuaManager` 类零命中。方案文档的 `InitialLoad.cs` 初始化入口尚未加任何 Lua 调用（`Assets/Scripts/Pipeline/InitialLoad.cs:11-15` 目前只做 `PersistentSceneRegistry.Register` 与常驻场景加载协程）。
- **前置依赖**：
  1. xLua 插件源码与 native 插件需先导入：`Assets/xLua/` **当前不存在**；`Assets/Plugins/` **已存在，但只有 `Newtonsoft.Json.dll` 一个托管依赖**，没有任何 xLua 的 native 插件，故该前置项仍未满足。
  2. `Assets/AddressableAssetsData/` **已存在**，热更下发所依赖的 Addressables 已接入，可直接复用（该前置项满足）。
  3. 附录方案中的类名需按当前代码校正：示例引用的 `StatsManager`、`StatsBridge` 在当前代码中都不存在；数值入口是 `StatsService`（`Gameplay/Player/Services/StatsService.cs:16`），`XLuaGenConfig` 的桥接类型清单必须重新选型后才能生成 Wrap 代码。
  4. 单例初始化顺序：方案自列风险项为「`LuaManager.Init` 放 `InitialLoad.Awake` 最前」；需注意 `InitialLoad.Awake` 早于各内容场景实例，与 `OnSingletonInitialized` 惯用法（`Composition_Guide.md` §2.3.1）的取舍要先定。

### 3.3 技能/物品效果数据驱动 + 状态机基类

- **目标**：技能与物品效果改为「效果列表 + 数据驱动」执行，取代按名/按字段的硬编码分支；引入状态机基类，取代手写状态分支。
- **理由**：当前每加一个技能或道具字段都要改代码；状态逻辑分散在 if/else 链里，行为不可组合。
- **当前状态（未实施）**：
  - `Assets/Scripts/Gameplay/Skills/SkillManager.cs:19-21`：取 `skillSlot.skillSO.skillName` 后 `switch (skillName)`，属按字符串分派。
  - `Assets/Scripts/Gameplay/Inventory/UseItem.cs:8-19`：`ApplyItemEffects(ItemSO item)` 按 `maxHealth`/`currentHealth`/`speed`/`damage`/`duration` 逐个 `if (> 0)` 判断，属按字段硬编码。
  - `Assets/Scripts/Gameplay/Player/PlayerMovement.cs:171`：`switch (playerState)` 执行逻辑；`:95-130` 另有 `if/else if` 状态链负责动画。
  - `MovementController.cs` **不存在**（检索 `class MovementController` 零命中），方案文档引用的「`MovementController.cs:187` 作者重构 TODO」是失效指针。
- **前置依赖**：
  1. 数值写入入口已收敛到 `StatsService`（`UpdateMaxHealth/UpdateHealth/UpdateSpeed/UpdateDamage/UpdateSkillPoints/AddExperience`，`StatsService.cs:40-50`），效果列表要作用的写方法已稳定，可直接作为驱动目标。
  2. 状态枚举目前是三套彼此独立的定义：`MyEnums.PlayerState`、`EnemyState`、`NPCState`（`Assets/Scripts/Contracts/MyEnums.cs:3-18`、`:39-45`）。状态机基类要统到什么粒度需先决策，否则会出现基类管不了第三套的返工。

### 3.4 事件总线（维持不采用）

- **目标（若实施）**：建立中心化 `EventBus`，所有事件通道集中定义。
- **理由与不采用依据**：收益仅限于「通道集中定义」，**不解决**订阅关系可见性；成本是 14 条通道全量重接线并放弃 SO 资产在 Inspector 里的工作流。当前规模下收益不覆盖成本，故**维持不采用**；且 §3.5 的可视化工具恰是针对 SO 方案短板的补丁，补齐后更无必要上总线。
- **当前状态（未实施）**：无 `EventBus` 类。现行机制是事件 SO：`Assets/Scripts/Pipeline/SO/Events/` 下 **12 个事件类**（12 个 .cs，每文件一个类），全部直接继承 `ScriptableObject`、无公共基类；`public event` 声明共 **14 条**（`ToggleCanvasEventSO` 为 toggle + focus 双通道、`ShopKeeperEventSO` 为进入 + 离开双通道，其余 10 类各 1 条）。订阅惯例为 `OnEnable` 订阅 / `OnDisable` 或 `OnDestroy` 退订，发布方调各类 `RaiseXxx()`。
- **前置依赖 / 重评估触发**：通道数量或跨系统事件明显增多时重评估。若届时实施，须先定退订纪律（当前靠成对退订兜底，总线化后失效模式会变）。

### 3.5 事件引用可视化插件

- **目标**：纯 Editor 工具，落点 `Assets/Editor/EventGraph/`。分两档：**L1** 运行时订阅链查看器（Play 模式，反射读事件后备字段 + `Delegate.GetInvocationList()`，`EditorGUIUtility.PingObject` 定位）；**L2** 编辑模式接线图（扫 `.unity`/`.prefab`/`.asset` 的 GUID 引用，产出正向/反向/全局三视图）。**L3** 发布埋点结论为不做。
- **理由**：SO + Inspector 工作流的唯一短板是「关系不可见」，L2 任何时候可查，是编辑期排障的补丁；L1 是排查「为什么没收到事件」最快的手段。
- **当前状态（未实施）**：仅有可行性探讨文字，无任何代码；`Assets/Editor/` 下无 `EventGraph` 相关实现。
- **前置依赖**：
  1. 零运行时代码改动、零包依赖（Editor 目录程序集天然不进构建）。
  2. L2 的引用解析必须走 `AssetDatabase` / `PrefabUtility` API 而非裸 YAML 文本解析，以规避 nested prefab 与 override 细节——这是方案自认的主要工作量所在。
  3. 事件资产规模（本轮实测）：`Assets/GameSO/Events/` 下共 **28 个** 事件资产（根目录 9、`ToggleCanvasEvents` 10、`VoidEvents` 6、`InventorySlotsStatsEvents` 3）；引用点几十处，远低于性能敏感区。
  4. 立项触发条件：下次事件链路排查耗时超过半天，或新增事件通道前需要全局总览。

## 4. 约定与硬边界

1. **本文件任何条目都不得被写成「已实现」。** 引用本域内容时必须带 `Proposal` 状态与「未实施」限定语；把它当成现状是最高风险误用。
2. **§3 各项的「当前状态」一栏会随代码演进失效。** 复核时以源码为准重跑一次检索，不要直接复用本文数字。
3. **拆分/热更不得在未消除反向引用前动手。** 见 §3.1 前置依赖 1，违反的直接后果是编译失败而非运行期问题。
4. **事件机制的现行约定不在本域。** 事件 SO 的订阅-退订纪律属于各业务域 Guide 与 `Composition_Guide.md` §2.6；本域只记录「为什么不总线化」的决策，不定义事件契约。

## 5. 已知缺陷与风险

1. 上游线索文档的事件类路径已失效：`git HEAD:Docs/My_ARPG_重构优化清单_未解决.md:73` 写 `Assets/Scripts/Gameplay/SO/Events/`，实际为 `Assets/Scripts/Pipeline/SO/Events/`。（该线索文件已由用户在 2026-10-05 删除；本条与以下 3 条的行号均指 git HEAD 版本。）
2. 同文档的事件资产计数与实测不符：`git HEAD:...:74` 记「约 24 个（VoidEvents 5 / ToggleCanvasEvents 9 / Events 根目录 7 / InventorySlotsStatsEvents 3）」，本轮实测为 28 个（根 9 / ToggleCanvasEvents 10 / VoidEvents 6 / InventorySlotsStatsEvents 3）。
3. 同文档引用的 `MovementController.cs:187`（`git HEAD:...:32`）指向不存在的类（检索 `class MovementController` 零命中），无法作为状态机重构的起点。
4. xLua 附录（`git HEAD:...:147-177`）的骨架代码引用了不存在的 `StatsManager` 与 `StatsBridge`（两者检索均零命中），照抄会直接编译不过。
5. 无 asmdef 意味着「依赖方向」当前只是口头约定：任何跨域 `using` 都能编译通过，`ICanvasManager → UIManager` 这类反向引用会持续被复制。
6. 效果数据驱动缺状态机基类的统一前置：三套状态枚举独立定义，先做效果驱动会让后续状态机迁移二次改动技能/道具代码。

## 6. 未核验事项

1. 未决：原线索文档 `Docs/My_ARPG_重构优化清单_未解决.md` 已由用户在 2026-10-05 删除，其「⏸ 已排期 / 留存」标记现只能从 `git HEAD:Docs/My_ARPG_重构优化清单_未解决.md` 读取；这些标记是否仍反映用户当前意愿，本轮未与用户确认。
2. 事件计数已按 `Assets/Scripts/Pipeline/SO/Events/` 全目录实读修正为 12 类 / 14 条 `public event` / 28 个资产（§3.4、§3.5）。仍未核验的是「该目录之外是否还有事件通道」——本轮只逐个读了该目录，未在 `Assets/**` 全量检索其他 `event` 声明（未运行 Unity 验证）。
3. 假设：`Assets/AddressableAssetsData/` 的存在等价于 Addressables 可直接用于热更下发——未核对其分组、Remote 标记与 CDN 配置。
4. 假设：为 `Assets/Tests/Editor/` 建 asmdef 后测试仍能访问被测类型——未实际试拆（未运行 Unity，未做编译验证）。
5. 假设：`ICanvasManager → UIManager` 是拆分 `Contracts` 层的主要阻塞点——未做完整的跨域引用拓扑扫描，可能还有其他同类反向引用（未运行静态分析工具）。
