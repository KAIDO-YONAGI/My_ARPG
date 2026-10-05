# Android 构建与发布（Windows）

文档 ID：`BUILD-ANDROID-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责本工程 Windows 上 Android 构建的环境自检、两类已知故障的根因与幂等修复、构建产物位置与签名现状。Player/Project 配置项字典见 `ProjectConfig_Guide.md`，测试基线见 `TestBaseline_Guide.md`。本域不提供 Unity 命令行打包命令：仓库内不存在这样的命令，见 §4 D2。
上游来源：UnityAndroid 构建指南、构建验证记录、构建诊断笔记、UnityAndroid 代理配置脚本、播放器设置资产、工程自述文档。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `Android 构建` / `打包 APK` | §2.1、§2.4 产物位置、§5 未核验 |
| `Detecting Android SDK` 卡住 / `Checking Android SDK and components` | §2.2 根因一 |
| `lintVitalAnalyzeRelease` / `ExtractAarTransform` / `AarExtractor` | §2.3 根因二 |
| `MalformedInputException` / `malformed input off` | §2.3 根因二 |
| `StreamingAssets 中文名` | §2.3 根因二 |
| `sdkmanager 代理` / `gradle.properties` / `UNITY_ANDROID_PROXY_*` | §2.2、§2.6 环境自检 |
| `keystore` / `签名` / `apksigner verify` | §2.5 签名现状、§3 硬边界 |
| `Release 构建失败排查` | §3、§4 |
| `构建产物在哪` | §2.4 |

## 2. 当前实现

### 2.1 唯一的 Android 工具链入口

`Configure-UnityAndroidProxy` 是本域唯一的可执行工具，要求 PowerShell 5.1 以上。

- 参数面：`-Action Install|Status|Remove`，默认 `Install`；`-ProxyHost` 默认 `127.0.0.1`；`-ProxyPort` 默认 `7890`，校验 1..65535；`-EditorRoot string[]`；`-SkipNetworkCheck`。
- Editor 根目录发现顺序：显式 `-EditorRoot` → Unity Hub 的次要安装路径记录 → 程序目录下的 Unity Hub 与 Editor 路径 → 正在运行的 Unity 进程 → 注册表卸载项。
- 目标文件：Editor 安装目录下 AndroidPlayer 的 cmdline-tools 通配路径中的 sdkmanager 批处理。
- 三个 Action 的分派：`Install`、`Remove`、`Status`；汇总对象字段为 `Action / Proxy / ProxyReachable / GradleProperties / GradleManaged / UserProxyHost / UserProxyPort / SdkManagersFound`。
- **幂等性证据**：写入前先用 `Get-CleanSdkManagerText` 剥掉旧标记块与任何已存在的 `--proxy*` 参数，因此重复 `Install` 不会叠加参数；备份仅在无备份或上次已托管时刷新，避免把已改版本存成备份。
- **失败模式**：直接 throw，静默跳过不发生 —— `Unsupported sdkmanager.bat format`、`Could not find the sdkmanager execution marker`。

### 2.2 根因一：SDK 检测卡住——sdkmanager 与 Gradle 不继承 Windows 用户代理

Unity 的 SDK 检测会调用 Editor 内置的 sdkmanager 执行 `--list`；Java 与 sdkmanager 都不自动继承 Windows 用户代理，检测因此长时间停在“Detecting Android SDK”。

修复由 `Configure-UnityAndroidProxy` 落到三处，三处都是机器级配置，不属于工程内文件：

1. Editor 内置 sdkmanager：在 `@rem Execute sdkmanager` 之前插入标记块，并把调用行改写为追加 `%UNITY_ANDROID_SDKMANAGER_PROXY_ARGS%`，以 ASCII 编码写回。
2. 用户级 Gradle 配置：`systemProp.http/https.proxyHost/proxyPort` 与 `nonProxyHosts`，以 UTF-8 无 BOM 写入。
3. 用户级环境变量 `UNITY_ANDROID_PROXY_HOST` 与 `UNITY_ANDROID_PROXY_PORT`。

本机现状：2026-10-05 只读复核，未运行 Unity。

- `-Action Status -SkipNetworkCheck` 输出 `State = Patched`、`SdkManagersFound = 1`、`GradleManaged = False`、`UserProxyHost/UserProxyPort = 127.0.0.1 / 7890`。
- 被补丁的文件是 Editor 安装目录下 AndroidPlayer 的 cmdline-tools 6.0 sdkmanager，其中含标记块与改写后的 `SdkManagerCli ... %UNITY_ANDROID_SDKMANAGER_PROXY_ARGS% %CMD_LINE_ARGS%` 调用行，同目录留有该文件的原始备份。
- 用户主目录下的 Gradle 配置含 `systemProp.*.proxyHost/proxyPort` 六行，时间戳为 `Sun Aug 09 20:34:30 CST 2026`；这六行由脚本之外的工具或人工写入，因此不含脚本的 `# >>> UNITY_ANDROID_PROXY >>>` 标记 —— 这正是 `GradleManaged = False` 而代理仍然生效的情形。

