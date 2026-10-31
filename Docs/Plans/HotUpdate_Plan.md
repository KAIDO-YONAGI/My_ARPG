# 资源热更方案：Addressables 远端 Catalog 与场景线上化

状态：`草稿（未实施）`
最后更新：`2026-10-19`
文档性质：方案草稿，存放在非权威参考区 `Docs/Plans/`，**不属于** `Y_MultipleAgentWorkflow/` 的权威文档；未落地前不参与该工作流的路由与维护计数。
适用范围：热更的**能力边界、远端 Catalog 配置、场景线上化后的加载链路改造、发布 SOP 与回滚、验证清单，以及后续代码热更（HybridCLR）的程序集划分原则**。场景组语义与常驻场景注册归 SceneFlow 域；存档键与 schema 归 Data 域；打包环境、签名与工程设置归 Build 域；邮件的数据下发归邮件域（本文件只定义共用版本号）。工程内除 Addressables 的静态配置外尚无实现。
引用约定：文中 `Xxx_Guide` 与 `Xxx_Guide §n` 均指 `Y_MultipleAgentWorkflow/` 下的权威文档。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `热更`、`不重装 APK`、`线上更新` | §2 现状、§4 设计 |
| `能不能热更代码`、`改 C# 要不要发版` | §3 能力边界 |
| `Addressables`、`catalog`、`ServerData` | §2.1、§5.1 |
| `场景线上化`、`CanStreamedLevelBeLoaded` | §4.4 |
| `启动检查更新`、`下载进度`、`Bootstrap` | §4.1、§4.2、§5.3 |
| `内容更新`、`Update a Previous Build`、`回滚` | §6 |
| `HybridCLR`、`asmdef`、`AOT 集` | §5.4 |
| `真机拉不到资源`、`明文 HTTP` | §2.3、§5.3 |
| `面试怎么讲` | §9 |

## 2. 现状取证

### 2.1 Addressables：已装、场景已标，但运行时零调用

| 项 | 实测值 | 证据 |
|---|---|---|
| 包版本 | `com.unity.addressables 1.22.3` | `Packages/manifest.json` |
| 运行时 API 调用 | **0 处**（全仓无 `using UnityEngine.AddressableAssets`） | 产品脚本全量检索 |
| Default Group | `Scenes`（`ca89086c…`） | `AddressableAssetSettings.asset` |
| Scenes 组条目 | **5 条**：`Scene1`、`StartingMenu`、`Scene2`、`PersistentScene`、`TestScene`；**不含** `InitialScene` | `AssetGroups/Scenes.asset` |
| 条目地址 | 全资产路径，如 `Assets/Scenes/GameScene/Scene1.unity` | 同上 |
| 组的 Build / Load 路径 | `Local.BuildPath` / `Local.LoadPath`（即 `[Addressables.BuildPath]/[BuildTarget]` 与 `{RuntimePath}/[BuildTarget]`） | `Scenes_BundledAssetGroupSchema.asset` 的 `m_BuildPath` / `m_LoadPath` 指针 |
| Bundle 模式 / 压缩 | `PackTogether` / LZ4（`m_BundleMode: 0`、`m_Compression: 1`） | 同上 |
| 内容更新允许 | `StaticContent: 0`（可参与内容更新） | `Scenes_ContentUpdateGroupSchema.asset` |
| 远端 Catalog | **关闭**（`m_BuildRemoteCatalog: 0`） | `AddressableAssetSettings.asset` |
| Remote.LoadPath | 仍为默认占位 `http://[PrivateIpAddress]:[HostingServicePort]` | 同上 |
| 随包构建 | `m_BuildAddressablesWithPlayerBuild: 1` | 同上 |
| 内容状态基线 | `Android/`、`Windows/`、`WebGL/` 三个 `addressables_content_state.bin` | `AddressableAssetsData/` |
| StreamingAssets 实际产物 | 只有 `GameGuide.txt`，**没有** `aa/` 目录 | 目录实测 |

结论：这是**半套骨架**——资产侧准备好了（场景已标、内容更新组已建、每平台都有内容状态基线），但运行时一次都没用过，路径还是本地，Catalog 不出远端。方案要做的是把它接上，不是从零搭。

### 2.2 场景加载链路（当前实现）

