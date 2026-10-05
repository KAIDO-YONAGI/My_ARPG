# 项目验证指南

文档 ID：`WF-PROJECT-VALIDATION`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责本工程在仓库内能查到出处的验证命令与流程——环境检查、命令清单、进程与产物处理、环境变量，以及留白待用户确认的项。配置项语义归 `..\Build\ProjectConfig_Guide.md`；Android 故障根因归 `..\Build\AndroidBuild_Guide.md`。

简称与位置：

| 简称 | 位置 |
|---|---|
| Android 构建指南 | `Docs/UnityAndroidBuildGuide.md` |
| Android 构建验证记录 | `Docs/UnityAndroidBuildVerification.md` |
| 项目现状文档 | `Docs/My_ARPG_MVCS项目现状.md` |
| 代理配置脚本 | `Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1` |
| 工程版本文件 | `ProjectSettings/ProjectVersion.txt` |
| 包清单 | `Packages/manifest.json` |
| 忽略规则 | `.gitignore` |

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `怎么验证` / `验证流程` | §3 命令清单 |
| `跑测试` / `EditMode 测试` / `testResults` | §3 C12、§5 |
| `代理状态` / `sdkmanager 代理` | §3 C4–C8 |
| `产物在哪` / `apk 检查` | §3 C9–C10、§4 |
| `签名校验` / `apksigner` | §3 C11 |
| `打包命令是什么` | §5 留白项 |
| `环境变量` / `Gradle 全局配置` | §6 |
| `进程能不能停` / `能不能并发跑` | §4 |

## 2. 前置环境检查

按顺序确认，任一条不成立就停在当前层，不继续下层命令：

1. **没有任何 Unity 编辑器实例打开本工程**——headless 测试会另起一个编辑器实例，该工程当前必须处于空闲状态。
2. **Editor 二进制存在**。项目现状文档记录的路径为 `D:\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe`；本机该路径存在，版本与工程版本文件声明的 `2022.3.62f3c1` 一致。
3. **PowerShell 不低于 5.1**——代理配置脚本首行声明 `#Requires -Version 5.1`，Android 构建指南把这条列为前置条件；命令里的调用方式替换见 C5c。
4. **需要走代理时，代理软件在该地址与端口上监听**。
5. **路径前提**：C12 的命令把工程根与 Editor 路径写成绝对路径，换机器后按新机器改写。
6. **构建产物检查的前提**是产物已存在，见 §3 C9。

## 3. 命令清单

> 全部命令的出处都在仓库内，简称与位置的对应见表。仓库内查不到出处的「打包命令」不使用，见 §5。

### C1 读 Editor 版本

- 用途：确认打开与构建该工程所需的 Editor 版本。
- 命令：`Get-Content .\ProjectSettings\ProjectVersion.txt`
- 出处：工程版本文件本身；Android 构建指南引用同一个值。
- 成功判定：输出 `m_EditorVersion: 2022.3.62f3c1`。

### C2 定位 Editor 安装根目录

- 用途：拿到本机的 `$editorRoot`，供 C3 与 C11 使用。示例路径一律按本机实际值取。
- 命令：`$editorRoot = Get-Content -Raw "$env:APPDATA\UnityHub\secondaryInstallPath.json" | ConvertFrom-Json`
- 出处：Android 构建指南；代理配置脚本读取同一来源。
- 成功判定：得到非空路径字符串。本机为 `D:\Unity\Editor`。

### C3 定位 Editor 内置 `sdkmanager.bat`

- 用途：确认 Android 工具链位置；C8 需要它的完整路径。
- 命令：`Get-ChildItem -Path "$editorRoot\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\cmdline-tools\*\bin\sdkmanager.bat"`
- 出处：Android 构建指南；代理配置脚本使用同一个 glob。
- 成功判定：至少 1 条命中。本机命中 `cmdline-tools\6.0\bin\sdkmanager.bat`，与 C4 的 `SdkManagersFound = 1` 一致。探测不到时用 C5b 的 `-EditorRoot`。

