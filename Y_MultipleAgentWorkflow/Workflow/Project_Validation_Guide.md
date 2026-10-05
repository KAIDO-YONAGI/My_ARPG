# 项目验证指南

文档 ID：`WF-PROJECT-VALIDATION`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责“本工程在仓库里真的能查到出处的验证命令与流程”——环境检查、命令清单（逐条标注文件:行）、进程与产物处理、环境变量，以及**明确标为空白、等用户确认的项**。不负责发明任何构建/测试命令，不负责解释配置项语义（见 `..\Build\ProjectConfig_Guide.md`），不负责 Android 故障根因详解（见 `..\Build\AndroidBuild_Guide.md`）。
上游来源：`Docs/UnityAndroidBuildGuide.md`、`Docs/UnityAndroidBuildVerification.md`、`Docs/My_ARPG_MVCS项目现状.md:198-209`、`Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1`、`ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`、`.gitignore`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `怎么验证` / `验证流程` | §3 命令清单 |
| `跑测试` / `EditMode 测试` / `testResults` | §3 C12、§5 |
| `代理状态` / `sdkmanager 代理` | §3 C4–C8 |
| `产物在哪` / `apk 检查` | §3 C9–C10、§4 |
| `签名校验` / `apksigner` | §3 C11 |
| `打包命令是什么` | §5（空缺，勿填） |
| `环境变量` / `gradle.properties` | §6 |
| `进程能不能停` / `能不能并发跑` | §4 |

## 2. 前置环境检查

按顺序确认，任一条不成立就不要继续下层命令：

1. **没有任何 Unity 编辑器实例打开本工程** —— 否则 headless 测试的第二个实例起不来（`Docs/My_ARPG_MVCS项目现状.md:209`）。
2. **Editor 二进制存在**。文档指定的路径为 `D:\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe`（`Docs/My_ARPG_MVCS项目现状.md:201`）；本机实测该路径存在，版本与 `ProjectSettings/ProjectVersion.txt:1` 的 `2022.3.62f3c1` 一致。
3. **PowerShell 不低于 5.1** —— 脚本首行 `#Requires -Version 5.1`（`Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:1`）；`Docs/UnityAndroidBuildGuide.md:21` 把这条列为前置条件。只有 PowerShell 5.1 的机器把 `pwsh -NoProfile` 换成 `powershell`（`Docs/UnityAndroidBuildGuide.md:109`）。
4. **需要走代理时，代理软件确实在该地址/端口监听**（`Docs/UnityAndroidBuildGuide.md:185`）。
5. **路径前提**：C12 的命令把项目根与 Editor 路径写死为绝对路径（`Docs/My_ARPG_MVCS项目现状.md:201,203`），换机器即失效。
6. **构建产物检查的前提**是产物已存在（§3 C9）。

## 3. 命令清单

> 全部命令的出处都在仓库内。未在此列出、且仓库内查不到出处的“打包命令”一律不得使用（§5）。

### C1 读 Editor 版本

- 用途：确认打开/构建该工程所需的 Editor 版本。
- 命令：`Get-Content .\ProjectSettings\ProjectVersion.txt`
- 出处：`Docs/UnityAndroidBuildGuide.md:36`
- 成功判定：输出 `m_EditorVersion: 2022.3.62f3c1`（值出处 `ProjectSettings/ProjectVersion.txt:1`）。

### C2 定位 Editor 安装根目录

- 用途：拿到本机的 `$editorRoot`，供 C3/C11 使用。**不要照抄任何示例路径。**
- 命令：`$editorRoot = Get-Content -Raw (Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json') | ConvertFrom-Json`
- 出处：`Docs/UnityAndroidBuildGuide.md:39`；同一来源也被脚本读取（`Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:71-77`）
- 成功判定：得到非空路径字符串。本机实测为 `D:\Unity\Editor`。

### C3 定位 Editor 内置 `sdkmanager.bat`

