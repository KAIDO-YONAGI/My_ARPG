# Workflow 业务开发日志

## 2026-10-05：协议初始化

- 建立分层路由、五层深度上限、维护计数周期与 WorkingAgent 并发控制。
- 工程专属资源与构建、运行屏障待确认。

## 2026-10-05：项目验证指南落成

- 写入 `Y_MultipleAgentWorkflow/Workflow/Project_Validation_Guide.md`（`WF-PROJECT-VALIDATION`，
  状态由 `PendingConfiguration` 改为 `Active`）。本指南只收录能在仓库内查到出处的命令，
  共 12 条（C1–C12），其中 C1–C4、C7、C9–C11 只读，C5/C6 带机器级副作用，C12 为 headless
  EditMode 测试。
- 依据：`Docs/UnityAndroidBuildGuide.md`、`Docs/UnityAndroidBuildVerification.md`、
  `Docs/My_ARPG_MVCS项目现状.md`、`Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1`。
- 已核验（只读，未运行 Unity）：`Unity.exe` 存在；`-Action Status -SkipNetworkCheck` 得
  `State = Patched`、`SdkManagersFound = 1`、`GradleManaged = False`、
  `UserProxyHost/Port = 127.0.0.1 / 7890`；`Builds/` 下只有 `Android/`，
  `My_ARPG-release.apk` 为 28,509,358 字节；两套 build-tools（33.0.2 / 34.0.0）各含
  `apksigner.bat`；`Logs/` 下没有 `test-editmode.xml`。
- 未核验：C12 能否跑通并产出通过结果；C8 的 `--list`；C11 的实际 verify；
  `GRADLE_USER_HOME` 未设置时 Gradle 的读取位置。均写入指南「未核验事项」。
- 缺陷：**Android Player 打包命令在仓库中不存在**（全仓检索 `-buildTarget`、
  `-executeMethod`、`BuildPipeline.`、`BuildPlayerOptions` 零命中），已写成 §5 的留白项；
  另有 C12 退出码不可靠、验证证据因忽略规则与无 CI 配置而不入库两项风险。

## 2026-10-05：文档写法规范与写法检查器

- `Workflow_Guide.md` 增设「文档写法」与「写法自查」两节：按当前状态陈述；资产按名称与职责、
  代码按类型名与方法名；不写资产路径与代码行号；结论用正文讲清，不用括号夹注；
  少用否定与转折句式；同一事实只在一处展开；DeveloperLog 保留时间线。
- 新增 `Workflow\Scripts\Check-DocStyle.ps1`，逐份文档检查资产路径、被禁扩展名、行号、
  来历叙述、修订小节与括号密度，error 时退出码为 1；库内引用与包 ID 先遮蔽再判定。
- `Project_Validation_Guide.md` 按同一规范重写，命令操作数保持可执行，文档简称与位置
  列成对照表。