### 2.3 根因二：Release 构建在 AAR 解包阶段失败——非 ASCII StreamingAssets 名

堆栈落在 `com.android.builder.aar.AarExtractor` + `java.nio.charset.MalformedInputException` + `malformed input off : 8, length : 1`。

- 修复：StreamingAssets 下的引导文本改名为 ASCII 名 `GameGuide`，**文件内容仍可 UTF-8 中文**。
- 现状：2026-10-05 实测，StreamingAssets 下只有 `GameGuide` 及其元文件，1372 字节，无中文名文件。
- 有效性证据：构建验证记录里该产物内 `assets` 下的 GameGuide 条目存在，非 ASCII 条目 0 条。
- 注意范围：工程根的项目文档目录下仍有中文名的开发日志与游戏指南两份文本文件，而该目录不在会进包的目录内，不进入 AAR。**规则是“被打包目录内的名字必须 ASCII”。**

### 2.4 产物位置（实测）

构建产物目录下只有一个 `Android` 子目录：

| 产物 | 字节数 | 最后写入 |
|---|---|---|
| `My_ARPG-release.apk` | 28,509,358 | 2026-08-09 18:06:23 |
| `My_ARPG.apk` | 45,437,354 | 2026-08-09 17:21:03 |

两份产物都位于构建产物目录的 `Android` 子目录下。`.apk` 与 `.aab` 的取舍、Development 与 Release 的取舍无法从仓库判定：仓库内没有编辑器构建目标记录文件，该记录目录也在忽略规则内。

### 2.5 签名现状：仍是 Android Debug 证书（发布阻断）

- 播放器设置里 `androidUseCustomKeystore` 为 `0`，`AndroidKeystoreName` 与 `AndroidKeyaliasName` 均为空 —— 未配置自有 keystore。
- 构建验证记录里的签名主体是 `Signer #1 certificate DN: C=US, O=Android, CN=Android Debug`。
- 结论：**当前产物不可对外发布**，与工程自述文档的说明一致。

### 2.6 环境自检顺序

照此顺序做，不要跳步。

1. 读编辑器版本记录，确认 Editor 版本，本机为 `2022.3.62f3c1`。
2. 从 Unity Hub 的次要安装路径记录读出 Editor 安装根目录；本机为 `D:\Unity\Editor`。
3. 探测内置 sdkmanager，本机命中 1 条：cmdline-tools 6.0。探测不到时用 `-EditorRoot` 显式指定。
4. 用 `-Action Status` 读出实际状态，**不要照抄任何示例路径**。
5. 代理连通性：`Test-NetConnection <代理地址> -Port <代理端口>`。
6. 真实验证网络路径：用 Status 输出里的 sdkmanager 执行 `--list`。**只跑 `--version` 不算证据**。
7. 构建后在 Editor 里做一次非 Development 构建，再走 §2.7 自检。

### 2.7 构建后自检

Console 无 error → 产物存在且非空 → 包内条目名全 ASCII → 签名非 `CN=Android Debug` → 真机冒烟，覆盖启动、场景切换与存档读写 → 把实际值补记进构建验证记录。

签名校验用 Editor 内置 build-tools；本机存在 `33.0.2` 与 `34.0.0` 两套，各含 apksigner：