- 用途：确认 Android 工具链位置；C8 需要它的完整路径。
- 命令：`Get-ChildItem -Path "$editorRoot\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\cmdline-tools\*\bin\sdkmanager.bat"`
- 出处：`Docs/UnityAndroidBuildGuide.md:42`；脚本同源 glob 见 `Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:131`
- 成功判定：至少 1 条命中。本机实测 1 条（`...\cmdline-tools\6.0\bin\sdkmanager.bat`），并被 C4 的 `SdkManagersFound = 1` 佐证。探测不到时用 C5b 的 `-EditorRoot`（`Docs/UnityAndroidBuildGuide.md:51-52`）。

### C4 查询 Android 代理配置状态（只读）

- 用途：看内置 sdkmanager 是否已被打补丁、Gradle 配置与环境变量现状。
- 命令：`pwsh -NoProfile -ExecutionPolicy Bypass -File .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 -Action Status`
- 出处：`Docs/UnityAndroidBuildGuide.md:142-145`；脚本分支 `Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:385-389`，输出字段 `:399-413`
- 成功判定：结果表每行 `State` 为 `Patched`、`SdkManagersFound > 0`、代理在跑时 `ProxyReachable = True`。**`GradleManaged = False` 不代表代理没生效**（`Docs/UnityAndroidBuildGuide.md:152-154`；本机实测即为此种情形：`gradle.properties` 含六行 `systemProp.*` 但无脚本标记）。

### C5 安装 Android 代理配置（机器级副作用，需独占）

- 用途：让 Editor 内置 sdkmanager 与 Gradle 走代理，解决 `Detecting Android SDK` 卡住。
- 命令：`pwsh -NoProfile -ExecutionPolicy Bypass -File .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 -Action Install -ProxyHost <代理地址> -ProxyPort <代理端口>`
- 出处：`Docs/UnityAndroidBuildGuide.md:102-107`；脚本分支与副作用 `Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:363-374`、`:187-213`、`:285-308`、`:320-323`
- 成功判定：重跑 C4 得 `State = Patched`。脚本幂等，可重复执行（`Docs/UnityAndroidBuildGuide.md:113`；实现见脚本 `:141-162`、`:182-185`）。
- 变体 **C5b**：自定义 Unity 安装路径加 `-EditorRoot '<Editor 安装根目录>'`（`Docs/UnityAndroidBuildGuide.md:130-137`）。
- 变体 **C5c**：仅 PowerShell 5.1 的机器把 `pwsh -NoProfile` 换成 `powershell`（`Docs/UnityAndroidBuildGuide.md:109`）。

### C6 卸载 Android 代理配置

- 用途：还原 `sdkmanager.bat`、删掉脚本托管的 Gradle 块与两个用户环境变量。
- 命令：`pwsh -NoProfile -ExecutionPolicy Bypass -File .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 -Action Remove`
- 出处：`Docs/UnityAndroidBuildGuide.md:172-175`；脚本 `Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:376-383`、`:222-249`、`:303-308`、`:326-329`
- 成功判定：C4 显示 `NotPatched` 或备份已恢复；用户环境变量变为空。注意 `Remove` 只删带脚本标记的块（`Docs/UnityAndroidBuildGuide.md:152-154`）。

### C7 代理端口连通性

- 用途：确认代理软件确实在监听。
- 命令：`Test-NetConnection <代理地址> -Port <代理端口>`
- 出处：`Docs/UnityAndroidBuildGuide.md:160`
- 成功判定：`TcpTestSucceeded : True`。

### C8 sdkmanager 真实网络路径验证

- 用途：证明 Java/仓库网络路径真的修好了。
- 命令：`& '<C3 得到的 sdkmanager.bat 完整路径>' --list`
- 出处：`Docs/UnityAndroidBuildGuide.md:163`
- 成功判定：能列出远端包而不长时间等待。
- **硬边界**：只跑 `sdkmanager --version` 不会访问远程仓库，不能证明网络路径已修好（`Docs/UnityAndroidBuildGuide.md:166-167`）。

