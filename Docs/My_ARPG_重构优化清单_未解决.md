# My_ARPG 重构优化清单 · 未解决

> 2026-09-13 从《My_ARPG_重构优化清单》拆分。本文档只保留**未完成/待评估项**；
> 已完成项的实现与验证记录见《My_ARPG_重构优化清单_已解决.md》（归档）。

## 总览

| # | 事项 | 原编号 | 状态 | 时机 |
|---|---|---|---|---|
| 1 | Assembly Definition 拆分 | 4.5 | ⏸ 已排期 | 下轮 |
| 2 | xLua 热更新 MVP（含完整方案附录） | 4.9 | ⏸ 已排期（依赖 4.3 已就绪） | 下轮 |
| 3 | 技能/物品效果数据驱动 + 状态机基类 | 4.10 剩余 | ⏸ 留存 | 随排期 |
| 4 | GameJam 成果按需迁移（Audio 三件套等） | 4.14 剩余 | ⏸ 随需 | 有需求时 |
| 5 | 多场景加载（含 GameJam 方案明细） | 4.11 / 2.4 | ⏸ 缓办 | 有多场景需求时 |
| 6 | 事件总线 | 3.2 | ❌ 维持不采用 | 通道数明显增多时重评估 |
| 7 | **事件引用可视化插件 · 可行性探讨** | 3.3 / 4.8 | 🔍 本次新增探讨（2026-09-13） | 见第 7 节结论 |

---

## 1. Assembly Definition 拆分（原 4.5，下轮）

无 `.asmdef`。建议拆法：Contracts/SO/Gameplay.Core/按功能域/UI/Units/Save/Pathfinding。StatsManager→UI 反向调用已随 4.3 消除，Shop/UIManager 解耦已随 4.2/4.6 完成，asmdef 的主要编译障碍已扫清。注意：拆分后 `Assets/Tests/Editor/` 需要随之建测试 asmdef 并引用被测程序集。

## 2. xLua 热更新接入（原 4.9，下轮；依赖 4.3 已就绪）

无 `Assets/xLua/`、无 LuaManager。完整方案见下方附录。

## 3. 技能/物品效果数据驱动 + 状态机基类（原 4.10 剩余）

| 项 | 现状 |
|---|---|
| 技能/物品效果数据驱动 | ⏸ 未开始（用户决策留存）。`SkillManager.cs` 按 skillName 字符串 switch；`UseItem.cs` 按字段 if 判断。4.3 完成后 StatsManager 转发层 API 已稳定，适合做效果列表驱动 |
| 状态机基类 | ⏸ 未开始（用户决策留存）。`PlayerMovement.cs` switch(PlayerState)；`MovementController.cs:187` 有作者本人的重构 TODO |

## 4. GameJam 成果按需迁移（原 4.14 剩余 + 2.5 调研明细）

**通用直拷**（ObjectPool、TimeManager 已迁，见归档清单）：

| 成果 | GameJam 位置 | 说明 |
|---|---|---|
| CameraShake + ShakeEventSO | `Scripts/Systems/Camera/CameraShake.cs` + `SO/Events/ShakeEventSO.cs` | SO 事件驱动，多段震动叠加、平方衰减、走 `CinemachineBasicMultiChannelPerlin`。My_ARPG 已用 Cinemachine |
| 动画播放抽象四件套 | `Scripts/Systems/Animation/` | `IAnimationPlayer`（含归一化时间点回调）、防低帧率漏触发、State 名==Clip 名约定。对攻击前摇/判定帧有用 |
| ParallaxLayer | `Scripts/Systems/Rendering/ParallaxLayer.cs` | 32 行即插即用视差层 |
| UIFlicker | `Scripts/UI/Widgets/UIFlicker.cs` | TMP 文本闪烁小工具 |

**轻改造**：