### C4 查询 Android 代理配置状态

- 用途：查看 Editor 内置 sdkmanager 的补丁状态，以及 Gradle 配置与环境变量现状。只读命令。
- 命令：`pwsh -NoProfile -ExecutionPolicy Bypass -File .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 -Action Status`
- 出处：Android 构建指南；代理配置脚本的 `Status` 分支。
- 成功判定：结果表每行 `State` 为 `Patched`、`SdkManagersFound > 0`、代理在跑时 `ProxyReachable = True`。`GradleManaged = False` 与代理是否生效无关：本机 Gradle 全局配置含六行 `systemProp.*`，没有脚本标记。

### C5 安装 Android 代理配置

- 用途：让 Editor 内置 sdkmanager 与 Gradle 走代理，解决 `Detecting Android SDK` 卡住。
- 命令：`pwsh -NoProfile -ExecutionPolicy Bypass -File .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 -Action Install -ProxyHost <代理地址> -ProxyPort <代理端口>`
- 出处：Android 构建指南；代理配置脚本的 `Install` 分支。
- 成功判定：重跑 C4 得 `State = Patched`。脚本幂等，可重复执行。
- 副作用：这条命令带机器级副作用，执行前取得独占，见 §4。
- 变体 **C5b**：自定义 Unity 安装路径时加 `-EditorRoot '<Editor 安装根目录>'`。
- 变体 **C5c**：只有 PowerShell 5.1 的机器把 `pwsh -NoProfile` 换成 `powershell`。

### C6 卸载 Android 代理配置

- 用途：还原 `sdkmanager.bat`，删掉脚本托管的 Gradle 块与两个用户环境变量。
- 命令：`pwsh -NoProfile -ExecutionPolicy Bypass -File .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 -Action Remove`
- 出处：Android 构建指南；代理配置脚本的 `Remove` 分支。
- 成功判定：C4 显示 `NotPatched` 或备份已恢复；用户环境变量变为空。`Remove` 只删带脚本标记的块。

### C7 代理端口连通性

- 用途：确认代理软件在该端口上监听。
- 命令：`Test-NetConnection <代理地址> -Port <代理端口>`
- 出处：Android 构建指南。
- 成功判定：`TcpTestSucceeded : True`。

### C8 sdkmanager 真实网络路径验证

- 用途：证明 Java 与仓库网络路径可用。
- 命令：`& '<C3 得到的 sdkmanager.bat 完整路径>' --list`
- 出处：Android 构建指南。
- 成功判定：能列出远端包，没有长时间等待。
- **硬边界**：`--list` 真正访问远程仓库，是网络路径可用的证据；`--version` 只读本地版本。

### C9 构建产物存在性

- 用途：确认构建真的产出了东西。只读命令。
- 命令：`Get-ChildItem Builds\Android`
- 出处：Android 构建验证记录。
- 成功判定：存在 `My_ARPG-release.apk`。本机为 28,509,358 字节，写入时间 2026-08-09 18:06:23；同目录另有 `My_ARPG.apk`，45,437,354 字节。

### C10 包内条目与 ASCII 检查

- 用途：确认写入包的资源条目名全部是 ASCII。只读命令，用 .NET 压缩库直接读包，额外解压工具一律不用。
- 命令：
  ```powershell
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path 'Builds\Android\My_ARPG-release.apk'))
  $zip.Entries | Where-Object { $_.FullName -like 'assets/*' }            # 期望命中 assets/GameGuide.txt
  $zip.Entries | Where-Object { $_.FullName -match '[^\x00-\x7F]' }       # 期望无输出
  $zip.Dispose()
  ```
- 出处：Android 构建验证记录。
- 成功判定：`assets/GameGuide.txt` 存在，非 ASCII 过滤无输出。

### C11 签名校验