| 环节 | 实现 | 证据 |
|---|---|---|
| 常驻场景 | `InitialLoad` 在 `Awake` 注册 `PersistentSceneRegistry` 后起协程，逐条 `Application.CanStreamedLevelBeLoaded` + `SceneManager.LoadSceneAsync(name, Additive)` | `Assets/Scripts/Pipeline/InitialLoad.cs:35-42` |
| 内容场景 | `SceneChanger.RequestSceneLoad` 是唯一入口，先广播 `loadEventSO` 再执行；整组卸载后按序加载 | `SceneChanger.cs:103-107`、`:198-236` |
| 加载 | 同样 `CanStreamedLevelBeLoaded` 校验后 `SceneManager.LoadSceneAsync(scene.sceneName, Additive)` | `SceneChanger.cs:254-283` |
| 卸载 | `SceneManager.UnloadSceneAsync(loaded)`，跳过已注册常驻场景 | `SceneChanger.cs:205-233` |
| 场景 SO | `GameSceneSO.sceneName` 是加载键；`SaveKey` 取场景资产 GUID | `Assets/Scripts/Pipeline/SO/GameSceneSO.cs` |
| Build Settings | 6 条全部启用，顺序 `InitialScene`、`PersistentScene`、`StartingMenu`、`Scene1`、`Scene2`、`TestScene` | `ProjectSettings/EditorBuildSettings.asset` |

即：**场景既在 Build Settings 里，又在 Addressables 组里**——同一份场景会被打进包体又打进 bundle，包体重复，且远端 catalog 永远用不上。

### 2.3 平台与构建事实

| 项 | 值 | 证据 | 对本方案的影响 |
|---|---|---|---|
| 引擎 | `2022.3.62f3c1` | `ProjectVersion.txt` | `c1` 为中国版构建；HybridCLR 阶段需单独冒烟 |
| **Android 后端** | **IL2CPP**（`scriptingBackend: Android: 1`） | `ProjectSettings.asset:712-713` | 代码热更的前提之一已满足，**不需要切后端** |
| 其他平台后端 | 默认（Mono） | 同上（无其他平台覆盖项） | Editor 内验证走 Mono，与真机行为不同 |
| API 兼容级 | `apiCompatibilityLevel: 6`（.NET Standard 系） | `ProjectSettings.asset` | HybridCLR 要求 .NET Standard 2.1 或 .NET 4.x，落地前在 Player Settings 面板复核具体档位 |
| 托管剥离 | `managedStrippingLevel` 无 Android 条目（走默认） | `ProjectSettings.asset` | HybridCLR 阶段必须显式锁定 + `link.xml` |
| 架构 / SDK | ARM64、minSdk 22、targetSdk 33 | `ProjectSettings.asset:178-179`、`AndroidTargetArchitectures: 2` | targetSdk 33 触发下一条 |
| **明文 HTTP** | `insecureHttpOption: 0`（**不允许**） | `ProjectSettings.asset:828` | 局域网 `http://` 在真机被系统拒绝，见 §5.3 |
| **INTERNET 权限** | `ForceInternetPermission: 0`（Auto） | `ProjectSettings.asset:185` | Auto 靠静态分析推断，远端加载可能拿不到权限，见 §5.3 |
| 版本号 | `bundleVersion: 1.0`、`AndroidBundleVersionCode: 1` | `ProjectSettings.asset` | 版本门禁的最小载体 |
| 签名 | Android Debug 证书，未配自有 keystore | `Build/AndroidBuild_Guide.md §2.5`（`Y_MultipleAgentWorkflow/Build/`） | 上架前需换，但不阻塞热更验证 |

> **一处需要更正的旧结论**：此前分析曾把本项目记为「全平台 Mono」，因为它只看了那行空的 `scriptingBackend:`。实际平台覆盖在紧随其后的 `Android: 1`，即 **Android 已是 IL2CPP**。这条更正直接影响阶段 D 的成本估算——不必再付「切后端」的代价。

### 2.4 三个「零」