| 成果 | 改造点 |
|---|---|
| AudioManager + AudioPlayer + AudioConfigSO（`Scripts/Systems/Audio/`） | **My_ARPG 全项目零音频代码（grep 已证实）**，这是最大的功能缺口。架构：YSingleton 单例 + SO 配置 + AudioPlayer 池化 + BGM 交叉淡入 + 加权随机选段防疲劳。只需把 `SFXType/BGMType` 两个枚举换成 My_ARPG 自己的 |
| HealthHeartUI（`Scripts/UI/HUD/HealthHeartUI.cs`） | IntEventSO 驱动心形血条，需随迁 `IntEventSO` 并适配 PlayerStatsSO（4.3 后数据源已就位） |

**按需/不建议**：
- ArrayPool（My_ARPG 无此场景）；
- VolumeManager（硬耦合 GameJam 的 AgeManager，且 My_ARPG 是内置渲染管线——2026-09-08 已确认，无 URP 包与管线资产）；
- Rewind 接口层（无回溯玩法需求，仅当未来做时缓/幽灵回放时整体搬）；
- SceneArtAssetsTrigger（依赖多场景寻址模式，随第 5 节缓办）;
- PlayerAnimationController（重度耦合年龄玩法，只借鉴 SO 映射模式）。

**框架小件**：`IntEventSO`/`FloatEventSO`（My_ARPG 事件 SO 家族缺这两个基础类型，HealthHeartUI 迁移时必带）、`PersistentSceneRegistry`（多场景缓办时随迁）。

## 5. 多场景加载（原 4.11/2.4，缓办）

GameJam 方案完整但**迁移非纯拷贝**，按已定决策单列缓办：

**GameJam 实现**（`FrameWork/Scripts/Scene/SceneChanger.cs`，284 行）：
- 场景加载以列表为单位：`:23` `List<GameSceneSO> firstSceneToLoad`；事件签名 `SceneLoadEventSO.cs:9` 为 `event Action<List<GameSceneSO>, Vector3, bool>`。
- `:219-240` `LoadScenesRoutine`：按列表顺序逐个 `SceneManager.LoadSceneAsync(scene.sceneName, LoadSceneMode.Additive)` 叠加加载。
- `:189-214` `UnloadCurrentScenes`：从后往前卸载，`PersistentSceneRegistry.IsPersistent` 的常驻场景跳过。
- `PersistentSceneRegistry.cs`（44 行）：静态 HashSet 注册常驻场景名。
- `:271` 全部完成后广播 `sceneLoadedEvent`。

**My_ARPG 适配成本**（缓办原因）：
- `SO/GameSceneSO.cs:7` 是单个 `AssetReference`，My_ARPG 走 Addressables；合并两套需改 GameSceneSO 结构 + SceneChanger 加载/卸载循环 + `SceneLoadEventSO` 签名（现为 `Action<GameSceneSO,Vector3,bool>`），签名变更波及 4 个发布方（Teleport/SaveSystem/RetryManager/ButtonSceneToggler）与 3 个订阅方（SceneChanger/UIManager/DataManager）。
- 待有多场景需求（如主场景+光照场景+UI 场景分离）时再立项。

## 6. 事件总线（原 3.2，维持不采用）

做法：建中心化 `EventBus`，所有通道集中定义。如实评估：解决通道集中定义；不解决订阅关系可见性；成本是 12 条通道全量重接线并放弃 SO 资产 Inspector 工作流。当前规模下收益不覆盖成本。**维持不采用**（待通道数量或跨系统事件明显增多时重新评估）。

> 注：第 7 节的可视化工具恰是"维持 SO 方案"的补丁——SO 工作流的短板是关系不可见，补齐后更无必要上总线。

## 7. 事件引用可视化插件 · 可行性探讨（原 3.3/4.8，2026-09-13）

原 2026-09-08 决策"不做"；本次基于当前代码实测重新评估实现路径。原方案存档：Editor 窗口枚举全部事件 SO 资产 + `GetInvocationList` 展示订阅链 + 发布计数埋点，成本一个 Editor 脚本的量级。

### 7.1 现状实测（2026-09-13，代码为准）

