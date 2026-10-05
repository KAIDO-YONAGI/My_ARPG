# 程序集与热更规划 Proposal

> **本文件是 Proposal：以下全部内容均为「未实施」的设计与评估结论，不得当作当前能力引用、不得据此推断代码已具备该结构。**
> 唯一事实来源是源码；与本文件冲突时以代码为准。「当前状态」一栏记录静态核对的结果，作用是把设计锚在真实起点上。

文档 ID：`ARCH-ASSEMBLY-PLAN`
状态：`Proposal`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只记录「程序集拆分 / 热更接入 / 效果数据驱动 / 事件机制演进」五项**未实施**设计的目标、理由、当前状态与前置依赖。已实现架构的事实见 `Architecture\Composition\Composition_Guide.md` 与各业务域 Guide；本文件不构成立项或排期承诺。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `asmdef`、`Assembly Definition`、`程序集拆分`、`编译时间` | §3.1 |
| `xLua`、`热更`、`LuaManager`、`LuaScripts` | §3.2 |
| `效果数据驱动`、`状态机基类`、`switch(skillName)` | §3.3 |
| `事件总线`、`EventBus`、`通道集中定义` | §3.4 |
| `事件引用可视化`、`订阅链查看器`、`接线图`、`EventGraph` | §3.5 |

## 2. 当前实现

**本域无当前实现。** 下述五项全部为未实施设计，本节只写名称与状态索引，实质内容在 §3。

| # | 事项 | 状态 |
|---|---|---|
| 1 | Assembly Definition 拆分 | 未实施 |
| 2 | xLua 热更新 MVP | 未实施 |
| 3 | 技能/物品效果数据驱动 + 状态机基类 | 未实施 |
| 4 | 事件总线 | 未实施，当前决策为维持不采用 |
| 5 | 事件引用可视化插件 | 未实施，现有可行性探讨 |

## 3. 未实施设计（逐项：目标 / 理由 / 当前状态 / 前置依赖）

### 3.1 Assembly Definition 拆分

- **目标**：新增 Assembly Definition 资产，把当前全部产品代码所在的预定义程序集 `Assembly-CSharp` 拆为 `Contracts` / `SO` / `Gameplay.Core` / 按功能域 / `UI` / `Units` / `Save` / `Pathfinding`；并为编辑器测试程序集建立 Assembly Definition、引用被测程序集。
- **理由**：程序集边界是唯一能在编译期强制依赖方向的机制；拆分同时缩短增量编译时间。
- **当前状态：未实施**。工程内 Assembly Definition 资产数量为 **0**，测试程序集下同为 **0**；5 个测试类型（`AStarOpenHeapTests`、`CanvasFocusStackTests`、`ObjectPoolTests`、`PlayerStatsModelTests`、`PlayerStatsSOTests`）仍在预定义程序集里。
- **前置依赖**：
  1. 反向引用先消除，否则拆分会直接编译失败。已识别的候选是 `ICanvasManager` 的默认实现直读 `UIManager.Instance`，即 `Contracts` 层指向 `UI` 层的硬引用，详见 `Architecture\Composition\Composition_Guide.md` §2.6。
  2. 测试程序集的 Assembly Definition 需与产品程序集同步建立，否则拆完后测试会失去对被测类型的可见性。

### 3.2 xLua 热更新 MVP

- **目标**：接入 xLua，建立单例 `LuaManager`、桥接层与 Addressables 的 `LuaScripts` 分组，先下沉战斗、数值、道具公式，跑通「改 Lua → 重打 Addressables → 客户端拉新字节码 → 数值变化，不发版」的闭环。
- **理由**：数值与公式类改动当前必须发版；热更把这类改动变成资源下发。
- **当前状态：未实施**。xLua 源码目录与 `LuaScripts` 目录均未建立，Lua 脚本数量为 **0**，`LuaManager` 类型未定义。初始化入口 `InitialLoad` 的 `Awake` 当前只执行 `PersistentSceneRegistry.Register` 与常驻场景加载协程。
- **前置依赖**：
  1. xLua 插件源码与 native 插件需先导入：工程当前无 xLua 源码；托管依赖现有 `Newtonsoft` 一项，xLua 的 native 插件待补齐。
  2. Addressables 已接入，`AddressableAssetsData` 配置已存在，热更下发可直接复用。
  3. `XLuaGenConfig` 的桥接类型清单需按当前代码重新选型后才能生成 Wrap 代码。
  4. 初始化顺序：`LuaManager.Init` 放在 `InitialLoad.Awake` 最前会早于各内容场景实例，与 `OnSingletonInitialized` 惯用法的取舍需先定，见 `Architecture\Composition\Composition_Guide.md` §2.3.1。

### 3.3 技能/物品效果数据驱动 + 状态机基类

- **目标**：技能与物品效果改为「效果列表 + 数据驱动」执行，取代按名、按字段的硬编码分支；引入状态机基类，取代手写状态分支。
- **理由**：当前每加一个技能或道具字段都要改代码；状态逻辑分散在 if/else 链里，行为不可组合。
- **当前状态：未实施**。
  - `SkillManager` 取 `skillSlot.skillSO.skillName` 后 `switch (skillName)` 分派，属按字符串分派。
  - `UseItem` 的 `ApplyItemEffects(ItemSO item)` 按 `maxHealth` / `currentHealth` / `speed` / `damage` / `duration` 逐个 `if (> 0)` 判断，属按字段硬编码。
  - `PlayerMovement` 以 `switch (playerState)` 执行状态逻辑，另有 `if/else if` 状态链负责动画。
  - `MovementController` 类型在工程中未定义，状态机重构的起点是 `PlayerMovement`。
