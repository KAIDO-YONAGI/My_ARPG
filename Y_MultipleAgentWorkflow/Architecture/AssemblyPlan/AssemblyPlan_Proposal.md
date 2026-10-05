# 程序集与热更规划 Proposal

> **本文件为 Proposal：以下全部内容均为未实施设计，不作为当前能力引用。**

文档 ID：`ARCH-ASSEMBLY-PLAN`
状态：`Proposal`
最后更新：`2026-10-05`
权威范围：记录程序集拆分、热更接入、效果数据驱动、事件机制演进五项**未实施**设计的目标、理由、当前状态与前置依赖。已实现架构的事实见 `..\Composition\Composition_Guide.md` 与各业务域 Guide。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `asmdef`、`Assembly Definition`、`程序集拆分`、`编译时间` | 本文 §3.1 |
| `xLua`、`热更`、`LuaManager`、`LuaScripts` | 本文 §3.2 |
| `效果数据驱动`、`状态机基类`、`switch(skillName)` | 本文 §3.3 |
| `事件总线`、`EventBus`、`通道集中定义` | 本文 §3.4 |
| `事件引用可视化`、`订阅链查看器`、`接线图`、`EventGraph` | 本文 §3.5 |

## 2. 当前实现

本域无当前实现。下述五项均为未实施设计，本节只写名称与状态索引，实质内容在 §3。

| # | 事项 | 状态 |
|---|---|---|
| 1 | Assembly Definition 拆分 | 未实施 |
| 2 | xLua 热更新 MVP | 未实施 |
| 3 | 技能/物品效果数据驱动 + 状态机基类 | 未实施 |
| 4 | 事件总线 | 未实施，当前决策为维持不采用 |
| 5 | 事件引用可视化插件 | 未实施，现有可行性探讨 |

## 3. 未实施设计

每项按目标、理由、当前状态、前置依赖四要素记录。

### 3.1 Assembly Definition 拆分

- **目标**：新增 Assembly Definition 资产，把当前全部产品代码所在的预定义程序集 `Assembly-CSharp` 拆为 `Contracts` / `SO` / `Gameplay.Core` / 按功能域 / `UI` / `Units` / `Save` / `Pathfinding`；并为编辑器测试程序集建立 Assembly Definition、引用被测程序集。
- **理由**：程序集边界是唯一能在编译期强制依赖方向的机制；拆分同时缩短增量编译时间。
- **当前状态：未实施**。工程内 Assembly Definition 资产数量为 **0**，测试程序集下同为 **0**；5 个测试类型 `AStarOpenHeapTests` `CanvasFocusStackTests` `ObjectPoolTests` `PlayerStatsModelTests` `PlayerStatsSOTests` 仍在预定义程序集里。
- **前置依赖**：
  1. 反向引用先消除。已识别的候选是 `ICanvasManager` 的默认实现直读 `UIManager.Instance`，即 `Contracts` 层指向 `UI` 层的硬引用，见 `..\Composition\Composition_Guide.md` §2.6。
  2. 测试程序集的 Assembly Definition 需与产品程序集同步建立，测试程序集才能看见被测类型。

### 3.2 xLua 热更新 MVP

- **目标**：接入 xLua，建立单例 `LuaManager`、桥接层与 Addressables 的 `LuaScripts` 分组，先下沉战斗、数值、道具公式，跑通「改 Lua → 重打 Addressables → 客户端拉新字节码 → 数值变化，不发版」的闭环。
- **理由**：数值与公式类改动当前必须发版；热更把这类改动变成资源下发。
- **当前状态：未实施**。xLua 源码目录与 `LuaScripts` 目录均未建立，Lua 脚本数量为 **0**，`LuaManager` 类型未定义。初始化入口 `InitialLoad` 的 `Awake` 当前只执行 `PersistentSceneRegistry.Register` 与常驻场景加载协程。
- **前置依赖**：
  1. xLua 插件源码与 native 插件需先导入：工程当前无 xLua 源码；托管依赖现有 `Newtonsoft` 一项，xLua 的 native 插件待补齐。
  2. Addressables 已接入，`AddressableAssetsData` 配置已存在，热更下发可直接复用。
  3. `XLuaGenConfig` 的桥接类型清单按当前代码选型后生成 Wrap 代码。
  4. 初始化顺序：`LuaManager.Init` 放在 `InitialLoad.Awake` 最前，会早于各内容场景实例；`OnSingletonInitialized` 惯用法见 `..\Composition\Composition_Guide.md` §2.3.1。

