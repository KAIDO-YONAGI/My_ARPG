# Workflow 业务开发日志

## 维护规则

按项目约定记录带日期的任务事实、验证证据与维护计数，现行结论同步到对应 Guide。了解现状优先读取 Guide；需要任务历史时，只定位并读取最近一天或两天的相关条目，禁止整份日志载入上下文。只有任务需要追溯历史时才按日期、标题或关键词定向读取更早条目。读取范围不改变存储保留，删除具体历史条目须经用户明确确认。纯文档维护不增加业务计数。

## 2026-11-07 现有编辑器实例内运行测试

`Project_Validation_Guide.md` 的 C12 登记 Test Runner 与 `TestRunnerApi` 的现有实例执行方式：先确认导入与编译完成，再执行普通 NUnit 用例，在结束回调导出 XML。实际运行结果为 `Logs/ui-version-editmode.xml`，80/80 通过，其中项目 79 项、Addressables 包 1 项。临时 Editor 入口已移除；headless 命令仍要求工程空闲。版本和路径按 `ProjectVersion.txt` 与本机实际安装同步。纯验证文档维护，计数不变。

## 2026-10-05：单元测试目录约定与根目录整理

- 写入 `Project_Validation_Guide.md`（`WF-PROJECT-VALIDATION`）：C12 增加用例位置一条，指向 `..\Build\TestBaseline_Guide.md` §3.1；§1 线索表加入 `单元测试` / `新增用例`。
- 根目录删除 IDE 构建中间产物 `obj\`、旧包 `Builds\Android\My_ARPG.apk`、场景备份 `Logs\StartingMenu.unity.20260912_185123.bak`，以及当天建在 `obj\` 下的 5 个一次性 .NET 验证工程。
- 已核验：根目录不再有 `obj\`；`Builds\Android\` 保留最新的 `My_ARPG-release.apk`；结构校验 112 pass、0 warning、0 error。

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

## 2026-10-05：协议初始化

- 建立分层路由、五层深度上限、维护计数周期与 WorkingAgent 并发控制。
- 工程专属资源登记在根 `Router.md` 的全局并发资源表。