| 零 | 实测 | 后果 |
|---|---|---|
| 网络代码 0 处 | 全仓 `UnityWebRequest` / `HttpClient` / `System.Net` 命中 0 | 下载层、超时、重试、断点全部要新建 |
| 服务端 0 个 | 存档走本地 JSON（`Application.persistentDataPath`，`SaveSystem.cs`） | 远端目录用静态文件托管即可，不必引入服务端 |
| asmdef 0 个 | `Assets/` 下无 `.asmdef`，全部代码在 `Assembly-CSharp` | 阶段 D 必须先拆程序集，这是代码热更的硬门槛 |

## 3. 能力边界

| 能热更 | 不能热更（必须发新 APK） |
|---|---|
| 贴图、音频、动画片段 | 新增或修改任何 `MonoBehaviour` / `ScriptableObject` **脚本** |
| 预制体布局、组件参数 | 给已有脚本**新增字段**（字段名即序列化键，包内脚本没有该字段就丢数据） |
| 场景摆放、光照烘焙结果 | 新增或修改枚举成员（含红点 `RedDotKey`） |
| SO 数值、对话/任务文本 | 新增 UI 面板脚本、新增红点视图 |
| 对话树结构（资产间引用） | 修 C# 逻辑 bug |

一句话：**本轮热更能改"内容"，改不了"规则"。** 阶段 D 上 HybridCLR 后才能改规则。

## 4. 设计

### 4.1 目标链路

```
Bootstrap.unity（新，Build Settings #0）
  └ AddressablesBootstrapper
       Addressables.InitializeAsync()
       → CheckForCatalogUpdates() / UpdateCatalogs()
       → GetDownloadSizeAsync()            // 0 则跳过
       → DownloadDependenciesAsync()       // 带进度条
       → LoadSceneAsync("InitialScene", Additive) + SetActiveScene
       → 自毁，交棒给 InitialLoad / SceneChanger
```

### 4.2 为什么用独立 Bootstrap 场景

因为 `SceneChanger.Start` 有一条明确的时序不变量，`SceneChanger.cs:109-116` 的注释写得很清楚：

> 首个场景组请求放在 Start：同批所有 Awake/OnEnable 已跑完，`TimeManager` 实例与 `SaveDataManager`/`UIManager` 的事件订阅必然就绪，广播不漏听众。

把异步的网络检查插进 `InitialLoad.Awake`，等于让「场景切换」早于「订阅就绪」或晚于它，两种都可能漏广播。**加一个前置场景只增加一次加载，不改任何既有不变量**——这是本方案里性价比最高的一个决定。

代价：多一个场景与一次 Additive 加载（约几十毫秒），换来零时序风险。

### 4.3 三条版本号

| 版本号 | 管什么 | 变更频率 | 载体 |
|---|---|---|---|
| `appVersion` | APK 版本，低于远端 `minAppVersion` 时强制更新 | 极少 | `Application.version` ↔ `bundleVersion` |
| `resourceVersion` | Addressables 资源与 catalog | 中 | 远端 `version.json` |
| `hotfixVersion` | 热更 dll（阶段 D） | 中 | 同上 |
| `mailVersion` | 运营邮件 JSON（邮件域用，本域只提供目录约定） | 高 | 同上 |

拆开的原因：改一封邮件不该牵动热更版本号。合成一个版本号是最常见的自找麻烦。

### 4.4 场景线上化的最小改动

**目标**：场景不再从 Build Settings 加载，改从 Addressables 加载；组条目的地址规范为 `sceneName`。

| 改动点 | 现状 | 目标 |
|---|---|---|
| 组地址 | `Assets/Scenes/GameScene/Scene1.unity`（全路径） | `Scene1`（与 `GameSceneSO.sceneName` 同值） |
| `SceneChanger` 加载 | `CanStreamedLevelBeLoaded` + `SceneManager.LoadSceneAsync(name, Additive)`（`:254-283`） | `Addressables.LoadSceneAsync(scene.sceneName, Additive)` |
| `SceneChanger` 卸载 | `SceneManager.UnloadSceneAsync(loaded)`（`:220`） | `Addressables.UnloadSceneAsync(handle)`——**必须持有 handle** |
| `LoadedScene` 结构 | `Asset` + `Scene Instance` | 追加 `AsyncOperationHandle<SceneInstance> Handle` |
| `InitialLoad` 加载 | 同 `SceneManager`（`:35-42`） | 同 Addressables |
| Build Settings | 6 条 | 收缩到只剩启动场景（阶段 C 后只剩 `Bootstrap`） |

