# Android 构建与发布（Windows）

文档 ID：`BUILD-ANDROID-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
权威范围：只负责本工程 Windows 上 Android 构建的环境自检、两类已知故障的根因与幂等修复、构建产物位置与签名现状。Player/Project 配置项字典见 `ProjectConfig_Guide.md`，测试基线见 `TestBaseline_Guide.md`。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `Android 构建` / `打包 APK` | §2.1、§2.4 产物位置 |
| `Detecting Android SDK` 卡住 / `Checking Android SDK and components` | §2.2 |
| `lintVitalAnalyzeRelease` | §3.1 |
| `ExtractAarTransform` / `AarExtractor` | §2.3 |
| `MalformedInputException` / `malformed input off` | §2.3 |
| `StreamingAssets 中文名` | §2.3 |
| `sdkmanager 代理` / `gradle.properties` / `UNITY_ANDROID_PROXY_*` | §2.2、§2.6 环境自检 |
| `keystore` / `签名` / `apksigner verify` | §2.5 签名现状、§2.7 签名校验、§3.6 |
| `Release 构建失败排查` | §2.3、§3.1 |
| `构建产物在哪` | §2.4 |

## 2. 当前实现

### 2.1 唯一的 Android 工具链入口

`Configure-UnityAndroidProxy` 是本域唯一的可执行工具，要求 PowerShell 5.1 以上。

- 参数面：`-Action Install|Status|Remove`，默认 `Install`；`-ProxyHost` 默认 `127.0.0.1`；`-ProxyPort` 默认 `7890`，校验 1..65535；`-EditorRoot string[]`；`-SkipNetworkCheck`。
- Editor 根目录发现顺序：显式 `-EditorRoot` → Unity Hub 的次要安装路径记录 → 程序目录下的 Unity Hub 与 Editor 路径 → 正在运行的 Unity 进程 → 注册表卸载项。
- 目标文件：Editor 安装目录下 AndroidPlayer 的 cmdline-tools 通配路径中的 sdkmanager 批处理。
- 三个 Action 的分派：`Install`、`Remove`、`Status`；汇总对象字段为 `Action / Proxy / ProxyReachable / GradleProperties / GradleManaged / UserProxyHost / UserProxyPort / SdkManagersFound`。
- **幂等性**：写入前先用 `Get-CleanSdkManagerText` 剥掉旧标记块与任何已存在的 `--proxy*` 参数，重复 `Install` 不会叠加参数；备份仅在无备份或上次已托管时刷新。
- **失败模式**：直接 throw，错误信息为 `Unsupported sdkmanager.bat format`、`Could not find the sdkmanager execution marker`。

### 2.2 根因一：SDK 检测卡住——sdkmanager 与 Gradle 不继承 Windows 用户代理

Unity 的 SDK 检测会调用 Editor 内置的 sdkmanager 执行 `--list`；Java 与 sdkmanager 都不自动继承 Windows 用户代理，检测因此长时间停在“Detecting Android SDK”。

修复由 `Configure-UnityAndroidProxy` 落到三处，三处都是机器级配置，不属于工程内文件：

1. Editor 内置 sdkmanager：在 `@rem Execute sdkmanager` 之前插入标记块，并把调用行改写为追加 `%UNITY_ANDROID_SDKMANAGER_PROXY_ARGS%`，以 ASCII 编码写回。
2. 用户级 Gradle 配置：`systemProp.http/https.proxyHost/proxyPort` 与 `nonProxyHosts`，以 UTF-8 无 BOM 写入。
3. 用户级环境变量 `UNITY_ANDROID_PROXY_HOST` 与 `UNITY_ANDROID_PROXY_PORT`。

### 2.3 根因二：Release 构建在 AAR 解包阶段失败——非 ASCII StreamingAssets 名

堆栈落在 `com.android.builder.aar.AarExtractor` + `java.nio.charset.MalformedInputException` + `malformed input off : 8, length : 1`。

- 修复：StreamingAssets 下的引导文本改名为 ASCII 名 `GameGuide`，**文件内容仍可 UTF-8 中文**。
- StreamingAssets 下只有 `GameGuide` 及其元文件，1372 字节。

### 2.4 产物位置

构建产物目录下只有一个 `Android` 子目录：

| 产物 | 字节数 | 最后写入 |
|---|---|---|
| `My_ARPG-release.apk` | 28,509,358 | 2026-08-09 18:06:23 |
| `My_ARPG.apk` | 45,437,354 | 2026-08-09 17:21:03 |

两份产物都位于构建产物目录的 `Android` 子目录下。

### 2.5 签名现状：Android Debug 证书

- 播放器设置里 `androidUseCustomKeystore` 为 `0`，`AndroidKeystoreName` 与 `AndroidKeyaliasName` 均为空，未配置自有 keystore。
- 产物签名主体为 `Signer #1 certificate DN: C=US, O=Android, CN=Android Debug`。

### 2.6 环境自检顺序

照此顺序做，不要跳步。

1. 读编辑器版本记录，确认 Editor 版本，本机为 `2022.3.62f3c1`。
2. 从 Unity Hub 的次要安装路径记录读出 Editor 安装根目录；本机为 `D:\Unity\Editor`。
3. 探测内置 sdkmanager，本机命中 1 条：cmdline-tools 6.0。探测不到时用 `-EditorRoot` 显式指定。
4. 用 `-Action Status` 读出实际状态，不要照抄任何示例路径。
5. 代理连通性：`Test-NetConnection <代理地址> -Port <代理端口>`。
6. 网络路径验证：用 Status 输出里的 sdkmanager 执行 `--list`；`--list` 访问远程仓库，`--version` 只读本地版本。
7. 构建后在 Editor 里做一次非 Development 构建，再走 §2.7 自检。

### 2.7 构建后自检

Console 无 error → 产物存在且非空 → 包内条目名全 ASCII → 签名非 `CN=Android Debug` → 真机冒烟，覆盖启动、场景切换与存档读写 → 把实际值补记进构建验证记录。

签名校验用 Editor 内置 build-tools；本机存在 `33.0.2` 与 `34.0.0` 两套，各含 apksigner：

```powershell
& '<apksigner 的完整路径>' verify --print-certs '<apk 路径>'
```

## 3. 约定与硬边界

### 3.1 禁止用关闭 lintVital 来修复 AAR 报错

### 3.2 包内目录名必须 ASCII

会进包的目录首当其冲的是 StreamingAssets，其中不得出现中文名。

### 3.3 `-Action Install` 是机器级副作用，不是项目级操作

它改 Editor 安装目录、用户级 Gradle 配置与用户环境变量，会影响使用这些 Editor 的所有项目。Unity Hub 覆盖或修复 Editor 后必须重跑一次。

### 3.4 `GradleManaged = False` 意味着 Gradle 代理属性来自脚本之外

用户级 Gradle 配置里的代理条目可能由其他工具或人工写入且不带脚本标记。`Remove` 只删自己带标记的块，不动这些条目。

### 3.5 Unity 已启动时用户环境变量不会注入既有进程

但批处理内的默认值仍立即生效。切换代理端口后要重跑 `Install` 并重启 Unity。

### 3.6 `CN=Android Debug` 的产物不得对外发布

签名现状见 §2.5。

### 3.7 本域不改工程设置

修改 Player 设置属于配置域，见 `ProjectConfig_Guide.md`。
