# Unity Android 构建指南（Windows）

> **本文不绑定任何一台机器。** 文中出现的 `<代理地址>`、`<Editor 安装根目录>`、`<sdkmanager.bat 的完整路径>`
> 都是占位符，`127.0.0.1` / `7890` 只是脚本的默认值。请先按「第一步：确认环境」的命令读出你自己机器上的
> 实际值，再照做。
> 本文只处理两类问题：构建前的代理配置，以及 Release 构建在 AAR 解包阶段的失败。

## 适用范围

Windows 上的 Unity Android 构建，重点处理：

- Unity 长时间停在 `Detecting Android SDK` / `Checking Android SDK and components`。
- Release 构建在 `lintVitalAnalyzeRelease`、`ExtractAarTransform` 或 `AarExtractor` 阶段失败。

## 前置条件

| 项 | 要求 | 怎么确认 |
| --- | --- | --- |
| 系统 | Windows 10 / 11 | — |
| Unity | 用 Unity Hub 安装，且带 Android Build Support（SDK / NDK / OpenJDK） | `Get-Content .\ProjectSettings\ProjectVersion.txt` |
| PowerShell | 5.1 及以上（仓库脚本开头是 `#Requires -Version 5.1`） | `$PSVersionTable.PSVersion` |
| 代理 | **只在你的网络无法直连 Google / Maven 仓库时才需要** | 见「手动验证」 |

本工程配套脚本：

```powershell
.\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1
```

## 第一步：确认环境（先读，再照抄）

后面的命令都依赖三个位置。**不要照抄本文示例，先在本机读出来。**

```powershell
# 1) 本工程使用的 Unity 版本
Get-Content .\ProjectSettings\ProjectVersion.txt

# 2) Unity Hub 记录的 Editor 安装根目录（每台机器不同，Hub 配置里就有）
$editorRoot = Get-Content -Raw (Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json') | ConvertFrom-Json

# 3) Editor 内置的 sdkmanager.bat（cmdline-tools 的版本号按实际目录取）
Get-ChildItem -Path "$editorRoot\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\cmdline-tools\*\bin\sdkmanager.bat"
```

用户级 Gradle 配置的位置与用户名无关，一律写成：

```text
%USERPROFILE%\.gradle\gradle.properties
```

如果第 3 步探测不到 `sdkmanager.bat`，说明你的 Editor 不在 Hub 记录的位置，
用脚本参数显式指定根目录（见「一键全局配置」的 `-EditorRoot`）。

## 已确认的根因

### 根因一：SDK 检测卡住（代理没有被继承）

Unity 2022.3 使用 Editor 自带的 Android SDK、NDK、OpenJDK 和 Gradle。SDK 检测会调用内置
`sdkmanager.bat --list`。Java / sdkmanager **不会自动继承 Windows 用户代理**，所以只要你的网络需要经
代理才能访问 Google 仓库，这个请求就会长时间等待。

`sdkmanager` 官方支持以下参数（`<代理地址>` / `<代理端口>` 换成你自己代理软件的实际监听值）：

```text
--proxy=http
--proxy_host=<代理地址>
--proxy_port=<代理端口>
```

Gradle 则通过用户目录下的 `%USERPROFILE%\.gradle\gradle.properties` 读取全局系统代理属性。

### 根因二：Release lint 解包 AAR 失败（非 ASCII 文件名）

本工程原来包含：

```text
Assets/StreamingAssets/游戏指南.txt
```

Windows 中文代码页下生成的 `out.aar` 将这个 ZIP 条目写成了不可被 Android Gradle Plugin
稳定解码的文件名。完整堆栈最终落在：

```text
com.android.builder.aar.AarExtractor
java.nio.charset.MalformedInputException
malformed input off : 8, length : 1
```

修复方式是把文件名改为 ASCII：

```text
Assets/StreamingAssets/GameGuide.txt
```

文件内容仍可使用 UTF-8 中文。不要通过关闭 `lintVital` 掩盖这个 AAR 结构问题。

## 一键全局配置

从项目根目录执行（`<代理地址>` / `<代理端口>` 按实际填写）：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File `
  .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 `
  -Action Install `
  -ProxyHost <代理地址> `
  -ProxyPort <代理端口>
```

- 只装了 Windows PowerShell 5.1、没有 PowerShell 7 的机器，把 `pwsh -NoProfile` 换成 `powershell`。
- 脚本参数的**默认值**是 `-ProxyHost 127.0.0.1`、`-ProxyPort 7890`（Clash 的常见默认端口）。
  如果你的代理软件监听别的端口（例如 V2Ray 常见 10809），必须显式传参，不要依赖默认值。

脚本是幂等的，可重复执行。它会：

1. 扫描 Unity Hub 记录的 Editor 安装根目录、常见 Unity Hub / Program Files 路径、正在运行的 Unity 进程和注册表安装项。
2. 修改每个已安装 Editor 的内置 `sdkmanager.bat`。
3. 为每个原始批处理文件建立 `.unity-android-proxy.original` 备份。
4. 写入用户级 `%USERPROFILE%\.gradle\gradle.properties`。
5. 写入用户环境变量 `UNITY_ANDROID_PROXY_HOST` 和 `UNITY_ANDROID_PROXY_PORT`。

脚本只删除自己带标记的 Gradle 配置块，已有的其他 `gradle.properties` 设置会保留。Unity Hub
覆盖或修复过 Editor 后再次执行 `Install`，脚本会以新的未修改批处理文件刷新备份，避免卸载时
恢复旧版本工具。

这会覆盖使用这些 Unity Editor 的所有项目。安装新 Editor 或升级/修复 Editor 后，需要重新执行一次，
因为 Unity Hub 可能覆盖内置 Android 工具。

自定义 Unity 安装路径（`<Editor 安装根目录>` 就是「第一步」读出的 `$editorRoot`）：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File `
  .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 `
  -Action Install `
  -EditorRoot '<Editor 安装根目录>' `
  -ProxyHost <代理地址> `
  -ProxyPort <代理端口>
