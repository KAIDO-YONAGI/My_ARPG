# Build Router

文档 ID：`BUS-BUILD`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `Android 构建` / `打包 APK` / `Release 构建` / `sdkmanager 代理` / `Detecting Android SDK` / `lintVitalAnalyzeRelease` / `ExtractAarTransform` / `AarExtractor` / `malformed input` / `StreamingAssets 中文名` / `keystore` / `apksigner verify` | `AndroidBuild_Guide.md` |
| `Unity 版本` / `包版本` / `manifest 依赖` / `asmdef` / `预定义程序集` / `构建场景列表` / `场景顺序` / `Player Settings` / `Product Name` / `包名` / `minSdk` / `targetSdk` / `Addressables` / `domain reload` | `ProjectConfig_Guide.md` |
| `EditMode 测试` / `PlayMode 测试` / `Test Runner` / `跑测试` / `测试总数` / `test-framework` / `NUnit` / `testResults XML` / `测试覆盖` | `TestBaseline_Guide.md` |
| 跨域的“在仓库里怎么验证” | `..\Workflow\Project_Validation_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Build`
- `path:Y_MultipleAgentWorkflow/Build/`
- UnityAndroid 代理配置脚本 `Configure-UnityAndroidProxy`：改它会动 Editor 安装目录与用户级 Gradle 配置，必须独占
- 工程设置资产：播放器设置、构建场景列表、编辑器版本记录
- 包清单与锁定清单，成对变更
- 构建产物目录与日志输出目录

## 能力边界

**Active 能力**

- `AndroidBuild_Guide.md`：Android 构建环境自检顺序；两类已知故障的根因与幂等修复，即代理未继承与非 ASCII 的 StreamingAssets 名；产物位置与签名现状。
- `ProjectConfig_Guide.md`：重现阶段一份可构建工程所需的最小事实，含 Editor 版本、包依赖、场景列表与顺序、Player/Build 配置原值、程序集结构、Addressables 状态。
- `TestBaseline_Guide.md`：测试位置与框架、52 个用例的分布、覆盖与零覆盖域、纯 C# 选型及其边界、asmdef 风险。
- 可复用的只读核验命令在 `..\Workflow\Project_Validation_Guide.md` 中逐条标注出处，涵盖代理状态查询、产物与签名检查。

**Proposal**

- 无。本域不提出尚未落地的测试计划或构建改造方案。

**需要用户确认后才能写入的空白项**

1. **Android Player 构建命令**：仓库内不存在 Android 打包命令——对全仓的 markdown、脚本、批处理、清单与源码检索 `-buildTarget`、`-executeMethod`、`BuildPipeline`、`BuildPlayerOptions` 均无命中，唯一命中的是测试用的 `-runTests -testPlatform`。已知做法是在编辑器里执行一次非 Development 构建。在用户给出命令之前，本域不书写任何打包命令。
2. **构建输出契约**：`.apk` 还是 `.aab`、Development 还是 Release、输出目录与文件名规则。
3. **测试通过判据**：退出码不可靠，需要 `testResults` XML 的具体判定规则与固定的结果文件路径。
4. **PlayMode 验证方式**：仓库内 `[UnityTest]` 为 0，这条路径目前靠手工验证；是否补自动化测试待定。
5. **正式发布流程**：keystore 的来源与保管方式；当前产物为 `CN=Android Debug`，属发布阻断项。
6. **Addressables 内容**：`Scenes` 组是否为需在打包前构建的内容，还是遗留配置。
7. **工作目录**：验证命令当前硬编码本机工程绝对路径与 Editor 安装绝对路径，是否要求路径无关。
