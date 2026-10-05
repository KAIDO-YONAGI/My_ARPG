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
- `TestBaseline_Guide.md`：测试位置与框架、52 个用例的分布、覆盖与零覆盖域、纯 C# 选型及其边界、asmdef 约束。
- 只读检查命令见 `..\Workflow\Project_Validation_Guide.md`，涵盖代理状态查询、产物与签名检查。