### 3.3 技能/物品效果数据驱动 + 状态机基类

- **目标**：技能与物品效果改为「效果列表 + 数据驱动」执行，取代按名、按字段的硬编码分支；引入状态机基类，取代手写状态分支。
- **理由**：当前每加一个技能或道具字段都要改代码；状态逻辑分散在 if/else 链里。
- **当前状态：未实施**。
  - `SkillManager` 取 `skillSlot.skillSO.skillName` 后 `switch (skillName)` 分派，属按字符串分派。
  - `UseItem` 的 `ApplyItemEffects(ItemSO item)` 按 `maxHealth` / `currentHealth` / `speed` / `damage` / `duration` 逐个 `if (> 0)` 判断，属按字段硬编码。
  - `PlayerMovement` 以 `switch (playerState)` 执行状态逻辑，另有 `if/else if` 状态链负责动画。
  - `MovementController` 类型在工程中未定义，状态机重构的起点是 `PlayerMovement`。
- **前置依赖**：
  1. 数值写入入口已收敛到 `StatsService`，方法为 `UpdateMaxHealth` / `UpdateHealth` / `UpdateSpeed` / `UpdateDamage` / `UpdateSkillPoints` / `AddExperience`，效果列表要作用的写方法已稳定，可直接作为驱动目标。
  2. 状态枚举现有三套彼此独立的定义：`MyEnums.PlayerState`、`EnemyState`、`NPCState`。

### 3.4 事件总线：维持不采用

- **若实施的目标**：建立中心化 `EventBus`，所有事件通道集中定义。
- **当前状态：未实施**。`EventBus` 类型未定义。现行机制是事件 SO：工程内共 **12 个事件类**，每类一个文件，全部直接继承 `ScriptableObject`、无公共基类；`public event` 声明共 **14 条**，其中 `ToggleCanvasEventSO` 承载 toggle 与 focus 两条通道、`ShopKeeperEventSO` 承载进入与离开两条通道，其余 10 类各 1 条。订阅惯例是 `OnEnable` 订阅、`OnDisable` 或 `OnDestroy` 退订，发布方调各类的 `RaiseXxx()`。

### 3.5 事件引用可视化插件

- **目标**：纯 Editor 工具，落在编辑器程序集的 `EventGraph` 目录。分两档：**L1** 是运行时订阅链查看器，在 Play 模式用反射读事件后备字段与 `Delegate.GetInvocationList()`，用 `EditorGUIUtility.PingObject` 定位；**L2** 是编辑模式接线图，扫场景、预制体与资产上的 GUID 引用，产出正向、反向、全局三视图。**L3** 发布埋点为不做项。
- **当前状态：未实施**。现有可行性探讨文字，编辑器程序集内没有 `EventGraph` 相关实现。
- **前置依赖**：
  1. 零运行时代码改动，零包依赖；编辑器程序集天然不进构建。
  2. L2 的引用解析走 `AssetDatabase` 与 `PrefabUtility` API，覆盖 nested prefab 与 override 细节。
  3. 事件资产规模：工程内共 **28 个** 事件资产，其中根目录 9、`ToggleCanvasEvents` 10、`VoidEvents` 6、`InventorySlotsStatsEvents` 3；引用点几十处。

## 4. 约定与硬边界

1. **本文件任何条目都不得被写成「已实现」。** 引用本域内容时必须带 `Proposal` 状态与「未实施」限定语。
2. **拆分与热更须在消除反向引用后动手。** 见 §3.1 前置依赖 1。
3. **事件机制的现行约定不在本域。** 事件 SO 的订阅与退订纪律属于各业务域 Guide 与 `..\Composition\Composition_Guide.md` §2.6；本域记录事件总线的设计条目，不定义事件契约。