- 事件类 **11 个**，位于 `Assets/Scripts/Gameplay/SO/Events/`，**全部直接继承 ScriptableObject、无公共基类**；每类 1~2 个 field-like `public event Action…` 通道（`ToggleCanvasEventSO` 为 toggle + focus 双通道）。
- 事件资产约 **24 个**：VoidEvents 5、ToggleCanvasEvents 9、InventorySlotsStatsEvents 3、Events 根目录 7。
- 订阅方惯例：`OnEnable += / OnDestroy -=`（或 Start/OnDestroy 生命周期）；发布方调各类的 `RaiseXxx()` 公开方法。
- 推论：失活对象（如菜单场景未激活的 GamePlay UI 根下管理器）不在订阅链上——**快照式查看器查不到它们是语义正确而非缺陷**，解读时须知。

### 7.2 三个实现层次（按性价比排序）

**L1 运行时订阅链查看器（Play 模式，约半天）**

- 原理：`AssetDatabase.FindAssets` 枚举全部事件资产 → 反射读每个 event 的编译器生成后备字段（field-like event 的后备字段与事件同名、private，`GetField(name, NonPublic | Instance)` 可读，反射绕过 event 的访问限制）→ `Delegate.GetInvocationList()` → 展示 `目标类.方法名`；`handler.Target` 是活对象，可 `EditorGUIUtility.PingObject` 一键定位。
- 形态：EditorWindow，左侧资产树/右侧按通道分组列表；`playModeStateChanged` 回调 + 定时刷新。
- 价值：排查"为什么没收到事件"最快的手段。
- 限制：只反映"当前这一刻"的订阅状态；失活订阅者天然缺席（见 7.1 推论）。

**L2 编辑模式接线图（静态，约 1 天，核心价值所在）**

- 原理：接线关系是**序列化数据**——事件资产被哪些场景/prefab/组件的哪个序列化字段引用，扫 .unity/.prefab/.asset 的 GUID 引用即可还原（经 AssetDatabase / `PrefabUtility` API 走，避免裸 YAML 文本解析踩 nested prefab / override）。
- 产出三个视图：
  - 正向：选中事件资产 → 引用它的全部（场景/prefab, GameObject, 组件类型, 字段名）；
  - 反向：选中组件 → 它引用的全部事件资产（`SerializedObject` 遍历序列化属性，实现最简单）；
  - 全局：导出全项目事件关系 mermaid/dot 图（文档与评审用）。
- 已知边界：静态图不区分字段用于**订阅**还是**发布**——本项目惯例清晰（订阅方 `OnEnable +=`、发布方调 `RaiseXxx()`），列出该类型对该字段的代码用法（正则扫 .cs 够用）即可人工速判；Roslyn 精确分析可作后续增强，首版不做。
- 与 Play 状态无关、任何时候可查——这是对"SO + Inspector 工作流关系不可见"短板的直接补丁，也是三档里唯一覆盖编辑期排障的。

**L3 发布埋点（可选，不建议首做）**

- 目的：发布计数、最近发布时间、发布调用栈。
- 阻碍：事件类**无公共基类**——要么引入 `BaseEventSO` 让 11 个类统一继承（碰运行时代码，一次性小改但扩散面广），要么编辑器侧反射向后备字段 `Delegate.Combine` 注入计数 stub（后续用户 `+=` 会保留它，但 `-=` 语义对不上，脆弱）。
- 折中：排查具体事件时在 `RaiseXxx` 里临时 `Debug.LogError` 打调用栈即可（现有排障手段），无需工具化。三档里性价比最低。

### 7.3 技术风险与成本

- 落点 `Assets/Editor/EventGraph/`：Editor 目录的程序集天然不进构建，**零运行时代码改动、零包依赖**（L3 才需碰运行时，且可不做）。
- 多通道类按"类型内全部 event 字段"反射枚举，天然覆盖 `ToggleCanvasEventSO` 这类双通道，无需特判。
- 规模：资产 ~24、引用点几十处，远低于性能敏感区。
- 主要工作量在 L2 的 prefab/场景引用解析细节（override、nested prefab），用 API 而非文本解析可规避大半。

### 7.4 结论

