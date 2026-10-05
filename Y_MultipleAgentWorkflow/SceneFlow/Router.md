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
| 初始场景 / `InitialLoad` / `persistentScenes` | `SceneFlow_Guide.md` §2.5 |
| 构建场景列表 / `EditorBuildSettings` / 场景未入构建 | `SceneFlow_Guide.md` §2.9 |
| `Addressables` / `AssetReference` | `SceneFlow_Guide.md` §2.9 |
| 加载并发 / 请求被忽略 / `isLoading` | `SceneFlow_Guide.md` §2.1 |
| 重试 / `RetryButton` / 整组重载 | `SceneFlow_Guide.md` §2.8 §4.2 |
| 启动黑屏 / 管理器缺失 / `Instance` 为 null | `SceneFlow_Guide.md` §4.1 |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

本域为单层业务域，权威内容全部在 `SceneFlow_Guide.md`。

## 并发资源

- `workflow:SceneFlow`
- 场景管线脚本目录：`SceneChanger`、`PersistentSceneRegistry`、`Teleport`、`SceneDataForSave`、`CameraPixelSnap`
- 启动加载脚本 `InitialLoad`
- 场景 SO 契约 `GameSceneSO`
- 切场按钮脚本目录：`ButtonSceneToggler`、`RetryButton`
- 场景 SO 资产目录：全部 GameSceneSO 资产
- 场景文件目录
- 工程构建场景列表

## 能力边界

**Active（已确证、可直接依据执行）**

- 场景切换唯一入口是 `SceneChanger.RequestSceneLoad`，先广播后执行；新增切场触发点必须经该入口。
- 场景组以首个场景作为 `currentScene` 与存档键来源；`GameSceneSO.SaveKey` 是存档场景标识的唯一合法取值。
- `PersistentSceneRegistry` 由 `InitialLoad` 注册常驻场景，`SceneChanger` 卸载时跳过。
- 场景以 `SceneManager.LoadSceneAsync` 的 Additive 模式加载，加载前必须经 `Application.CanStreamedLevelBeLoaded` 校验，因此场景必须进工程构建场景列表。
- Addressables 运行时 API 零引用，产品脚本里只有两处提及该名称的注释；新增场景不需要 Addressables。

**Proposal（尚未实施，仅记录）**

- 清理 Addressables 残留设置 `m_Enabled` 与 `m_BuildAddressablesWithPlayerBuild`，以及 Scenes 组；这属构建产物治理，需用户确认后再动。
- 清理 `Start` 预制体与 `TitleButton` 预制体中 `loadEventSO`、`retryEventSO` 两个字段的序列化残留。

**需要用户确认的事项**

- 修改初始场景 `persistentScenes` 的登记项，对应 Guide §4.1 的缺陷修复；这属场景文件改动，影响启动链路。
- 修改 `SceneDataForSave.gameScenes` 这张存档键反查表；增删条目会影响旧档可读性。
- 删除或改写重试场景 SO 资产；它当前零引用，删除等价于放弃用场景 SO 承载重试的方案。
- 新增、移除构建场景或调整顺序；这影响 `Application.CanStreamedLevelBeLoaded` 的通过集合。
