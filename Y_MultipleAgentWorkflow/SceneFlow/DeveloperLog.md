# SceneFlow Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 SceneFlow 权威文档

**写入的文件**

- `Y_MultipleAgentWorkflow\SceneFlow\SceneFlow_Guide.md`（新建，113 行，ID `SCENE-FLOW-GUIDE`，状态 Active）
- `Y_MultipleAgentWorkflow\SceneFlow\Router.md`（重写为项目中文模板，ID `BUS-SCENEFLOW`，维护计数 `0/5`）
- 本文件（追加，未删除任何既有条目）

**依据的证据路径（全部实际读过，未依赖提示或旧文档）**

- 代码：`Assets/Scripts/Pipeline/Scene/SceneChanger.cs`、`PersistentSceneRegistry.cs`、`Teleport.cs`（类名 `SceneToggler`）、`SceneDataForSave.cs`、`CameraPixelSnap.cs`、`Assets/Scripts/Pipeline/InitialLoad.cs`、`Assets/Scripts/Pipeline/SO/GameSceneSO.cs`、`Assets/Scripts/Pipeline/SO/Events/SceneLoadEventSO.cs`、`Assets/Scripts/Pipeline/UI/Buttons/ButtonSceneToggler.cs`、`RetryButton.cs`、`Assets/Scripts/Gameplay/Player/Controllers/PlayerDamageController.cs`、`Assets/Scripts/Gameplay/Save/SaveSystem.cs`、`SaveDataManager.cs`、`Assets/Scripts/Contracts/YSingleton.cs`、`Assets/Scripts/Pipeline/UI/SystemCanvasManagers/UIManager.cs`
- 资产：5 个 `Assets/GameSO/GameSceneSO/**.asset` 全文 + `.meta`、`Assets/Scenes/InitialScene.unity:125-175`、`Assets/Scenes/GameScene/PersistentScene.unity:7045-7064, 30978-31002`、`Assets/Prefabs/UI/Buttons/Start.prefab:137-155`、`TitleButton.prefab:144-157`、`Assets/Prefabs/Grid/Teleport.prefab:96-115`、`Assets/AddressableAssetsData/AddressableAssetSettings.asset`、`AssetGroups/Scenes.asset:21-41`、`Packages/manifest.json:4`、`ProjectSettings/EditorBuildSettings.asset:7-27`、`ProjectSettings/ProjectSettings.asset:15-16`
- 全量检索：对 `Assets/**/*.cs` 检索 `Addressables|AssetReference|AsyncOperationHandle` = 2 处注释、0 处代码；脚本 `.cs.meta` GUID ↔ 场景/预制体 YAML 引用逐条比对；对全部 `.unity/.prefab` 检索 GameSceneSO 资产 GUID 定位引用者
- 历史：`git show e7c9ef8 -- Assets/Scenes/InitialScene.unity`、`git log e7c9ef8..HEAD -- Assets/Scenes/InitialScene.unity`（空）
- 既有实证输入：`Temp/doc-discovery/asset-data-pipeline.json`

**已核验（静态证据充分，可作事实引用）**

- `RequestSceneLoad` 是唯一入口且「先广播后执行」；现有 4 个调用方 + 1 处启动自调用，全部经该入口。
- `isLoading` 覆盖「广播后 → OnLoadCompleted」整窗，窗口内请求被 Warning 拒绝。
- 加载顺序、`CanStreamedLevelBeLoaded` 前置校验只报错不中断、`firstLoaded`/`currentScene`/`currentScenes[0]` 的取值规则。
- `PersistentSceneRegistry` 为纯静态 HashSet，按 `sceneName` 注册，`SubsystemRegistration` 复位。
- 存档键 = `GameSceneSO.SaveKey`，写入侧 `SaveDataManager.cs:75/96/104-113`，读回侧 `SaveSystem.cs:199-212` + `SceneDataForSave.gameScenes`（PersistentScene.unity:7053-7056）。
- Build Settings 6 条场景与顺序；Addressables 代码零引用、数据层残留位置。
- 常驻场景列表当前实际指向 MenuScene.asset（GUID 在 InitialScene.unity:155 与 MenuScene.asset.meta 双向吻合）。

**未核验（已写入 Guide 第 5 节，均标注「未运行 Unity 验证」）**

- 静态类跨场景存活与 SubsystemRegistration 复位时机的运行时表现
- 从 InitialScene 启动时的实际失败链（PersistentScene 不加载 ⇒ SceneChanger.Instance 为 null）
- SaveKey 编辑器/打包两分支的等价性与同名场景去重
- 回菜单时 StartingMenu 不被卸载、`SetObjects(false)` 的实际表现
- 本次未运行 Unity、未执行构建、未做 git 写操作

**发现的缺陷（已写入 Guide 第 4 节）**

1. 高｜`InitialScene.unity:154-155` 的 `persistentScenes` 被 `e7c9ef8` 误指为 MenuScene（原值为 `823843722642c914dba2fb270e6b288d` = PersistentScene.unity），且 PersistentScene 无任何 GameSceneSO 组可承载，导致启动链路不加载任何管理器；`git log e7c9ef8..HEAD` 显示未修复。
2. 低｜`RetrySceneSO.asset` 仍为旧 schema（只有 `sceneReference.m_AssetGUID`、无 `sceneName`），接线后会被 `SceneChanger.cs:223` 静默跳过（早于 Build Settings 校验，连 Error 都不打）。
3. 中｜`Start.prefab` / `TitleButton.prefab` 残留 `loadEventSO` / `retryEventSO` 两个已删序列化键。
4. 中｜Addressables `m_Enabled:1` + `m_BuildAddressablesWithPlayerBuild:1` 仍会在 Player 构建产出 bundle，而代码已脱离。
5. 低｜`TestScene` 在 Build Settings 内但无任何引用（孤儿场景）。
6. 低｜旧文档冲突：`Docs/My_ARPG_MVCS项目现状.md:198` 的接线检查项未覆盖 `InitialScene.persistentScenes`，是缺陷 1 漏过验证清单的原因；`Docs/My_ARPG_重构优化清单_已解决.md:78` 的描述与代码一致。注意该现状文档在本次会话期间被其它 agent 改动过（测试计数 47→53、行号漂移），引用时以当次实读为准。
7. 低｜`SceneChanger.Start()` 与 `InitialLoad` 的加载发起顺序无显式执行序保护（仅注释约定）。

**结论**：SceneFlow 域权威文档与 Router 已建立，维护计数保持 `0/5`（本次为文档建立，非任务型增量）。本域仍存在 1 条高危启动链路缺陷，需用户确认后再修 `Assets/Scenes/InitialScene.unity`。