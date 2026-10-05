# SceneFlow 权威 Guide（场景组加载 / 常驻场景 / 加载唯一入口）

文档 ID：`SCENE-FLOW-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
权威范围：场景组加载管线，由场景切换脚本组、启动加载脚本 `InitialLoad` 与场景 SO 契约 `GameSceneSO` 组成，覆盖加载入口、场景组语义、常驻场景注册、存档键来源与构建场景列表依赖。存档读写与存档 schema 归 Data 域；UI 画布管理与事件通道定义归资产域与玩法域；Addressables 包的构建配置不在本域。

- 场景管线代码：`SceneChanger`、`PersistentSceneRegistry`、`Teleport`、`SceneDataForSave`、`CameraPixelSnap`
- 启动加载代码 `InitialLoad`，场景 SO 契约 `GameSceneSO`
- 切场与重试代码：`ButtonSceneToggler`、`RetryButton`
- 重试链上的代码：`PlayerDamageController`、`SaveSystem`、`SaveDataManager`
- 工程构建场景列表、场景文件、资产数据盘点产物

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `SceneChanger` / `RequestSceneLoad` / `SceneToggler` / `ButtonSceneToggler` | §2.1 唯一入口与调用方 |
| `GameSceneSO` / `sceneName` / `sceneType` / `SaveKey` | §2.2 场景 SO 契约 |
| `场景组` / `Additive` / `firstSceneToLoad` / `sceneToLoad` | §2.3 组语义与加载顺序 |
| `常驻场景` / `PersistentSceneRegistry` / `IsPersistent` | §2.4 常驻注册表 |
| 初始场景 / `InitialLoad` / `persistentScenes` | §2.5 启动声明 |
| `存档键` / `sceneIDAndPlayerPos` / `SceneDataForSave` | §2.7 存档键来源 |
| `Teleport` / `传送` / `重试` / `RetryButton` | §2.8 切场触发点与重试 |
| 构建场景列表 / `EditorBuildSettings` / `CanStreamedLevelBeLoaded` | §2.9 构建场景列表 |
| `Addressables` / `AssetReference` | §2.9 Addressables 现状 |
| `isLoading` / 加载窗口 / 并发请求 | §2.1 加载窗口守卫 |

## 2. 当前实现

### 2.1 唯一入口：先广播后执行

`SceneChanger.RequestSceneLoad(List<GameSceneSO>, Vector3, bool)` 是场景切换的唯一对外入口，方法体只有两条语句：先 `loadEventSO.RaiseLoadRequestEvent(...)` 同步调用所有订阅方并等其返回，再 `OnLoadRequestEvent(...)` 走本类的切换流程。类注释与 `LoadScenesRoutine` 的注释都把这条顺序写成硬约定。调用方共 4 处，全部走该入口：传送脚本 `Teleport` 内的 `SceneToggler` 传送切场、`ButtonSceneToggler` 的按钮切场、`SaveSystem` 的读档、`PlayerDamageController` 的整组重载；`SceneChanger` 在启动阶段还自调用一次。工程内 `RaiseLoadRequestEvent` 只有 `SceneChanger` 一处调用，`SceneManager.LoadSceneAsync` 只出现在 `SceneChanger` 与启动期的 `InitialLoad` 常驻加载里，没有绕过入口的调用点。

参数语义与执行段守卫：`scenes` 是完整的目标场景组，空组只打 Warning 后返回；`position` 传 `Vector3.zero` 时改用组内首个有效场景 SO 的 `initialPosition` 作出生点；`isToFade` 控制过渡动画，启动那一次传 `false`。执行段的第一道守卫是加载窗口标记 `isLoading`，窗口内的新请求只打 Warning 并被忽略，标记到 `OnLoadCompleted` 复位。广播先于标记置位，因此该窗口拦得住并发的整组请求，广播段自身的同步重入会穿过。`GetCurrentScenes()` 返回 `currentScenes` 的副本，整组重载的调用方拿到的引用不受本类 `Clear()` 影响。

### 2.2 GameSceneSO 语义

字段语义：`ID` 标识资产本身，不落盘、跨会话会变，注释明示它不能充当存档键；`sceneAsset` 只在编辑器下存在；`sceneName` 是运行时加载名；`sceneType` 取 `MyEnums.SceneType`；`initialPosition` 是出生点。`OnValidate` 在编辑器下用 `sceneAsset.name` 覆盖 `sceneName`。

当前工程的 GameSceneSO 资产共 5 个：MenuScene、Scene1、Scene2、TestScene 与 RetrySceneSO，`sceneName` 分别为 StartingMenu、Scene1、Scene2、TestScene 与空。其中没有常驻场景类型的资产。

### 2.3 组语义与加载顺序

- 卸载段先等 `fadeDuration`，再按倒序遍历 `currentScenes` 逐一 `UnloadSceneAsync`，跳过常驻场景，随后 `currentScenes.Clear()`。
- 加载段按列表正序逐个用 `Application.CanStreamedLevelBeLoaded` 校验，失败只 `LogError` 并 `continue`，整组不中断；`sceneType == Menu` 时调 `SetObjects(false)`，`Location` 时调 `SetObjects(true)`。
- `firstLoaded` 取组内第一个成功加载的场景，作为 `loadedScene` 与 `currentScene`；`currentScenes[0]` 是 `GetCurrentGameScene()` 的返回值。
- 完成后 `sceneLoadedEvent` 广播前必须先赋 `currentScene`，这是源码里写明的不变量。

### 2.4 常驻场景注册表

`PersistentSceneRegistry` 是纯静态类，内部用 `static readonly HashSet<string> SceneNames` 按 `sceneName` 记录，对外提供 `Register(IEnumerable<GameSceneSO>)`、`Register(GameSceneSO)`、`IsPersistent(GameSceneSO)` 与 `IsPersistent(string)`。`SceneChanger` 在卸载循环里查询它，命中时常驻场景打 Warning 并跳过。

会话与局间复位由 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] Reset()` 清空 `SceneNames`，它在每次进入播放模式时被调用，关闭 Domain Reload 时同样触发。`Register` 只 Add 不 Clear，同一会话内重复注册是幂等写入。

