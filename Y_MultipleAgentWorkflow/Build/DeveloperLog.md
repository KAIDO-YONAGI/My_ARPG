# Build Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Build 域权威文档

**写入的文件**

- `Y_MultipleAgentWorkflow/Build/AndroidBuild_Guide.md`（`BUILD-ANDROID-GUIDE`，Active）
- `Y_MultipleAgentWorkflow/Build/ProjectConfig_Guide.md`（`BUILD-PROJECTCONFIG-GUIDE`，Active）
- `Y_MultipleAgentWorkflow/Build/TestBaseline_Guide.md`（`BUILD-TESTBASELINE-GUIDE`，Active）
- `Y_MultipleAgentWorkflow/Build/Router.md`（由英文占位模板改写为项目中文 Router 格式）

**依据的证据路径**

- 构建：`Docs/UnityAndroidBuildGuide.md`、`Docs/UnityAndroidBuildVerification.md`、`Docs/UnityAndroidBuildDiagnostics.skill.md`、`Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1`、`README.md:161-164`
- 配置：`ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`、`ProjectSettings/EditorBuildSettings.asset`、`ProjectSettings/ProjectSettings.asset`、`ProjectSettings/EditorSettings.asset`、`Assembly-CSharp-Editor.csproj`、`Assembly-CSharp.csproj`、`Assets/AddressableAssetsData/**`
- 测试：`Assets/Tests/Editor/*.cs`（5 文件）、`Assets/Scripts/Pipeline/Pathfinding/AStarOpenHeap.cs`、`Assets/Scripts/Pipeline/UI/SystemCanvasManagers/CanvasFocusStack.cs`、`Assets/Scripts/Contracts/MyEnums.cs`、`Directory.Build.targets`
- 线索输入：`Temp/doc-discovery/build-runtime-tooling.json`

**本轮实际核验（均为只读，未运行 Unity、未修改工程文件）**

- 产物位置与体量：`Builds/` 下仅 `Android/`，`My_ARPG-release.apk` = 28,509,358 字节（2026-08-09 18:06:23）、`My_ARPG.apk` = 45,437,354 字节（2026-08-09 17:21:03）。与 `Docs/UnityAndroidBuildVerification.md:32` 一致 —— **不存在“文档写 Builds/Android、实测在 Builds/”的不一致**，故未把它列为冲突。
- StreamingAssets：`Assets/StreamingAssets/` 仅含 `GameGuide.txt`（1372 字节）+ meta，非 ASCII 改名修复已落地。
- 代理现状：`Configure-UnityAndroidProxy.ps1 -Action Status -SkipNetworkCheck` → `State = Patched`、`SdkManagersFound = 1`、`GradleManaged = False`、`UserProxyHost/Port = 127.0.0.1 / 7890`；被补丁文件为 `...\cmdline-tools\6.0\bin\sdkmanager.bat`（`:68-72` 为 UNITY_ANDROID_PROXY 标记块、`:74` 为注入 `%UNITY_ANDROID_SDKMANAGER_PROXY_ARGS%` 后的调用行，同目录存在 `.unity-android-proxy.original` 备份）；`C:\Users\12248\.gradle\gradle.properties` 含六行 `systemProp.*` 代理属性但**不含**脚本标记 —— 实测印证了 `Docs/UnityAndroidBuildGuide.md:152-154` 关于 `GradleManaged = False` 仍可能代理生效的说明。
- Editor 与工具链：`D:\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe` 存在；内置 build-tools 有 `33.0.2`、`34.0.0`，各含 `apksigner.bat`。
- 场景与程序集：6 个构建场景文件在磁盘上全部存在；`Assets/` 下 `*.asmdef` 计数 = 0；`Assembly-CSharp-Editor.csproj:20,49-54,915-917` 证明 5 个测试文件与 `Assets/Editor/InputActionReferenceRebuilder.cs` 同处预定义编辑器程序集。
- 测试计数：逐文件清点 `[Test]` = 24 + 15 + 6 + 5 + 2 = **52**，`[TestCase]`/`[UnityTest]`/`[TestFixture]`/`[Category]` 均为 0。
- 全仓检索 `-buildTarget` / `-executeMethod` / `BuildPipeline.` / `BuildPlayerOptions` 无命中；唯一命中为 `Docs/My_ARPG_MVCS项目现状.md:202,204` 的 `-runTests -testPlatform EditMode`。

**未核验项**：已逐条写入各 Guide 的「未核验事项」小节，主要是序列化枚举值语义（`scriptingBackend.Android: 1`、`AndroidTargetArchitectures: 2`、`apiCompatibilityLevel: 6`、`activeInputHandler: 1`）、Test Runner 是否枚举全部 52 用例、`.apk`/`.aab` 与 Development/Release 的实际取舍、文档记录的 headless 测试命令当前是否可跑通。

**发现的缺陷（已写入 Guide）**

1. 无自有 keystore（`androidUseCustomKeystore: 0`）→ 发布阻断。
2. 仓库内不存在 Android Player 打包命令，构建不可 CLI 复现。
3. `Packages/manifest.json:3` 的 unity-mcp 指向仓库外且被 gitignore 的 `../../../Materials/` → 全新克隆解析不了依赖。
4. 旧文档测试计数过期（记 4 文件/46 用例，实测 5/52，差额为未跟踪的 `AStarOpenHeapTests.cs`）。
5. `Docs/My_ARPG_MVCS项目现状.md:166` 按“Domain Reload 关闭”叙述，而 `EditorSettings.asset:25` 为 `m_EnterPlayModeOptionsEnabled: 0`（未关闭）。
6. `Docs/README.md` 与两份 `My_ARPG_重构优化清单_*.md` 在 HEAD 中存在，但在当前工作区已被删除（`git status --porcelain -- Docs` 三个 ` D`，非本轮所为），而 `README.md:144` 仍链接它们 —— 旧文档引用链在当前工作区断裂。
7. 无 CI（`.github/` 不存在），`Builds/`、`Logs/`、`*.bat`、`*.csproj` 全被忽略 → 构建/测试证据只在本机。
8. 工作区相对 HEAD 是脏的（`manifest.json`、`packages-lock.json`、`AStarPathFinder.cs`、`SceneChanger.cs`、`PlayerDamageController.cs`、`SaveDataManager.cs`、两个场景、`README*.md` 已改；`AStarOpenHeap.cs`、`AStarOpenHeapTests.cs` 未跟踪），2026-08-09 的构建结果不保证对应当前工作区。

**维护计数**：`0/5` → 本轮为首次建立，未触发维护周期变更。
