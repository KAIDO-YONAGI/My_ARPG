# Build Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Build 域权威文档

**写入的文件**

- `AndroidBuild_Guide.md`（`BUILD-ANDROID-GUIDE`，Active）
- `ProjectConfig_Guide.md`（`BUILD-PROJECTCONFIG-GUIDE`，Active）
- `TestBaseline_Guide.md`（`BUILD-TESTBASELINE-GUIDE`，Active）
- `Router.md`（由英文占位模板改写为项目中文 Router 格式）

**依据的证据路径**

- 构建：`..\..\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1`
- 配置：`..\..\ProjectSettings\ProjectVersion.txt`、`..\..\Packages\manifest.json`、`..\..\ProjectSettings\EditorBuildSettings.asset`、`..\..\ProjectSettings\ProjectSettings.asset`、`..\..\ProjectSettings\EditorSettings.asset`、`Assembly-CSharp-Editor.csproj`、`Assembly-CSharp.csproj`、`AddressableAssetsData` 目录
- 测试：`Tests/Editor` 目录下的 5 个测试文件、`AStarOpenHeap`、`CanvasFocusStack`、`MyEnums`、`Directory.Build.targets`

**本轮实际核验（均为只读，未运行 Unity、未修改工程文件）**

- 产物位置与体量：`Builds/` 下仅 `Android/`，`My_ARPG-release.apk` 为 28,509,358 字节，最后写入 2026-08-09 18:06:23；`My_ARPG.apk` 为 45,437,354 字节，最后写入 2026-08-09 17:21:03。
- StreamingAssets：目录下仅含 `GameGuide.txt` 及其元文件，1372 字节，非 ASCII 改名修复已落地。
- 代理现状：`Configure-UnityAndroidProxy.ps1 -Action Status -SkipNetworkCheck` 输出 `State = Patched`、`SdkManagersFound = 1`、`GradleManaged = False`、`UserProxyHost/Port = 127.0.0.1 / 7890`；被补丁文件为 Editor 内置 cmdline-tools 6.0 的 `sdkmanager.bat`，其中含 UNITY_ANDROID_PROXY 标记块与注入 `%UNITY_ANDROID_SDKMANAGER_PROXY_ARGS%` 后的调用行，同目录存在 `.unity-android-proxy.original` 备份；用户级 `gradle.properties` 含六行 `systemProp.*` 代理属性，不含脚本标记。
- Editor 与工具链：`D:\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe` 存在；内置 build-tools 有 `33.0.2`、`34.0.0`，各含 `apksigner.bat`。
- 场景与程序集：6 个构建场景文件在磁盘上全部存在；工程内 `*.asmdef` 计数为 0；`Assembly-CSharp-Editor.csproj` 收录 5 个测试文件与编辑器侧的 `InputActionReferenceRebuilder`，两者同处预定义编辑器程序集。
- 测试计数：逐文件清点 `[Test]` = 24 + 15 + 6 + 5 + 2 = **52**，`[TestCase]`/`[UnityTest]`/`[TestFixture]`/`[Category]` 均为 0。
- 全仓检索 `-buildTarget` / `-executeMethod` / `BuildPipeline.` / `BuildPlayerOptions` 均无命中；唯一命中为 `-runTests -testPlatform EditMode`。

**维护计数**：`0/5` → 本轮为首次建立，未触发维护周期变更。