### C9 构建产物存在性（只读）

- 用途：确认构建真的产出了东西。
- 命令：`Get-ChildItem Builds\Android`
- 出处：`Docs/UnityAndroidBuildVerification.md:43`
- 成功判定：存在 `My_ARPG-release.apk`。本机实测 28,509,358 字节（2026-08-09 18:06:23），与 `Docs/UnityAndroidBuildVerification.md:32` 一致；同目录另有 `My_ARPG.apk`（45,437,354 字节）。

### C10 包内条目与 ASCII 检查（只读，无需 7-Zip）

- 用途：确认 `StreamingAssets` 里没有把非 ASCII 名写进 AAR/APK。
- 命令：
  ```powershell
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path 'Builds\Android\My_ARPG-release.apk'))
  $zip.Entries | Where-Object { $_.FullName -like 'assets/*' }        # 期望含 assets/GameGuide.txt
  $zip.Entries | Where-Object { $_.FullName -match '[^\x00-\x7F]' }   # 期望无输出
  $zip.Dispose()
  ```
- 出处：`Docs/UnityAndroidBuildVerification.md:47-51`
- 成功判定：`assets/GameGuide.txt` 存在，且非 ASCII 过滤无输出（`Docs/UnityAndroidBuildVerification.md:56-58`）。

### C11 签名校验

- 用途：判断产物是否还是 Android Debug 证书。
- 定位工具：`Get-ChildItem -Path "$editorRoot\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\*\apksigner.bat"`（出处 `Docs/UnityAndroidBuildGuide.md:212`；本机实测命中 `33.0.2`、`34.0.0` 两套）
- 命令：`& '<apksigner.bat 的完整路径>' verify --print-certs '<apk 路径>'`
- 出处：`Docs/UnityAndroidBuildGuide.md:213`；同样命令亦见 `Docs/UnityAndroidBuildVerification.md:44`
- 成功判定：能打印 `Signer #1 certificate DN`。若仍是 `CN=Android Debug`，说明没配项目自己的 keystore（`Docs/UnityAndroidBuildGuide.md:216-217`；配置侧证据 `ProjectSettings/ProjectSettings.asset:283` `androidUseCustomKeystore: 0`）。

### C12 EditMode 测试（本机命令行）