**为什么卸载必须换 API**：`Addressables.LoadSceneAsync` 返回的 `Scene` 若用 `SceneManager.UnloadSceneAsync` 卸载，界面会正常消失，但 Addressables 的内部引用计数不会被释放，对应 bundle 常驻内存——这是「看不出问题、但内存只涨不落」的经典坑。必须用 handle 卸载。

**地址用 `sceneName` 而不是 GUID**：`sceneName` 本来就是 `GameSceneSO` 的加载键，改地址不需要动 `GameSceneSO` 的字段；且 address 是 catalog 里的字符串键，与 GUID 无关，改名不影响任何存档。

### 4.5 旧存档兼容

`GameSceneSO.SaveKey` 的注释已经声明：取值是场景资产 GUID，**「与旧 Addressables 方案 `sceneReference.AssetGUID` 的取值完全一致，旧存档仍可解析」**。所以场景 Addressable 化之后：

- `SaveData.sceneIDAndPlayerPos.sceneID` 存的 GUID 不变；
- `SceneDataForSave.gameScenes` 反查逻辑不变；
- 旧档直接可读。

这是现成的兼容保障，不需要额外迁移代码。**但不能改 `GameSceneSO` 的字段名**（`Layering_Guide §3.6`）。

### 4.6 离线策略（待确认）

首次安装是否必须离线可玩，现在没有定论，两种形态的成本差别明显：

| 形态 | 做法 | 代价 | 适用 |
|---|---|---|---|
| **A 联网可玩** | 内容组全部走远端，首次启动必须联网 | 无网/被墙 = 黑屏，无法进游戏 | 第一轮打通链路 |
| **B 内置基线 + 远端增量** | 基线内容随 APK 内置一份（`StreamingAssets/aa/Android`），启动先读内置 catalog 再查远端更新 | 引导脚本多一步、包体变大、需要一份「基线/增量」双目录规则 | 面向真实玩家 |

**建议**：阶段 A–C 用 A 打通并验证，对外表述为「资源热更链路已打通」；上线前切 B。**未确认前不要在文档里写成 B 已具备。**

## 5. 落地步骤

### 5.1 阶段 A：配置链路（不改一行代码）

1. Profiles 新建 `Dev-LAN`：`Remote.LoadPath = http://<局域网IP>:<端口>/[BuildTarget]`；`Remote.BuildPath` 保持 `ServerData/[BuildTarget]`。
2. `Scenes` 组的 **Build & Load Paths** 从 `Local` 切到 `Remote`（改的就是 schema 上 `m_BuildPath` / `m_LoadPath` 两个指针）。
3. 打开 **Build Remote Catalog**（`m_BuildRemoteCatalog`）。
4. 组条目地址批量改为 `sceneName`。
5. 用 Addressables 内置 **Hosting** 服务托管（默认 `Remote.LoadPath` 就是它的占位变量，无需自建 HTTP 服务器）。
6. 跑一次 Addressables 构建，检查 `ServerData/Android` 产物与 **Analyze** 面板无阻塞警告。

**验收**：`ServerData/Android` 下有 catalog（json + hash）与 bundle；用浏览器能拉到 `catalog.json`。**注意阶段 A 不涉及运行时，代码未改，场景仍从 Build Settings 加载，别在这里下"热更成功"的结论。**

### 5.2 阶段 B：场景线上化（本轮最大风险）

按 §4.4 改 `SceneChanger` 与 `InitialLoad`，随后从 Build Settings 移除 5 个内容场景。

**必须跑的回归**（当前工程没有场景加载相关的自动化测试，只能手工）：

| 场景 | 期望 |
|---|---|
| 启动 → 菜单 | 常驻场景与菜单场景都加载成功 |
| 菜单 → 游戏 | 整组卸载旧内容场景，加载新组，玩家位置正确 |
| 读档 | `LoadSave` 定位到正确场景并加载，位置正确 |
| 返回菜单 | 内容场景卸载干净，菜单正常 |
| Retry | `RetrySceneSO` 流程正常 |
| 常驻场景不被卸载 | `PersistentSceneRegistry` 判定生效，无 "Skipped unload" 之外的异常日志 |

