# SceneFlow Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 SceneFlow 权威文档

**写入的文件**

- `SceneFlow_Guide.md`，ID `SCENE-FLOW-GUIDE`，状态 Active
- `Router.md`，ID `BUS-SCENEFLOW`，维护计数 `0/5`
- 本文件

**依据的证据**

以下证据全部实际读过。

- 代码：`SceneChanger`、`PersistentSceneRegistry`、`Teleport`、`SceneDataForSave`、`CameraPixelSnap`、`InitialLoad`、`GameSceneSO`、`SceneLoadEventSO`、`ButtonSceneToggler`、`RetryButton`、`PlayerDamageController`、`SaveSystem`、`SaveDataManager`、`YSingleton`、`UIManager`
- 资产：5 个 GameSceneSO 资产与各自 meta、InitialScene、PersistentScene、Start 预制体、TitleButton 预制体、Teleport 预制体、AddressableAssetSettings、Scenes 组、`manifest.json`、EditorBuildSettings、ProjectSettings
- 全量检索：对全部产品脚本检索 `Addressables`、`AssetReference` 与 `AsyncOperationHandle`，命中 2 处注释、0 处调用；脚本 GUID 与场景、预制体引用逐条比对
- 历史：`git show e7c9ef8` 与 `git log e7c9ef8..HEAD` 查 InitialScene 的改动

**已核验的结论**

- `RequestSceneLoad` 是唯一入口，先广播后执行；现有 4 个调用方与 1 处启动自调用全部经该入口。
- `isLoading` 覆盖广播后到 `OnLoadCompleted` 的整窗，窗口内请求被 Warning 拒绝。
- 加载顺序、`CanStreamedLevelBeLoaded` 前置校验只报错不中断、`firstLoaded`、`currentScene`、`currentScenes[0]` 的取值规则。
- `PersistentSceneRegistry` 为纯静态 HashSet，按 `sceneName` 注册，在 `SubsystemRegistration` 复位。
- 存档键取 `GameSceneSO.SaveKey`，写入侧在 `SaveDataManager`，读回侧在 `SaveSystem` 与 `SceneDataForSave.gameScenes`。
- 构建场景列表 6 条及顺序；Addressables 在代码层零引用，数据层配置保持启用。
- 常驻场景列表当前指向 MenuScene。

**结论**

SceneFlow 域权威文档与 Router 已建立，维护计数保持 `0/5`。