```powershell
& '<apksigner 的完整路径>' verify --print-certs '<apk 路径>'
```

## 3. 约定与硬边界

1. **禁止用关闭 lintVital 来“修复” AAR 报错**：这是掩盖 AAR 结构问题。后果：真正的非 ASCII 条目继续留在包里，问题被压到运行时与其他工具链。
2. **包内目录名必须 ASCII**：会进包的目录首当其冲的是 StreamingAssets，其中不得出现中文名。后果：Release 构建在 lint 与 AAR 阶段失败，且报错位置离真实原因很远。
3. **`-Action Install` 是机器级副作用，不是项目级操作**：它改 Editor 安装目录、用户级 Gradle 配置与用户环境变量，会影响使用这些 Editor 的**所有项目**。Unity Hub 覆盖或修复 Editor 后必须重跑一次。
4. **`GradleManaged = False` 意味着 Gradle 代理属性来自脚本之外**：用户级 Gradle 配置里的代理条目可能由其他工具或人工写入且不带脚本标记，本机实测正是这种情况。`Remove` 只删自己带标记的块，不动这些条目。
5. **Unity 已启动时用户环境变量不会注入既有进程**，但批处理内的默认值仍立即生效。切换代理端口后要重跑 `Install` 并重启 Unity。
6. **`CN=Android Debug` 的产物不得对外发布**（§2.5）。后果：任何分发都是拿调试证书签名，无法上架，也无法做正式升级链。
7. **本域不改工程设置**：修改 Player 设置属于配置域，见 `ProjectConfig_Guide.md`。误改会静默改变构建产物契约。

## 4. 已知缺陷与风险

- **D1（发布阻断）**：无自有 keystore —— 播放器设置里 `androidUseCustomKeystore` 为 `0`，密钥别名与路径为空。
- **D2**：无法用 CLI 复现构建。仓库内不存在 Android Player 打包命令。对全仓的 markdown、脚本、批处理、清单与源码检索 `-buildTarget`、`-executeMethod`、`BuildPipeline.`、`BuildPlayerOptions` 均无命中，唯一命中的是测试用的 `-runTests -testPlatform`。已知做法是在编辑器里执行一次非 Development 构建。→ 该命令必须由用户给出，见 `..\Workflow\Project_Validation_Guide.md` 的空缺项。
- **D3**：无 CI，证据只在本机。仓库内没有 CI 配置目录；构建产物目录、日志目录、批处理脚本、IDE 工程文件与解决方案文件都在忽略规则内，构建证据只留在本机。
- **D4（构建目标状态不可考）**：用户设置目录下只有编辑器用户设置、搜索记录与界面布局，没有编辑器构建目标记录文件，且该目录在忽略规则内。
- **D5（产物与工作区不一致）**：构建产物写于 2026-08-09；当前工作区存在未提交改动，涉及包清单与锁定清单、若干产品脚本与场景、工程自述文档，另有未跟踪的新增寻路实现与对应测试文件；因此产物内容与工作区当前状态不一定一致。

## 5. 未核验事项

- 假设：`scriptingBackend.Android` 为 `1` 等价于 IL2CPP、`AndroidTargetArchitectures` 为 `2` 等价于 ARM64；本指南只把序列化原值当事实，这两个映射关系待编辑器实测。
- 假设：45MB 的 `My_ARPG.apk` 是 Development 构建、28MB 的 `My_ARPG-release.apk` 是非 Development 构建；该区分仅由文件名与体积推断，待编辑器实测。
- 假设：脚本当前已让本机 sdkmanager 走通代理；只读了补丁内容与 `State = Patched`，`sdkmanager --list` 的联网结果待实测。
- 假设：补丁后的 sdkmanager 在 Unity 2022.3 的 SDK 检测路径上确实被调用，待编辑器实测。
- 假设：`33.0.2` 与 `34.0.0` 两套 build-tools 的 apksigner 都能校验本工程产物；未执行 verify，待实测。
- 假设：包清单里的仓库外 `file:` 依赖 unity-mcp 不影响 Android 打包产物内容，待编辑器实测。