### 5.3 阶段 C：Bootstrap 与真机验证

1. 新增 `Assets/Scripts/Pipeline/Boot/AddressablesBootstrapper.cs` 与 `Assets/Scenes/Boot/Bootstrap.unity`，Build Settings 置索引 0。
2. 用协程实现 §4.1 流程；下载失败给可重试提示，不吞异常。
3. **Android 两个开关必须先改，否则真机必然失败**：

| 设置 | 现值 | 改为 | 不改的表现 |
|---|---|---|---|
| Player Settings > Other Settings > **Allow downloads over HTTP**（`insecureHttpOption`） | `0` Not allowed | `1` Allowed in development builds | targetSdk 33 下 `http://` 被拒：`Cleartext HTTP traffic not permitted` |
| **Internet Access**（`ForceInternetPermission`） | `0` Auto | `1` Require | Auto 靠静态分析推断，可能不加 `INTERNET` 权限，远端加载直接失败 |

4. 真机用局域网 IP，**不要**把 `Dev-LAN` 的 IP 提交进版本库（会变）。

### 5.4 阶段 D：代码热更（HybridCLR，尚未开始）

#### 前置：程序集划分

工程现在 0 个 asmdef。HybridCLR 必须有一个独立的「热更程序集」。划分原则：

| 集合 | 放什么 | 理由 |
|---|---|---|
| **AOT 集** | `Contracts`、`Pipeline`、**全部 `ScriptableObject` 类**、**全部 `MonoBehaviour`**、第三方 | SO 与 MonoBehaviour 会被场景/预制体/bundle 按脚本 GUID 引用，必须在包体内可解析。放进热更集会直接 `MissingScript` |
| **热更集** | 纯 C# 玩法逻辑：Model / Service / Controller、数值规则、任务判定、邮件逻辑 | 不参与资产序列化引用，才允许整体换 dll |

工程已有一条纯 C# 逻辑线（`PlayerStatsModel`、`IPlayerStatsReadOnly`、`ExperienceController` / `HealthController` / `StatsCanvasController`、`CanvasFocusStack`），**这批就是热更集的第一批内容**，划分成本比想象中低。

**必须先拆的一处反向依赖**：`Pipeline` 引用 `Gameplay` 全工程只有 1 处——`Assets/Scripts/Pipeline/SO/PlayerStatsSO.cs:2` 的 `using Gameplay.Player.Models;`。不拆掉它，AOT 集就反向依赖热更集，形成循环。

#### 六步管线

| 步 | 做法 | 落点 |
|---|---|---|
| ① 版本检查 | 远端 `hotupdate/version.json`：`appVersion` / `resourceVersion` / `hotfixVersion` / `minAppVersion` | `Bootstrap` |
| ② 差量下载 | 资源走 Addressables 的 catalog 差异；dll 走清单的文件级差异 | 下载层 |
| ③ MD5 校验 | 每个 dll 落地后算 MD5 与清单比对，失败重下 | 下载层 |
| ④ 补充 AOT 元数据 | `RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet)`，为 AOT 泛型实例化补元数据 | `HotUpdateLoader` |
| ⑤ `Assembly.Load` | 加载热更 dll 字节 | 同上 |
| ⑥ 反射入口 | `GetType("Game.HotUpdate.Entry").GetMethod("Start").Invoke(null, null)` | 同上 |

#### 文件清单

| 文件 | 说明 |
|---|---|
| `Assets/Scripts/Pipeline/HotUpdate/HotUpdateManifest.cs` | `version`、`files[{name,size,md5}]`、`aotDlls[]` |
| `Assets/Scripts/Pipeline/HotUpdate/HotUpdateDownloader.cs` | 下载 + MD5 + 重试 |
| `Assets/Scripts/Pipeline/HotUpdate/HotUpdateLoader.cs` | 补元数据 → Load → 反射入口 |
| `Assets/Scripts/HotUpdate/Entry.cs` | 热更集入口，`public static void Start()` |
| `Assets/Editor/HybridCLRBuild.cs` | 一键：生成 `link.xml`、拷贝 AOT dll、编译热更 dll |
| `Assets/link.xml` | 保住 AOT 类型不被剥离 |

#### 不要假设的五个点