- **前置依赖**：
  1. 数值写入入口已收敛到 `StatsService`，方法为 `UpdateMaxHealth` / `UpdateHealth` / `UpdateSpeed` / `UpdateDamage` / `UpdateSkillPoints` / `AddExperience`；效果列表要作用的写方法已稳定，可直接作为驱动目标。
  2. 状态枚举现有三套彼此独立的定义：`MyEnums.PlayerState`、`EnemyState`、`NPCState`。状态机基类要统到什么粒度需先决策，否则会出现基类覆盖不了第三套的返工。

### 3.4 事件总线：维持不采用

- **若实施的目标**：建立中心化 `EventBus`，所有事件通道集中定义。
- **理由与不采用依据**：收益仅限于「通道集中定义」；订阅关系的可见性由 §3.5 的可视化工具覆盖。成本是 14 条通道全量重接线，并放弃事件 SO 资产在 Inspector 里的工作流。当前规模下收益不覆盖成本，故维持不采用。
- **当前状态：未实施**。`EventBus` 类型未定义。现行机制是事件 SO：工程内共 **12 个事件类**，每类一个文件，全部直接继承 `ScriptableObject`、无公共基类；`public event` 声明共 **14 条**，其中 `ToggleCanvasEventSO` 承载 toggle 与 focus 两条通道、`ShopKeeperEventSO` 承载进入与离开两条通道，其余 10 类各 1 条。订阅惯例是 `OnEnable` 订阅、`OnDisable` 或 `OnDestroy` 退订，发布方调各类的 `RaiseXxx()`。
- **前置依赖 / 重评估触发**：通道数量或跨系统事件明显增多时重评估。届时实施前须先定退订纪律；现行做法靠成对退订兜底，总线化会改变订阅失效的模式。

### 3.5 事件引用可视化插件

- **目标**：纯 Editor 工具，落在编辑器程序集的 `EventGraph` 目录。分两档：**L1** 是运行时订阅链查看器，在 Play 模式用反射读事件后备字段与 `Delegate.GetInvocationList()`，用 `EditorGUIUtility.PingObject` 定位；**L2** 是编辑模式接线图，扫场景、预制体与资产上的 GUID 引用，产出正向、反向、全局三视图。**L3** 发布埋点为不做项。
- **理由**：SO 加 Inspector 工作流的短板是关系不可见；L2 任何时候可查，是编辑期排障的补丁；L1 是排查订阅方收不到事件最快的手段。
- **当前状态：未实施**。现有可行性探讨文字，编辑器程序集内没有 `EventGraph` 相关实现。
- **前置依赖**：
  1. 零运行时代码改动，零包依赖；编辑器程序集天然不进构建。
  2. L2 的引用解析须走 `AssetDatabase` 与 `PrefabUtility` API，以覆盖 nested prefab 与 override 细节，这是主要工作量。
  3. 事件资产规模按静态核对计，未运行编辑器验证：工程内共 **28 个** 事件资产，其中根目录 9、`ToggleCanvasEvents` 10、`VoidEvents` 6、`InventorySlotsStatsEvents` 3；引用点几十处，远低于性能敏感区。
  4. 立项触发条件：下次事件链路排查耗时超过半天，或新增事件通道前需要全局总览。

## 4. 约定与硬边界

1. **本文件任何条目都不得被写成「已实现」。** 引用本域内容时必须带 `Proposal` 状态与「未实施」限定语；把它当成现状是最高风险误用。
2. **§3 各项的「当前状态」一栏会随代码演进失效。** 复核时以源码为准重跑一次核对，不要直接复用本文数字。
3. **拆分与热更须在消除反向引用后动手。** 见 §3.1 前置依赖 1，违反的直接后果是编译失败。
4. **事件机制的现行约定不在本域。** 事件 SO 的订阅、退订纪律属于各业务域 Guide 与 `Architecture\Composition\Composition_Guide.md` §2.6；本域只记录事件总线的取舍理由，不定义事件契约。

## 5. 已知缺陷与风险

1. 工程无 Assembly Definition，「依赖方向」当前只靠约定约束：任何跨域 `using` 都能编译通过，`ICanvasManager → UIManager` 这类反向引用会持续被复制。
2. 效果数据驱动与状态机基类共享一个前置：三套状态枚举独立定义，先做效果驱动会让后续状态机迁移二次改动技能与道具代码。

## 6. 未核验事项

1. 事件计数 12 类 / 14 条 `public event` / 28 个资产来自事件 SO 目录的逐个实读；该目录之外是否还有其他事件通道待核验，工程内 `event` 声明的全量检索待编辑器实测。
2. 假设：`AddressableAssetsData` 配置足以支撑热更下发；其分组、Remote 标记与 CDN 配置待核验。
3. 假设：编辑器测试程序集建立 Assembly Definition 后仍能访问被测类型；编译验证待编辑器实测。
4. 假设：`ICanvasManager → UIManager` 是拆分 `Contracts` 层的主要阻塞点；完整的跨域引用拓扑扫描待做。
