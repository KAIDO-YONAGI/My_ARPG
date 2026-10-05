# Workflow 业务开发日志

## 2026-10-05：协议初始化

- 建立分层路由、五层深度上限、维护计数周期与 WorkingAgent 并发控制。
- 工程专属资源登记在根 `Router.md` 的全局并发资源表。

## 2026-10-05：项目验证指南落成

- 写入 `Project_Validation_Guide.md`（`WF-PROJECT-VALIDATION`，
  状态 `Active`）。本指南收录本工程可执行的命令，共 12 条（C1–C12），其中 C1–C4、C7、C9–C11
  只读，C5/C6 带机器级副作用，C12 为 headless EditMode 测试。
- 依据：`..\..\Docs\UnityAndroidBuildGuide.md`、`..\..\Docs\UnityAndroidBuildVerification.md`、
  `..\..\Docs\My_ARPG_MVCS项目现状.md`、`..\..\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1`。
- 落成时只读取证：`Unity.exe` 存在；`-Action Status -SkipNetworkCheck` 得
  `State = Patched`、`SdkManagersFound = 1`、`GradleManaged = False`、
  `UserProxyHost/Port = 127.0.0.1 / 7890`；`Builds/` 下只有 `Android/`，
  `My_ARPG-release.apk` 为 28,509,358 字节；两套 build-tools（33.0.2 / 34.0.0）各含
  `apksigner.bat`。
- C12 实跑一次：`..\..\Logs\test-editmode.xml` 根节点 `result` 为 `Passed`，53 个用例、失败 0。

## 2026-10-05：单元测试目录约定与根目录整理

- 写入 `Project_Validation_Guide.md`（`WF-PROJECT-VALIDATION`）：C12 增加用例位置一条，指向 `..\Build\TestBaseline_Guide.md` §3.1；§1 线索表加入 `单元测试` / `新增用例`。
- 根目录删除 IDE 构建中间产物 `obj\`、旧包 `Builds\Android\My_ARPG.apk`、场景备份 `Logs\StartingMenu.unity.20260912_185123.bak`，以及当天建在 `obj\` 下的 5 个一次性 .NET 验证工程。
- 已核验：根目录不再有 `obj\`；`Builds\Android\` 保留最新的 `My_ARPG-release.apk`；结构校验 112 pass、0 warning、0 error。