1. **Unity 中国版兼容性**：本机是 `2022.3.62f3c1`（中国版 `c1`）。HybridCLR 官方覆盖 2022.3，但中国版是否完整验证需先做**最小 asmdef 冒烟**（Editor + Android 各一次），再谈全量迁移。
2. **剥离等级**：Android 未单独配置，走默认。不能依赖默认值，必须显式锁定并配 `link.xml`。
3. **Editor 与真机不同**：Editor 走 Mono，`LoadMetadataForAOTAssembly` 无实际作用。"Editor 能跑"不等于"真机能跑"。
4. **构建时长**：Android + ARM64 + IL2CPP 打包明显更慢，日常迭代走 Editor 热更加载验证，只在里程碑打真机包。
5. **热更集里不能出现新的 SO / MonoBehaviour 类型**：新增面板、新增红点 View 仍要发版。

## 6. 发布 SOP 与回滚

```
① 出基线包
   Addressables 构建（远端 profile）→ 上传 ServerData/Android → 归档 addressables_content_state.bin → 打 APK

② 每次热更
   改内容 → Addressables > Update a Previous Build（选中归档的 content_state）
          → 上传 ServerData/Android → 通知客户端重启生效

③ 回滚
   远端保留上一版目录，服务端把 catalog 指回旧目录；不做客户端降级
```

两个硬规则：

- **热更时不要再打 APK**，也不要在此时触发「随包构建 Addressables」（`m_BuildAddressablesWithPlayerBuild` 此时应为 0 或至少确认不会覆盖远端 catalog）。
- `addressables_content_state.bin` 是内容更新的锚点，**必须随基线包一起归档**，丢失即无法做增量。

## 7. 验证清单

| 场景 | 验证点 |
|---|---|
| 编辑器 | Hosting 服务下 Play 模式能加载改动后的场景/预制体；改一张贴图 → 内容更新 → 重启 Play 可见 |
| 真机（局域网） | 两个开关已改；能拉到 catalog 与 bundle；进游戏无黑屏 |
| 弱网 | 下载中断后重进能续上或明确重试，不卡死 |
| 离线 | 断网启动：形态 A 给明确提示；形态 B 能进游戏 |
| 旧存档 | 旧档可读、场景定位正确（验 §4.5） |
| 内存 | 反复切场景后 bundle 引用计数能回落（验 §4.4 的卸载 API） |
| 回滚 | 服务端指回旧 catalog 后，客户端重启回到旧内容 |

## 8. 约定与硬边界

### 8.1 不能靠热更修正代码 bug

Mono/IL2CPP 下代码在包体里。任何「线上崩了发个热更就行」的预期在阶段 D 之前都是错的。

### 8.2 资产一旦被「非 Addressable 的根」硬引用，就无法热更

Addressables 的更新以 catalog 为单位。若某个内容场景/预制体仍被 Build Settings 或非 Addressable 资产硬引用，它会被打进包体且更新无效。**所以「部分转换」不成立——必须把引用根（场景）整体 Addressable 化。** 这也是 §5.2 为什么绕不开。

### 8.3 Build Settings 与 Addressables 同时持有同一场景 = 双份包体

现有 5 个场景正是这种状态（§2.2）。线上化之后必须从 Build Settings 移除，否则包体冗余且远端更新永远不生效。

### 8.4 不改 APK 时不要重建 Addressables 覆盖远端 catalog

`addressables_content_state.bin` 与「Update a Previous Build」是配套的。用「New Build」覆盖上传，会让所有已发布客户端的 catalog 与内容对不上。

### 8.5 StreamingAssets 文件名必须是 ASCII

Android Release 构建在 AAR 解包阶段对非 ASCII 文件名抛 `MalformedInputException`（`Build/AndroidBuild_Guide.md §2.3`，即 `Y_MultipleAgentWorkflow/Build/`）。Addressables 的 `aa/` 输出是 ASCII，不违规；但人工往 `StreamingAssets` 放东西时仍受此约束。

### 8.6 存档字段名不可改

`Layering_Guide §3.6`：字段名就是存档 JSON 键。热更换了包内字段名 = 直接坏档。

## 9. 面试讲解稿

### 9.1 30 秒版本