```

## 查看状态

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File `
  .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 `
  -Action Status
```

应看到：

- 结果表里每个 `sdkmanager.bat` 的 `State` 为 `Patched`；**表格同时打印 `Path`，下一步直接拿它用**。
- `SdkManagersFound` 大于 0。若为 0，用 `-EditorRoot` 指定根目录后重试。
- `ProxyReachable` 为 `True`，前提是代理软件正在运行。
- `GradleManaged` 只有**刚执行过 `Install`** 的机器上才为 `True`。`%USERPROFILE%\.gradle\gradle.properties`
  里的代理属性也可能由其他工具或手工写入而不带脚本标记，此时 `GradleManaged` 为 `False` 但代理照样生效
  —— 脚本的 `Remove` 只删自己带标记的块，不会动这些条目。

## 手动验证

```powershell
# 1) 代理端口是否真的有监听（换成你自己的地址和端口）
Test-NetConnection <代理地址> -Port <代理端口>

# 2) 用 Status 输出里的 Path 运行内置 SDK Manager
& '<sdkmanager.bat 的完整路径>' --list
```

最后在 Unity 中做一次非 Development Android 构建。只运行 `sdkmanager --version` 不会访问远程仓库，
不能证明网络路径已修好。

## 卸载配置

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File `
  .\Tools\UnityAndroid\Configure-UnityAndroidProxy.ps1 `
  -Action Remove
```

卸载会：

- 从用户级 Gradle 配置中删除脚本管理的代理块。
- 删除两个用户环境变量。
- 从备份恢复每个 `sdkmanager.bat`。

## 代理软件注意事项

- 代理软件必须实际监听脚本配置的地址和端口。
- Unity 已经启动时，用户环境变量不会重新注入现有进程；批处理中的默认值仍可立即生效。
- 切换代理端口后重新执行 `Install`，并重启 Unity。
- 若使用 TUN/系统级透明代理，理论上不必修改 `sdkmanager.bat`，但仍应验证 Java 进程确实能访问仓库。

## Release 构建排查清单

遇到 `ExtractAarTransform` 或 `malformed input`：

1. 使用 `--stacktrace` 重跑失败的 Gradle 任务。
2. 用 7-Zip 或 ZIP API 测试 `out.aar` 是否损坏。
3. 列出 AAR 内非 ASCII 文件名，尤其检查 `StreamingAssets`。
4. 将构建输入中的文件名改成 ASCII，文件内容统一 UTF-8。
5. 清理并重新生成 Gradle 工程，再做完整 Release 构建。
6. 保留 lint；只有确认是第三方 lint 规则误报时才考虑按规则定点禁用。

## 构建后自检清单

每次 Release 构建完成后逐项确认，**不要用「进度条走完了」代替验证**：

1. **Unity Console 无 error**，且 `Build completed` 与实际产物一致。
2. **产物存在且非空**：`Builds/Android/` 下按 Player Settings 的 Product Name 生成 `.apk`（或 `.aab`）。
3. **包内条目名全是 ASCII**：用任意 ZIP 工具打开产物，确认 `assets/` 下没有中文等非 ASCII 条目
   （本工程对应 `assets/GameGuide.txt`）。
4. **签名不是 Android Debug**：Editor 内置 build-tools 里带 `apksigner`，可这样定位并校验：

   ```powershell
   Get-ChildItem -Path "$editorRoot\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\*\apksigner.bat"
   & '<apksigner.bat 的完整路径>' verify --print-certs '<apk 路径>'
   ```

   输出里 `Signer #1 certificate DN` 若仍是 `CN=Android Debug`，说明还没配项目自己的 keystore
   （`Project Settings > Player > Publishing Settings`）。
5. **装机冒烟**：安装到真机后至少跑通「启动场景 → Addressables 加载 → 存档读写」三条路径。
6. **留一条记录**：把你机器上的实际值（日期、Unity 版本、Editor 路径、产物路径、自检结果）追加到
   [`UnityAndroidBuildVerification.md`](UnityAndroidBuildVerification.md)，供下次排查对照。

## 相关文件

- 构建验证记录：[`UnityAndroidBuildVerification.md`](UnityAndroidBuildVerification.md)
- 可复用的排查 skill 草案：[`UnityAndroidBuildDiagnostics.skill.md`](UnityAndroidBuildDiagnostics.skill.md)
- 配套脚本：[`../Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1`](../Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1)

## 资料依据

排查时先用完整错误关键字检索社区，再回到官方文档确认参数和配置作用域：

- Android SDK Manager 官方参数：
  `https://developer.android.com/tools/sdkmanager`
- Gradle 用户级配置与 `GRADLE_USER_HOME`：
  `https://docs.gradle.org/current/userguide/build_environment.html`
- Unity 2022.3 Android SDK/NDK/OpenJDK 环境说明：
  `https://docs.unity3d.com/2022.3/Documentation/Manual/android-sdksetup.html`
- 与本工程错误完全一致的社区案例，根因同样是 `StreamingAssets` 中的中文文件名：
  `https://bluebirdofoz.hatenablog.com/entry/2024/12/10/024645`
- Unity Discussions 中关于 `StreamingAssets` 外文字符导致构建失败的讨论：
  `https://discussions.unity.com/t/build-failure-from-a-foreign-character-in-the-streaming-assets-folder/807255`
