# SceneFlow 权威 Guide（场景组加载 / 常驻场景 / 加载唯一入口）

文档 ID：`SCENE-FLOW-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：负责「Assets/Scripts/Pipeline/Scene/**、Assets/Scripts/Pipeline/InitialLoad.cs、Assets/Scripts/Pipeline/SO/GameSceneSO.cs 组成的场景组加载管线」——即加载入口、场景组语义、常驻场景注册、存档键来源、Build Settings 依赖。不负责存档读写与 JSON schema（见 Data 域）、不负责 UI 画布管理与事件通道定义（见 Assets/Gameplay 域）、不负责 Addressables 包的构建配置本身。
上游来源：`Assets/Scripts/Pipeline/Scene/SceneChanger.cs`、`Assets/Scripts/Pipeline/Scene/PersistentSceneRegistry.cs`、`Assets/Scripts/Pipeline/Scene/Teleport.cs`、`Assets/Scripts/Pipeline/Scene/SceneDataForSave.cs`、`Assets/Scripts/Pipeline/Scene/CameraPixelSnap.cs`、`Assets/Scripts/Pipeline/InitialLoad.cs`、`Assets/Scripts/Pipeline/SO/GameSceneSO.cs`、`Assets/Scripts/Pipeline/UI/Buttons/ButtonSceneToggler.cs`、`Assets/Scripts/Pipeline/UI/Buttons/RetryButton.cs`、`Assets/Scripts/Gameplay/Player/Controllers/PlayerDamageController.cs`、`Assets/Scripts/Gameplay/Save/SaveSystem.cs`、`Assets/Scripts/Gameplay/Save/SaveDataManager.cs`、`ProjectSettings/EditorBuildSettings.asset`、`Assets/Scenes/**`、`Temp/doc-discovery/asset-data-pipeline.json`、旧文档 `Docs/My_ARPG_MVCS项目现状.md`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `SceneChanger` / `RequestSceneLoad` / `SceneToggler` / `ButtonSceneToggler` | §2.1 唯一入口与调用方 |
| `GameSceneSO` / `sceneName` / `sceneType` / `SaveKey` | §2.2 场景 SO 契约 |
| `场景组` / `Additive` / `firstSceneToLoad` / `sceneToLoad` | §2.3 组语义与加载顺序 |
| `常驻场景` / `PersistentSceneRegistry` / `IsPersistent` | §2.4 常驻注册表 |
| `InitialScene` / `InitialLoad` / `persistentScenes` | §2.5 启动声明 |
| `存档键` / `sceneIDAndPlayerPos` / `SceneDataForSave` | §2.7 存档键来源 |
| `Teleport` / `传送` / `重试` / `RetryButton` | §2.8 切场触发点与重试 |
| `Build Settings` / `EditorBuildSettings` / `CanStreamedLevelBeLoaded` | §2.9 构建场景列表 |
| `Addressables` / `AssetReference` | §2.9 已弃用路径与残留 |
| `isLoading` / 加载窗口 / 并发请求 | §2.1 加载窗口守卫 |

## 2. 当前实现

### 2.1 唯一入口：先广播后执行

`SceneChanger.RequestSceneLoad(List<GameSceneSO>, Vector3, bool)` 是场景切换的唯一对外入口，方法体只有两条语句（`SceneChanger.cs:86-90`）：

1. `loadEventSO.RaiseLoadRequestEvent(...)` —— 同步调用所有订阅方并等其返回（`SceneLoadEventSO.cs:16-19`）；
2. `OnLoadRequestEvent(...)` —— 本类的切换流程。

类注释与 `LoadScenesRoutine` 注释都把它写成硬约定（`SceneChanger.cs:8-9`、`SceneChanger.cs:212-213`）。现有调用方共 4 处，全部走该入口：`ButtonSceneToggler.cs:21`、`Teleport.cs:22`（类名 `SceneToggler`）、`SaveSystem.cs:136`（读档）、`PlayerDamageController.cs:57`（重试整组重载）；另有 `SceneChanger.cs:98` 的启动自调用。核验结论：`RaiseLoadRequestEvent` 在 `Assets/Scripts` 内只有 `SceneChanger.cs:88` 一处调用，`SceneManager.LoadSceneAsync` 只有本类 `:239` 与启动期常驻加载 `InitialLoad.cs:41`，**没有绕过入口的反例**。

参数语义与执行段守卫：`scenes` 为完整目标场景组（空组只打 Warning 返回，`:135-139`）；`position` 传 `Vector3.zero` 表示改用组内首个有效场景 SO 的 `initialPosition` 作出生点（`:153`、`GetInitialPosition` `:164-172`）；`isToFade` 控制过渡动画（启动那次传 `false`，`:155`、`:278`）。执行段第一道守卫是加载窗口标记 `isLoading`：窗口内新请求只打 Warning 并被忽略（`:142-148`），标记到 `OnLoadCompleted` 的 `:287` 才复位；因为广播（`:88`）先于标记置位，该窗口拦得住并发的整组请求，拦不住广播段自身的同步重入。`GetCurrentScenes()` 返回 `currentScenes` 的**副本**（`:67-70`），整组重载的调用方不会拿到会被本类 `Clear()` 清空的引用。

### 2.2 GameSceneSO 语义

字段：`ID`（明示「不是存档键」，不落盘、跨会话会变，`GameSceneSO.cs:12-13`）、`sceneAsset`（仅 `#if UNITY_EDITOR`，`GameSceneSO.cs:15-18`）、`sceneName`（运行时加载名，`GameSceneSO.cs:21`）、`sceneType`（`MyEnums.SceneType`）、`initialPosition`（`GameSceneSO.cs:23-24`）。`OnValidate` 在编辑器下用 `sceneAsset.name` 覆盖 `sceneName`（`GameSceneSO.cs:57-64`）。

**当前工程无「PersistentScene」的 GameSceneSO 资产**：5 个资产为 MenuScene/Scene1/Scene2/TestScene/RetrySceneSO，`sceneName` 分别为 StartingMenu / Scene1 / Scene2 / TestScene / 空。

### 2.3 组语义与加载顺序

- 卸载段先等 `fadeDuration`（`SceneChanger.cs:186`），再**倒序**遍历 `currentScenes` 逐一 `UnloadSceneAsync`，跳过常驻场景（`SceneChanger.cs:188-204`），随后 `currentScenes.Clear()`（`:206`）。
- 加载段按**列表正序**逐个 `Application.CanStreamedLevelBeLoaded` 校验（`:226-232`），失败只 `LogError` 并 `continue`（不中断整组）；`sceneType == Menu` 时 `SetObjects(false)`，`Location` 时 `SetObjects(true)`（`:234-237`）。
- `firstLoaded` 取组内第一个成功加载的场景，作为 `loadedScene`（`:243-251`）与 `currentScene`（`:273-277`）；`currentScenes[0]` 是 `GetCurrentGameScene()` 的返回值（`:58-61`）。
- 完成后 `sceneLoadedEvent` 广播前必须先赋 `currentScene`，这是源码里写明的不变量（`:275-277`）。

### 2.4 常驻场景注册表

`PersistentSceneRegistry` 是**纯静态类**（`PersistentSceneRegistry.cs:8`），内部 `static readonly HashSet<string> SceneNames`（`:10`）按 `sceneName` 记录；`Register(IEnumerable<GameSceneSO>)` / `Register(GameSceneSO)` / `IsPersistent(GameSceneSO)` / `IsPersistent(string)`（`:12-37`）。SceneChanger 在卸载循环里对它查询并打 Warning 跳过（`SceneChanger.cs:194-199`）。

它能跨场景存活的原因：数据放在静态字段上，不挂在任何 MonoBehaviour 或场景对象上，场景卸载不回收它（Unity 静态生命周期语义，未运行验证）。会话/局间复位由 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] Reset()` 清空 `SceneNames`（`PersistentSceneRegistry.cs:39-43`）——它在每次进入播放模式（含关闭 Domain Reload 时）被调用。注意 `Register` 只 Add 不 Clear（`:26`），同一会话内重复注册只是幂等写入。

### 2.5 常驻场景在 InitialScene 的声明

`InitialScene.unity` 唯一根对象是 `InitialLoad`（`InitialScene.unity:125-141`，脚本 GUID 在 `:151`，值 `6a81adb439162844db041ee45d2f280d` = `InitialLoad.cs.meta`），其 `persistentScenes` 只有一条：GUID `7c55136055a6e324ba090da1c0385003` = **MenuScene.asset**（`InitialScene.unity:154-155`；`Assets/GameSO/GameSceneSO/MenuScene.asset.meta:2`）。`InitialLoad.Awake` 先注册再起协程（`InitialLoad.cs:11-15`），加载时同样校验 `CanStreamedLevelBeLoaded` 并跳过已加载场景（`InitialLoad.cs:31-39`）。

### 2.6 加载完成后的启动内容

`SceneChanger.Start()` 自调用 `RequestSceneLoad(firstSceneToLoad, Vector3.zero, false)`（`SceneChanger.cs:96-99`），注释说明放在 Start 是为了让同批 Awake/OnEnable 的订阅方就绪。`PersistentScene.unity` 中 `firstSceneToLoad = [MenuScene]`（`PersistentScene.unity:30992-30993`，脚本 GUID `55f1a515b64ebe640868999928218c10`），`player` 与 `objectsToUnableWhileMenuOrReset` 都指向玩家根节点（`:30994`、`:31001-31002`）。

### 2.7 存档键来源

存档键 = `GameSceneSO.SaveKey`：编辑器下用 `AssetDatabase.FindAssets("{sceneName} t:SceneAsset")` 取场景文件 GUID，非编辑器下退化为资产名（`GameSceneSO.cs:31-50`；`SaveKey` 不落盘，非编辑器分支是打包后的实际取值）。

写入侧：`SaveDataManager.OnAutoSave` 取 `scenesToLoadSO[0]` 作为「首个内容场景」（`SaveDataManager.cs:75`），并在 `:96`、`:104-113` 用 `SaveKey` 组 `sceneIDAndPlayerPos`；`GetCurrentGameScene()` 也被 `PrepareManualSaveData`（`SaveDataManager.cs:50`）和读档面板（`SaveLoadCanvasManager.cs:186`）使用。

读回侧：`SceneDataForSave.gameScenes` 是 `SaveKey → GameSceneSO` 的反查表（`SceneDataForSave.cs:4-7`），在 PersistentScene 中配置为 `[MenuScene, Scene1, Scene2]`（`PersistentScene.unity:7053-7056`），由 `SaveSystem.GetScene` 线性比对 `scene.SaveKey`（`SaveSystem.cs:199-212`）。

### 2.8 切场触发点与重试

- 进游戏：`Start.prefab`（GUID `6b87f82fd68ab0f40b9f99515133ac6d`，仅在 `StartingMenu.unity` 实例化）的 `sceneToLoad = [Scene1]`（`Assets/Prefabs/UI/Buttons/Start.prefab:146-151`）。
- 回菜单：`TitleButton.prefab`（GUID `d1e11eb959f03ff49a24a7affc115f92`，仅在 `PersistentScene.unity` 实例化）的 `sceneToLoad = [MenuScene]`（`Assets/Prefabs/UI/Buttons/TitleButton.prefab:148-151`）。
- 关卡互传：`Teleport.prefab` 内嵌 `SceneToggler`（`Assets/Prefabs/Grid/Teleport.prefab:103`），预制体自身 `sceneToLoad: []`，由场景实例覆盖为 Scene1→Scene2、Scene2→Scene1（`Scene1.unity:22642`、`Scene2.unity:4143` 指向对应 GameSceneSO 资产 GUID）。
- 重试：`RetryButton.HandleRetry` 只做隐藏按钮 + `ForceResumeGame` + `retryEventSO.OnEventRaised()`（`RetryButton.cs:8-16`）；真正的重载在 `PlayerDamageController.OnRetryRequest`：先 `StatsService.Instance.Respawn()` 回血，再 `RequestSceneLoad(GetCurrentScenes(), Vector3.zero, true)`（`PlayerDamageController.cs:54-58`）。回血必须早于广播段，原因写在 `PlayerDamageController.cs:48-52`：广播段（`SaveDataManager.OnAutoSave`）同步把运行态写进存档快照，晚于快照就会把死亡态血量落盘；至于回灌侧，`OnAutoLoad` 现有 `GetDataID() == null` 守卫会跳过固定槽位服务（`SaveDataManager.cs:129-130`），复活血量不再被快照覆盖回运行时。

### 2.9 Build Settings 与 Addressables 去依赖

构建场景列表共 6 条且全部 `enabled: 1`，顺序为 InitialScene → PersistentScene → StartingMenu → Scene1 → Scene2 → TestScene（`ProjectSettings/EditorBuildSettings.asset:7-25`）；`m_configObjects` 仍挂着 `com.unity.addressableassets` 配置对象（`:27`）。

Addressables 在代码层已零引用：对 `Assets/**/*.cs` 全量检索 `Addressables|AssetReference|AsyncOperationHandle` 只命中 2 处**注释**（`Assets/Scripts/Pipeline/SO/GameSceneSO.cs:28`、`Assets/Scripts/Gameplay/Save/SaveData.cs:55`），无任何 `using UnityEngine.AddressableAssets` 或 API 调用。包仍安装（`Packages/manifest.json:4` `com.unity.addressables: 1.22.3`），数据层残留仍在：`m_Enabled: 1`（`Assets/AddressableAssetsData/AddressableAssetSettings.asset:10`）、`m_BuildAddressablesWithPlayerBuild: 1`（`:44`）、Default Group = Scenes（`:15`），Scenes 组含 5 条场景条目含 PersistentScene（`Assets/AddressableAssetsData/AssetGroups/Scenes.asset:20-44`，其中 `:35` 是 PersistentScene.unity 的场景 GUID 行、`:36` 是对应的路径行）。

## 3. 约定与硬边界

1. **不得绕过 `RequestSceneLoad`**：直接 `RaiseLoadRequestEvent` 只通知订阅方、不切场景（`SceneChanger.cs:84`、`SceneChanger.cs:212-213`）；反过来，任何新增的切场触发点也必须经该入口，否则 UIManager 画布重置、SaveDataManager 自动存档等同步收尾全部缺失（`UIManager.cs:71-77`、`SaveDataManager.cs:30`）。
2. **场景必须进 Build Settings**：`CanStreamedLevelBeLoaded` 为假时只报错跳过（`SceneChanger.cs:226-232`、`InitialLoad.cs:35-39`），整组其余场景照常加载 ⇒ 缺条目表现为「什么都没发生 + 一条 Error」，不会抛异常。
3. **常驻场景不得出现在任何 `sceneToLoad` / `firstSceneToLoad` 组内**：Tooltip 明写这条（`Teleport.cs:12`、`ButtonSceneToggler.cs:6`）。组内第一个元素决定 `currentScene` 与存档键（`SceneChanger.cs:58-61`、`SaveDataManager.cs:75`），把常驻场景放首位会让存档键指向错误场景。
4. **`sceneName` 为空即静默跳过**：卸载段与加载段都用 `string.IsNullOrEmpty(scene.sceneName)` 过滤（`SceneChanger.cs:191`、`:223`），旧 schema 的 GameSceneSO 会整条被忽略。
5. **存档键不可换成 `ID`**：`ID` 由 `OnValidate` 生成且不落盘（`GameSceneSO.cs:12-13`、`:52-55`），改用它即坏旧档；`SaveKey` 的取值刻意与旧 Addressables `AssetReference.AssetGUID` 对齐（`GameSceneSO.cs:26-30`）。
6. **`SceneDataForSave.gameScenes` 必须覆盖所有可能被存进场景键的 GameSceneSO**：反查靠线性遍历 `SaveKey`（`SaveSystem.cs:207-210`），漏项会让读档 `GetScene` 返回 null、该档被判为不可加载。
7. **重试链要先回血再请求**：顺序反了会把死亡态血量写进广播段抓的存档快照（`PlayerDamageController.cs:48-52`）；回灌侧已由 `GetDataID() == null` 守卫跳过固定槽位服务（`SaveDataManager.cs:129-130`），不再是覆盖来源。

## 4. 已知缺陷与风险

1. **【高】InitialScene 的常驻场景被迁移误指为 MenuScene（未运行 Unity 验证）**：`e7c9ef8` 把旧的 `persistentScene.m_AssetGUID: 823843722642c914dba2fb270e6b288d`（= PersistentScene.unity）替换成 `persistentScenes: [7c55136055a6e324ba090da1c0385003]`（= MenuScene.asset），见 `git show e7c9ef8 -- Assets/Scenes/InitialScene.unity` 与 `InitialScene.unity:154-155`。后果链（静态推断）：PersistentScene 不在任何 GameSceneSO 组内 ⇒ 从 InitialScene 启动时它不会被加载 ⇒ `SceneChanger.Start()` 不执行、`firstSceneToLoad` 不触发，且 `PersistentSceneRegistry` 注册的实际是 `StartingMenu`（`InitialLoad.cs:13`、`PersistentSceneRegistry.cs:26`）；StartingMenu 内的 `Start.prefab` 调 `SceneChanger.Instance.RequestSceneLoad` 会因 `Instance == null` 抛 NullReferenceException，而正常路径（PersistentScene 启动）本应有效。`git log e7c9ef8..HEAD -- Assets/Scenes/InitialScene.unity` 为空，问题未被后续提交修复。
2. **【低】RetrySceneSO 仍是旧 schema，且已不再被引用（未运行 Unity 验证）**：资产无 `sceneName`/`sceneAsset`，只留旧 `sceneReference.m_AssetGUID`（`:16-20`，= Scene1.unity GUID）、`ID`（`:15`）与 `sceneType: 2`（Retry）（`Assets/GameSO/GameSceneSO/OtherScenes/RetrySceneSO.asset` 全文）。**它已不再被任何代码、场景或预制体引用**（`SceneChanger` 侧的重试事件订阅已删除，全工程对 `Assets/**` 的 GUID/名字检索零命中）；若将来又被接进某组的 `sceneToLoad`，才会命中 `SceneChanger.cs:223` 的空 `sceneName` 分支被**静默跳过**（连 Error 都不会打，因为过滤发生在 `CanStreamedLevelBeLoaded` 校验之前）。Retry 语义目前由 `RetryButton → RetryRequestEvent → PlayerDamageController.OnRetryRequest` 的整组重载实现，不需要场景 SO。
3. **【中】预制体残留已删字段**：`Start.prefab` / `TitleButton.prefab` 的 MonoBehaviour 段仍保留 `loadEventSO` 与 `retryEventSO` 两个已不在 `ButtonSceneToggler.cs`（仅 `sceneToLoad` / `ButtonCanvas` / `newPosition` / `isToFade`）中的序列化键（`Start.prefab:149,155`、`TitleButton.prefab:149,155`）；`Teleport.prefab:106` 的 `SceneToggler` 段同样残留 `loadEventSO`，`PersistentScene.unity:16277` 还有一条场景侧 override。Unity 反序列化会忽略它们，功能不受影响，但下次在 Inspector 保存这些预制体会静默清理。
4. **【中】Addressables 残留会在 Player 构建时继续产出 bundle**：`m_Enabled: 1` + `m_BuildAddressablesWithPlayerBuild: 1`（`AddressableAssetSettings.asset:10,44`），而代码路径已完全脱离。不影响场景加载正确性，只增加构建产物与耗时。
5. **【低】TestScene 是构建内的孤儿**：`Assets/Scenes/TestScene.unity` 在 Build Settings（`EditorBuildSettings.asset:24-25`）且其 GameSceneSO 存在，但无任何场景/预制体引用该 SO（对全部 `Assets/**` 的 GUID 检索只命中 `.meta` 与资产自身）。
6. **【低】旧文档与代码表述冲突（已按代码修正）**：`Docs/My_ARPG_重构优化清单_已解决.md` 已由用户在 2026-10-05 删除，内容见 `git HEAD:Docs/My_ARPG_重构优化清单_已解决.md`（其 `:78` 记「`GameSceneSO` 改用 `sceneName`」，与代码一致）；仍存在的 `Docs/My_ARPG_MVCS项目现状.md:199` 接线检查项（「事件 SO、`ShopSlot.shopRef`、掉落物 ID、`GameSceneSO.sceneName`」）未覆盖 `InitialScene.persistentScenes` 这一迁移新增字段，正是第 1 条缺陷能漏过验证清单的原因。
7. **【低】`SceneChanger.Start()` 与 `InitialLoad` 的相对顺序未受显式控制**：两者都在 Awake/Start 阶段发起 Additive 加载，源码只用注释声明「放在 Start 让订阅方就绪」（`SceneChanger.cs:92-95`），没有 `DefaultExecutionOrder` 保护（全工程唯一执行序属性在 `CameraPixelSnap.cs:9`，语义无关）。当前两场景互不共存，暂无实际冲突。

## 5. 未核验事项

1. 假设：静态 `HashSet` 在场景卸载/加载循环中不会被清空，因此 `PersistentSceneRegistry` 的注册结果真正「跨场景存活」；`SubsystemRegistration` 只在进入播放模式时复位一次（未运行 Unity 验证）。
2. 假设：从 InitialScene 启动的实际表现就是第 4 节第 1 条描述的失败链（PersistentScene 不加载 → `SceneChanger.Instance == null`）——这是基于场景资产引用与脚本 GUID 的静态推断，未运行 Unity 验证。
3. 假设：`GameSceneSO.SaveKey` 的编辑器分支对同名的多个 `t:SceneAsset` 会命中正确的那个（`GameSceneSO.cs:38-45` 遍历时只比对文件名，无路径去重）；当前工程场景名唯一，因此未暴露（未运行 Unity 验证）。
4. 假设：打包后 `SaveKey` 走 `return name`（资产名）分支，与编辑器下返回的场景文件 GUID 不同；二者是否对旧档等价未验证（未运行 Unity 验证）。
5. 假设：`TitleButton` 从关卡内回菜单时会走 `SetObjects(false)` 隐藏玩家根节点、且 `StartingMenu` 因常驻注册而不被 SceneChanger 卸载（未运行 Unity 验证）。
6. 假设：`Application.CanStreamedLevelBeLoaded` 在编辑器播放模式下对 Build Settings 内 `enabled: 1` 的场景返回 true（未运行 Unity 验证）。
7. 假设：`Teleport.prefab` 的 `sceneToLoad: []` 空数组不会被任何运行路径使用（当前两处场景实例都覆盖为 1 条），因此不会触发 `OnLoadRequestEvent` 的空组告警（未运行 Unity 验证）。
8. 假设：`CameraPixelSnap` 的 `OnPreCull/OnPostRender` 在 URP 下不被调用（工程 `Assets/Settings` 无 URP/HDRP 资产，走 Built-in）；该脚本属本目录但不属场景加载语义，未纳入上文分析（未运行 Unity 验证）。