> 我把资源层从「内置进包」改成了「远端 Catalog」：启动时先用一个独立的 Bootstrap 场景检查 catalog 更新、算差量、带进度下载，再加载业务场景；场景从 `Build Settings` + `SceneManager` 迁到 `Addressables.LoadSceneAsync`，卸载必须换成 `UnloadSceneAsync(handle)`，否则 bundle 引用计数不释放。代码层留到第二阶段用 HybridCLR，前置是把工程从「一个 Assembly-CSharp」拆成 AOT 集与热更集。

### 9.2 关键取舍

| 取舍 | 备选 | 选择 | 为什么 |
|---|---|---|---|
| 先资源还是先代码 | 直接上 HybridCLR | 先资源后代码 | 资源改动集中在配置 + 一条场景加载链路，可独立验证；代码热更需要拆程序集与重做打包链路，两个同时做，出问题无法归因 |
| 引导逻辑放哪 | 塞进 `InitialLoad.Awake` | 独立 Bootstrap 场景 | `SceneChanger.Start` 依赖「同批 Awake/OnEnable 已完成」这一不变量，改它等于拿时序赌运气；加一个前置场景只多一次加载 |
| 场景地址用什么 | 资产全路径 / GUID | `sceneName` | `sceneName` 本来就是加载键；address 是 catalog 字符串键，改名不影响存档 |
| 卸载走哪条 API | `SceneManager.UnloadSceneAsync` | `Addressables.UnloadSceneAsync(handle)` | 前者界面消失但引用计数不释放，bundle 常驻内存 |
| 版本号几个 | 一个总版本 | `app` / `resource` / `hotfix` / `mail` 四条 | 改一封邮件不应牵动热更版本号 |
| 首次安装是否离线 | 必须离线 | 先接受联网，上线前改内置基线 | 先把链路打通，再补工程化；但要在文档里明确标成待办，不能含糊 |

### 9.3 常见追问

**Q：Addressables 和 HybridCLR 各管什么？**
A：Addressables 管**内容**（资源、场景、配置），HybridCLR 管**代码**（dll）。两者不能互相替代：热更换不了脚本类型，因为场景/预制体按脚本 GUID 解析，包里没有该脚本就是 `MissingScript`。

**Q：为什么你的场景要整体 Addressable 化，不能只把几个预制体标成 Addressable？**
A：因为更新以 catalog 为单位，而资产被「非 Addressable 的根」硬引用时会被打进包体、更新无效。引用根就是场景，所以必须从场景这一层整体切。

**Q：AOT 集和热更集怎么分？**
A：判据是「会不会被资产序列化引用」。`ScriptableObject` 与 `MonoBehaviour` 会被场景/预制体按 GUID 引用，必须在 AOT；纯逻辑（Model/Service/Controller）不参与资产引用，才能进热更集。工程里原本就有一条纯 C# 数值线，正好是热更集的第一批内容。

**Q：内容更新能新增资产吗？**
A：不能想当然。Addressables 的 Content Update 对**新增**条目有额外限制（条目标记需要处理），第一次做必须用「改一张已有贴图」实测确认，而不是照「能加新资产」排期。

**Q：回滚怎么做？**
A：服务端把 catalog 指回上一版目录，客户端重启即回到旧内容；不做客户端降级（降级要处理本地缓存与新 catalog 的冲突，收益不值）。

**Q：Android 真机拉不到资源，你怎么排查？**
A：按三层查。① 权限与明文：`INTERNET` 权限是否真的在，`Allow downloads over HTTP` 是否允许（targetSdk 33 默认拒 cleartext）；② 网络：局域网 IP 是否可达、端口是否被防火墙拦；③ catalog：`.hash` 与 `.json` 是否可访问、MIME 是否正确。本工程这两项默认值都不对，是必改项。

### 9.4 不能吹的部分（诚实边界）

- 状态是 **Proposal**，代码未落地。当前工程的事实是「Addressables 装好、场景标了，但运行时零调用、路径是本地、远端 Catalog 关闭」。
- 没有做真实 CDN 与灰度；局域网验证不等价于线上。
- 没有做资源版本回滚的自动化，SOP 是人工切目录。
- 代码热更（阶段 D）**完全未开始**，且前置的程序集拆分尚未动手。