### 2.5 常驻场景在初始场景中的声明

初始场景的唯一根对象是 `InitialLoad`，其 `persistentScenes` 只登记一条：菜单场景资产 MenuScene。`InitialLoad.Awake` 先注册常驻场景再起协程，加载时同样用 `Application.CanStreamedLevelBeLoaded` 校验，并跳过已经加载的场景。

### 2.6 加载完成后的启动内容

`SceneChanger.Start()` 自调用 `RequestSceneLoad(firstSceneToLoad, Vector3.zero, false)`，调用点放在 Start 是为了让同批 `Awake` 与 `OnEnable` 的订阅方就绪。常驻场景里的 `SceneChanger` 配置 `firstSceneToLoad = [MenuScene]`，它的 `player` 与 `objectsToUnableWhileMenuOrReset` 都指向玩家根节点。

### 2.7 存档键来源

存档键取 `GameSceneSO.SaveKey`：编辑器下用 `AssetDatabase.FindAssets("{sceneName} t:SceneAsset")` 取场景文件 GUID，编辑器之外退化为资产名。`SaveKey` 不落盘，编辑器之外的分支是打包后的实际取值。

写入侧：`SaveDataManager.OnAutoSave` 取 `scenesToLoadSO[0]` 作为首个内容场景，并用 `SaveKey` 组 `sceneIDAndPlayerPos`；`GetCurrentGameScene()` 也被 `PrepareManualSaveData` 与读档面板 `SaveLoadCanvasManager` 使用。

读回侧：`SceneDataForSave.gameScenes` 是 `SaveKey → GameSceneSO` 的反查表，在常驻场景中配置为 `[MenuScene, Scene1, Scene2]`，由 `SaveSystem.GetScene` 线性比对 `scene.SaveKey`。

### 2.8 切场触发点与重试

- 进游戏：开始按钮预制体的 `sceneToLoad = [Scene1]`，该预制体只在 StartingMenu 场景中实例化。
- 回菜单：标题按钮预制体的 `sceneToLoad = [MenuScene]`，该预制体只在常驻场景中实例化。
- 关卡互传：传送预制体内嵌 `SceneToggler`，预制体自身的 `sceneToLoad` 为空数组，关卡场景实例把它覆盖成 Scene1 到 Scene2 与 Scene2 到 Scene1。
- 重试：`RetryButton.HandleRetry` 做三件事，隐藏按钮、调 `ForceResumeGame`、调 `retryEventSO.OnEventRaised()`；真正的整组重载在 `PlayerDamageController.OnRetryRequest`，先 `StatsService.Instance.Respawn()` 回血，再 `RequestSceneLoad(GetCurrentScenes(), Vector3.zero, true)`。回血早于广播段，`PlayerDamageController` 的注释写明原因：广播段里的 `SaveDataManager.OnAutoSave` 同步把运行态写进存档快照；回灌侧由 `SaveDataManager.OnAutoLoad` 的 `GetDataID() == null` 守卫跳过固定槽位服务，复活血量保留在运行时。

### 2.9 构建场景列表与 Addressables 现状

构建场景列表共 6 条且全部启用，顺序为 InitialScene、PersistentScene、StartingMenu、Scene1、Scene2、TestScene；配置对象列表里挂着 Addressables 的配置对象。

Addressables 在代码层零引用：对全部产品脚本检索 `Addressables`、`AssetReference` 与 `AsyncOperationHandle`，只命中 2 处注释，没有 `using UnityEngine.AddressableAssets`，也没有任何 API 调用。包在包清单中保持安装，版本 1.22.3。数据层的 Addressables 配置为：`m_Enabled` 与 `m_BuildAddressablesWithPlayerBuild` 均为 1，Default Group 为 Scenes，Scenes 组含 5 条场景条目，其中包含常驻场景。

## 3. 约定与硬边界

1. **切场一律经 `RequestSceneLoad`**：直接调 `RaiseLoadRequestEvent` 只通知订阅方，不切场景；新增的切场触发点也必须经该入口，否则 `UIManager` 的画布重置与 `SaveDataManager` 的自动存档等同步收尾全部缺失。
2. **场景必须进构建场景列表**：`Application.CanStreamedLevelBeLoaded` 为假时只报错并跳过该场景，整组其余场景照常加载。
3. **常驻场景不进任何 `sceneToLoad` 或 `firstSceneToLoad` 组**：`Teleport` 与 `ButtonSceneToggler` 的 Tooltip 明写这条。组内第一个元素决定 `currentScene` 与存档键。
4. **`sceneName` 为空即静默跳过**：卸载段与加载段都用 `string.IsNullOrEmpty(scene.sceneName)` 过滤，这类 GameSceneSO 整条被忽略。
5. **存档键取 `SaveKey`**：`ID` 由 `OnValidate` 生成且不落盘；`SaveKey` 的取值与 `AssetReference.AssetGUID` 对齐。
6. **`SceneDataForSave.gameScenes` 必须覆盖所有可能写进场景键的 GameSceneSO**：反查靠线性遍历 `SaveKey`，漏项使读档时 `SaveSystem.GetScene` 返回 null。
7. **重试链先回血再请求**：回血晚于广播段会把死亡态血量写进该广播段抓取的存档快照。