- 用途：判断产物使用的签名证书。
- 定位工具：`Get-ChildItem -Path "$editorRoot\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\*\apksigner.bat"`；本机命中 `33.0.2` 与 `34.0.0` 两套。
- 命令：`& '<apksigner.bat 完整路径>' verify --print-certs '<APK 路径>'`
- 出处：Android 构建指南；Android 构建验证记录给出同一条命令。
- 成功判定：能打印 `Signer #1 certificate DN`。签名显示 `CN=Android Debug` 时，产物用的是 Android 调试证书；配置侧证据是工程 Player 设置的 `androidUseCustomKeystore` 为 0。

### C12 EditMode 测试

- 用途：跑现有 52 个 EditMode 用例。
- 命令：
  ```powershell
  & "D:\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe" `
    -runTests -batchmode `
    -projectPath "D:\Unity\Projects\My_ARPG" `
    -testPlatform EditMode `
    -testResults "D:\Unity\Projects\My_ARPG\Logs\test-editmode.xml" `
    -logFile "D:\Unity\Projects\My_ARPG\Logs\test-editmode.log"
  ```
- 出处：项目现状文档，参数逐字保留。
- 成功判定：看结果 XML 根节点的 `result` 与失败计数；退出码不可靠。
- 前提：没有编辑器实例开着这个工程。
- 本机状态：日志目录下只有资产导入工作进程日志、包更新日志、着色器编译日志与一份启动菜单场景备份，没有这条命令的结果 XML 与日志文件，因此它当前没有跑通的证据。

## 4. 进程与产物处理

- **C12 会起一个 Unity 批处理实例**：执行期间与执行之前，打开该工程的编辑器实例都必须是 0 个；第二个实例不并发启动。
- **C5 与 C6 带机器级副作用**：它们改 Editor 安装目录里的 `sdkmanager.bat`、用户级 Gradle 全局配置与用户环境变量，影响使用这些 Editor 的所有项目。安装新 Editor 或 Unity Hub 修复覆盖 Editor 之后重跑一次。执行前取得独占，与其它构建或代理操作串行。
- **只读命令可重复**：C1–C4、C7、C9–C11。
- **产物位置**：`Builds\Android`；本机 `Builds\` 下只有这一个子目录。忽略规则排除 `Builds\`，产物不入库。
- **日志位置**：C12 的 `-testResults` 与 `-logFile` 参数指向 `Logs\`，结果文件名 `test-editmode.xml`、日志文件名 `test-editmode.log`；忽略规则排除 `Logs\`，日志不入库。
- **无 CI**：仓库内没有 CI 配置目录，C9–C12 的结果只存在于本机。
- **验证后留档**：构建验证结果按模板追加到 Android 构建验证记录。

## 5. 留白待用户确认的项

本节各条在用户给出结论之前保持空白，不写入任何内容。

1. **Android Player 打包命令：仓库中不存在。**
   对全仓的文档、脚本与配置文件检索 `-buildTarget`、`-executeMethod`、`BuildPipeline.`、`BuildPlayerOptions`，无任何命中；唯一命中的是 C12 的 `-runTests -testPlatform`。Android 构建指南对这一环只给出一句人工步骤：「最后在 Unity 中做一次非 Development Android 构建」。
2. **构建输出契约**：APK 还是 AAB、Development 还是 Release、输出目录、文件名规则。用户级构建设置由 Unity 写进被忽略的 `UserSettings\`，仓库里没有这份设置文件，无法从仓库判定。
3. **测试通过判据**：C12 的退出码不可靠，读结果 XML 时看哪个节点、用什么阈值，以及固定的结果文件路径，都需确认。
4. **PlayMode 验证命令**：仓库中 0 个 `[UnityTest]`，见 `..\Build\TestBaseline_Guide.md` §2.2 与 §2.7；后续是继续纯手工，还是补测试。
5. **真机冒烟是否可以自动化**：Android 构建指南描述的是人工三步——启动、场景切换、存档读写，没有命令形态。
6. **Addressables 内容是否需在打包前构建**：Addressables 配置一组资产齐备，产品代码零调用，是否需要构建内容未确认。
7. **路径无关性要求**：C12 把工程根与 Editor 路径写成绝对路径，是否要求改写成路径无关形式。
8. **代理地址与端口**：Android 构建指南与代理配置脚本默认 `127.0.0.1:7890`，实际值按机器确认；本机用户环境变量为 `127.0.0.1` 与 `7890`。

## 6. 环境变量

| 名称 | 作用 | 出处 | 本机实测 |
|---|---|---|---|
| `UNITY_ANDROID_PROXY_HOST` | 补丁后的 sdkmanager 脚本读取的代理地址；变量未定义时回退到安装时写入的值 | 由代理配置脚本写入与删除，被补丁后的 sdkmanager 脚本消费 | `127.0.0.1` |
| `UNITY_ANDROID_PROXY_PORT` | 同上，端口 | 同上 | `7890` |
| `%USERPROFILE%\.gradle\gradle.properties`，属环境面 | Gradle 的 `systemProp.http/https.proxyHost/proxyPort` 与 `nonProxyHosts` | 由代理配置脚本写入，作用域为用户级 | 存在，含六行 `systemProp.*`，无脚本标记 |
| `GRADLE_USER_HOME` | 设置后，用户级 Gradle 配置位置随之改变 | Android 构建指南指向 Gradle 官方文档 | 未设置 |
| `%APPDATA%\UnityHub\secondaryInstallPath.json`，属环境面 | Editor 安装根目录的权威来源，C2 依赖它 | Android 构建指南；代理配置脚本读取同一来源 | 可读出 `D:\Unity\Editor` |

补充事实：工程内没有项目级的环境变量文件，也没有 .NET 构建属性文件，因此没有项目级的环境注入点；环境相关状态来自用户环境或 Editor 安装目录。

## 7. 已知缺陷与风险

- **D1 打包是本指南最大的空洞**：§5 第 1 项是仓库内确实不存在该命令；照抄外部资料的打包命令会编造出看起来权威的流程，风险是把构建参数写错。
- **D2 C12 会误报通过**：结果判据是人读结果 XML，退出码不可靠；把它接进脚本只看退出码时，失败会被报成通过。
- **D3 C12 没有跑通证据**：日志目录下没有结果 XML，「52 个用例能被枚举并跑绿」缺少机器可查的证据。
- **D4 C5 与 C6 的爆炸半径**：它们改 Editor 安装目录与用户级配置。本机 `sdkmanager.bat` 处于 `Patched` 状态，而 Gradle 全局配置里的代理属性由非脚本方式写入；`Remove` 只清理带脚本标记的块，这部分属性会留下。
- **D5 验证证据不入库**：产物目录与日志目录由忽略规则排除，仓库内没有 CI 配置，验证结论在仓库内无法被审计。
- **D6 验证结论绑定执行时刻的工作区内容**：仓库基线只反映已提交状态，比对结论时要记下当时的提交与工作区改动。

## 8. 未核验事项

- 假设：C12 在当前工作区能跑通并产出一份 `result` 为通过的 XML；运行期行为待编辑器实测。
- 假设：无 Unity 编辑器实例打开该工程这一前提在自动化环境中能被可靠判定；判定方式待验证。
- 假设：C11 的两套 build-tools 都能校验本工程产物；实际执行 verify 待实测。
- 假设：C8 在当前本机代理配置下能完成 `--list`；当前读到的证据是 `sdkmanager.bat` 的补丁内容与 `State = Patched`，实跑待执行。
- 假设：`GRADLE_USER_HOME` 未设置时 Gradle 读用户级全局配置；依据 Android 构建指南的说明，实地验证待做。
- 假设：真机冒烟只有 Android 构建指南描述的三步人工流程，且没有可自动化的替代路径；该假设待验证。