**可行性高**：L1+L2 合计约 1.5 天，纯 Editor 工具，不动运行时代码；L3 不做或痛点出现再做。
与第 6 节的关系：可视化工具补齐 SO 事件方案的可见性短板后，更无必要上事件总线。
立项触发条件建议：下次事件链路排查耗时超过半天，或新增事件通道前需要全局总览时。

---

## 环境备忘（长期有效，排障/自动化前先看）

- 编辑器 Console 的 Warning 开关处于关闭状态——Unity 日志 API 遵循该过滤，MCP read_console 读不到任何 Warning（Debug.Log 正常）。
- 编辑器失焦且 runInBackground 关闭时 Play 循环不走帧（frameCount 停滞，Start/Update 不执行）——自动化验证需 `EditorApplication.Step()` 泵帧，或聚焦编辑器。
- 从 PersistentScene 直接进 Play 是非正常启动路径（正确入口 InitialScene），会放大单例竞态。

---

## 附录：xLua 接入方案（未实施，要点保留）

### 目录规划

| 层 | 位置 | 内容 |
|---|---|---|
| 插件 | `Assets/xLua/` + `Assets/Plugins/` | xLua 源码 + native 插件 |
| 桥接 | `Assets/Scripts/Gameplay/xLua/`（新建） | LuaManager（单例）、LuaBridge/、XLuaGenConfig |
| Lua 源 | `Assets/LuaScripts/`（不进包） | combat/、stats/、inventory/ |
| 下发 | Addressables "LuaScripts" 分组 | 复用已有 Addressables，CDN 下发 .bytes |

初始化入口：`InitialLoad.cs` 的 Awake 里加 `LuaManager.Instance.Init();`

### 下沉顺序（按收益）

1. 战斗/数值/道具公式：`PlayerCombat.DealDamage`、`EnemyCombat.Attack`、`EnemyHealth.ChangeHealth`、`UseItem.ApplyItemEffects`、`StatsManager` 规则 setter。
2. 业务规则：`InventoryManager` 买卖、`DialogManager` 触发条件、`QuestManager` 目标判定。
3. 状态机判定（只下沉判定，执行留 C#）。

### C# 侧骨架

```csharp
public class LuaManager : YSingleton<LuaManager>
{
    private LuaEnv env;

    protected override void OnSingletonInitialized()
    {
        env = new LuaEnv();
        env.AddLoader(CustomLoader);
        env.Global.Set("StatsBridge", StatsBridge.Instance);
        StartCoroutine(LoadEntry());
    }

    // CustomLoader 优先读 persistentDataPath/lua/，回退内置资源
    private IEnumerator LoadEntry()
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>("lua_main");
        yield return handle;
        if (handle.Result != null)
            env.DoString(handle.Result.text, "main", null);
    }
}
```

```csharp
[LuaCallCSharp]
public static List<Type> LuaCallCSharp = new()
{
    typeof(StatsManager), typeof(InventoryManager), typeof(StatsBridge),
};
```

### 热更流水线

```
[开发机] 改 .lua → xLua 菜单 Build → .bytes → Addressables LuaScripts 组标 Remote → Build → 上传 CDN
[客户端] CheckForCatalogUpdates → 下载 .bytes → persistentDataPath/lua/ → CustomLoader 加载 → 新逻辑生效
```

### 风险与对策

| 风险 | 对策 |
|---|---|
| 每帧逻辑下沉 | 第一版只下沉计算/判定，Update/物理留 C# |
| Wrap 代码遗漏 | 每加桥接类型都更新 XLuaGenConfig 并 Generate |
| 热更失败回退 | CustomLoader 本地兜底，异常时用内置 Lua |
| 单例初始化顺序 | LuaManager.Init 放 InitialLoad.Awake 最前，Lua 内部懒加载 |

### MVP 三步

1. 接框架（LuaManager + Addressables Lua 分组）。
2. 改两个战斗样板到 Lua。
3. 验证热更闭环：改 Lua → 重打 Addressables → 客户端拉新字节码 → 数值变化，全程不发版。
