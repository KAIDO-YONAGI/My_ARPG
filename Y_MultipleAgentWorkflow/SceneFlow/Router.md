# SceneFlow Router

文档 ID：`BUS-SCENEFLOW`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `SceneChanger` / `RequestSceneLoad` / 加载唯一入口 | `SceneFlow_Guide.md` §2.1 |
| `GameSceneSO` / `sceneName` / `SaveKey` / 场景键 | `SceneFlow_Guide.md` §2.2 §2.7 |
| `场景组` / `Additive` / `firstSceneToLoad` / `sceneToLoad` / `Teleport` | `SceneFlow_Guide.md` §2.3 §2.8 |
| `常驻场景` / `PersistentSceneRegistry` | `SceneFlow_Guide.md` §2.4 |
| `InitialScene` / `InitialLoad` / `persistentScenes` | `SceneFlow_Guide.md` §2.5 |
| `Build Settings` / `EditorBuildSettings` / 场景未入构建 | `SceneFlow_Guide.md` §2.8 |
| `Addressables` / `AssetReference` 场景迁移 | `SceneFlow_Guide.md` §2.9 |
| 加载并发 / 请求被忽略 / `isLoading` | `SceneFlow_Guide.md` §3.3 |
| 重试 / `RetryButton` / 整组重载 | `SceneFlow_Guide.md` §2.8 §4.2 |
| 启动黑屏 / 管理器缺失 / `Instance` 为 null | `SceneFlow_Guide.md` §4.1 |

> 本域无下级 Router（单层业务域），全部权威内容在 `SceneFlow_Guide.md`。

## 并发资源

- `workflow:SceneFlow`
- `path:Assets/Scripts/Pipeline/Scene/`
- `path:Assets/Scripts/Pipeline/InitialLoad.cs`
- `path:Assets/Scripts/Pipeline/SO/GameSceneSO.cs`
- `path:Assets/Scripts/Pipeline/UI/Buttons/`
- `path:Assets/GameSO/GameSceneSO/`
- `path:Assets/Scenes/`
- `path:ProjectSettings/EditorBuildSettings.asset`

## 能力边界

**Active（已确证、可直接依据执行）**

- 场景切换唯一入口 `SceneChanger.RequestSceneLoad`，先广播后执行；新增切场触发点必须经该入口。
- 场景组以首个场景作为 `currentScene` 与存档键来源；`GameSceneSO.SaveKey` 是存档场景标识的唯一合法取值。
- `PersistentSceneRegistry` 由 `InitialLoad` 注册常驻场景，`SceneChanger` 卸载时跳过。
- 场景以 `SceneManager.LoadSceneAsync(..., Additive)` 加载，加载前必须经 `Application.CanStreamedLevelBeLoaded`，因此场景必须进 `ProjectSettings/EditorBuildSettings.asset`。
- Addressables 运行时 API 零引用（仅剩 2 处注释），新增场景不需要 Addressables。

**Proposal（尚未实施，仅记录）**

- 清理 Addressables 残留设置（`m_Enabled` / `m_BuildAddressablesWithPlayerBuild`）与 Scenes 组，属构建产物治理，需用户确认后再动。
- 清理 `Start.prefab` / `TitleButton.prefab` 中已删字段（`loadEventSO` / `retryEventSO`）的序列化残留。

**需要用户确认的事项**

- 修改 `InitialScene.unity` 的 `persistentScenes`（§4.1 缺陷修复）——属场景文件改动，影响启动链路。
- 修改 `SceneDataForSave.gameScenes`（存档键反查表）——增删项会影响旧档可读性。
- 删除或改写 `RetrySceneSO.asset`（旧 schema 资产）——当前零引用，删除等价于放弃 Retry 场景 SO 方案。
- 新增/移除构建场景或调整顺序——影响 `CanStreamedLevelBeLoaded` 的通过集合。