- 用途：跑现有 52 个 EditMode 用例。
- 命令：
  ```powershell
  & "D:\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe" `
    -runTests -batchmode `
    -projectPath "D:\Unity\Projects\My_ARPG" `
    -testPlatform EditMode `
    -testResults "D:\Unity\Projects\My_ARPG\Logs\test-editmode.xml" `
    -logFile      "D:\Unity\Projects\My_ARPG\Logs\test-editmode.log"
  ```
- 出处：`Docs/My_ARPG_MVCS项目现状.md:200-207`（逐字照抄，未改动任何参数）
- 成功判定：**看 `Logs/test-editmode.xml` 根节点的 `result` 与失败计数，退出码不可靠**（`Docs/My_ARPG_MVCS项目现状.md:209`）。
- 前提：编辑器没有开着这个工程（同处 `:209`）。
- 本机状态：`Unity.exe` 路径存在；但 `Logs/` 下**没有** `test-editmode.xml` / `.log`（实测只有 `AssetImportWorker*.log`、`Packages-Update.log`、`shadercompiler-*.log`、`StartingMenu.unity.20260912_185123.bak`），即该命令在本轮**未被验证跑通**。

## 4. 进程与产物处理

- **C12 会起一个 Unity 批处理实例**：执行期间及之前都不能有别的编辑器实例打开该工程（`Docs/My_ARPG_MVCS项目现状.md:209`）。不要并发起第二个实例。
- **C5/C6 是机器级副作用，不是项目级操作**：它们改 Editor 安装目录里的 `sdkmanager.bat`（`Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:187-213`）、用户级 `%USERPROFILE%\.gradle\gradle.properties`（`:285-308`）、用户环境变量（`:320-329`），会影响使用这些 Editor 的**所有项目**（`Docs/UnityAndroidBuildGuide.md:125`）。安装新 Editor 或 Unity Hub 修复覆盖 Editor 后需重跑（`:125-126`）。**执行前应取得独占，不要与其他构建/代理操作并发。**
- **只读命令可重复**：C1–C4、C7、C9–C11。
- **产物位置**：`Builds/Android/`（本机实测，`Builds/` 下只有 `Android/` 一个子目录）。`Builds/` 被忽略（`.gitignore:6`），产物不入库。
- **日志位置**：由 C12 的 `-testResults` / `-logFile` 参数指定为 `Logs/test-editmode.xml` 与 `.log`；`Logs/` 被忽略（`.gitignore:7`），日志不入库。
- **无 CI**：`.github/` 不存在（本机实测），因此 C9–C12 的结果只存在于本机，没有远端留痕。
- **验证后留档**：构建验证结果按模板追加到 `Docs/UnityAndroidBuildVerification.md`（`Docs/UnityAndroidBuildGuide.md:219-220`、`Docs/UnityAndroidBuildVerification.md:60-88`）。

## 5. 待用户确认的空缺项（**保持空白，不要填内容**）

1. **Android Player 打包命令：仓库中不存在。**
   已对 `*.md / *.ps1 / *.bat / *.json / *.cs / *.targets / *.yml` 全仓检索 `-buildTarget`、`-executeMethod`、`BuildPipeline.`、`BuildPlayerOptions`，**无任何命中**；唯一命中的是 C12 的 `-runTests -testPlatform`（`Docs/My_ARPG_MVCS项目现状.md:202,204`）。旧文档对这一环只有一句人话：“最后在 Unity 中做一次非 Development Android 构建”（`Docs/UnityAndroidBuildGuide.md:166`）。→ 在用户给出命令之前，本节不得写入任何内容。
2. **构建输出契约**：`.apk` 还是 `.aab`、Development 还是 Release、输出目录、文件名规则。仓库无 `UserSettings/EditorUserBuildSettings.asset` 且 `UserSettings/` 被忽略（`.gitignore:9`），无法从仓库判定。
3. **测试通过判据**：C12 的自述退出码不可靠，读 XML 的具体判定规则（哪个节点、什么阈值）与固定的结果文件路径需确认。
4. **PlayMode 验证命令**：仓库中 0 个 `[UnityTest]`（见 `..\Build\TestBaseline_Guide.md` §2.2、§2.7），是继续纯手工还是要补测试。
5. **真机冒烟是否可以自动化**：`Docs/UnityAndroidBuildGuide.md:218` 描述的是人工三步（启动 → 场景切换 → 存档读写），没有命令形态。
6. **Addressables 内容是否需在打包前构建**：`Assets/AddressableAssetsData/` 配置齐全但产品代码零调用，是否需要构建内容未确认。
7. **路径无关性要求**：C12 硬编码 `D:\Unity\Projects\My_ARPG` 与 Editor 绝对路径，是否要求改写成路径无关形式。
8. **代理地址/端口**：文档与脚本默认 `127.0.0.1:7890`（`Docs/UnityAndroidBuildGuide.md:110`、脚本 `:8-11`），实际值需按机器确认；本机实测用户环境变量为 `127.0.0.1 / 7890`。

## 6. 环境变量

| 名称 | 作用 | 出处 | 本机实测 |
|---|---|---|---|
| `UNITY_ANDROID_PROXY_HOST` | sdkmanager.bat 补丁读取的代理地址（`if not defined` 回退到安装时的值） | 写入 `Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:320-321`，删除 `:326-329`；消费点见被补丁的 `sdkmanager.bat:69` | `127.0.0.1` |
| `UNITY_ANDROID_PROXY_PORT` | 同上，端口 | 同上（`sdkmanager.bat:70`） | `7890` |
| `%USERPROFILE%\.gradle\gradle.properties`（非环境变量，同属环境面） | Gradle 全局 `systemProp.http/https.proxyHost/proxyPort` + `nonProxyHosts` | `Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1:26`、`:285-293`；作用域说明 `Docs/UnityAndroidBuildGuide.md:70` | 存在，含六行 `systemProp.*`，无脚本标记 |
| `GRADLE_USER_HOME` | 若设置，用户级 Gradle 配置位置随之改变 | `Docs/UnityAndroidBuildGuide.md:234`（指向 Gradle 官方文档） | 未设置（空） |
| `%APPDATA%\UnityHub\secondaryInstallPath.json`（非环境变量，属环境面） | Editor 安装根目录的权威来源，C2 依赖它 | `Docs/UnityAndroidBuildGuide.md:39`；脚本 `:71-77` | 可读出 `D:\Unity\Editor` |

补充事实：项目内**不存在**环境变量/属性文件——根目录无 `.env`、无 `Directory.Build.props`（本机实测），因此没有项目级的环境注入点，所有环境相关状态要么来自用户环境、要么来自 Editor 安装目录。

## 7. 已知缺陷与风险

- **D1（本指南最大的空洞是打包）**：§5 第 1 项不是“待补充文档”，而是仓库内**确实不存在**该命令；任何照抄外部资料的打包命令都属于编造，风险是把构建参数写错却看起来像权威流程。
- **D2（C12 会误报通过）**：文档明说退出码不可靠（`Docs/My_ARPG_MVCS项目现状.md:209`），而唯一的结果判据是人读 XML。若有人把它接进脚本只看 `$LASTEXITCODE`，失败会被报成通过。
- **D3（C12 从未被验证跑通）**：`Logs/` 下无 `test-editmode.xml`（本机实测），因此“52 个用例能被枚举并跑绿”目前没有机器可查的证据。
- **D4（C5/C6 的爆炸半径被低估）**：它们改的是 Editor 安装目录与用户级配置（`Docs/UnityAndroidBuildGuide.md:125`），本机实测 `sdkmanager.bat` 已处于 `Patched` 状态，且 `gradle.properties` 里有**非脚本写入**的代理属性 —— 后续任何 `Remove` 都不会清掉后者。
- **D5（验证证据不入库）**：`Builds/`、`Logs/` 被忽略（`.gitignore:6-7`），`.github/` 不存在，验证结论无法在仓库内被审计。
- **D6（工作区相对 HEAD 是脏的）**：`git status --porcelain` 显示 `Packages/manifest.json`、`Packages/packages-lock.json`、`AStarPathFinder.cs`、`SceneChanger.cs`、`PlayerDamageController.cs`、`SaveDataManager.cs`、两个场景与 `README*.md` 已改，`AStarOpenHeap.cs` / `AStarOpenHeapTests.cs` 未跟踪 → 现在跑出的结果对应的是工作区，不是 HEAD。

## 8. 未核验事项

- 假设：C12 在当前工作区仍能跑通并产出一份 `result` 为通过的 XML（未运行 Unity 验证）。
- 假设：无 Unity 编辑器实例打开该工程这一前提在自动化环境中能被可靠判定（未验证）。
- 假设：C11 的两套 build-tools 都能校验本工程产物（未实际执行 verify）。
- 假设：C8 在当前本机代理配置下能完成 `--list`（未执行，只读了 `sdkmanager.bat` 的补丁内容与 `State = Patched`）。
- 假设：`GRADLE_USER_HOME` 未设置时 Gradle 一定读 `%USERPROFILE%\.gradle\gradle.properties`（未实地验证，依据 `Docs/UnityAndroidBuildGuide.md:70,234`）。
- 假设：真机冒烟（`Docs/UnityAndroidBuildGuide.md:218`）没有可自动化的替代路径（未验证）。
