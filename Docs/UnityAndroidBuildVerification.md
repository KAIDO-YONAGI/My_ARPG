# Unity Android 构建验证记录

> 这是**历史记录**，不是操作前提。配置与排障步骤见 [`UnityAndroidBuildGuide.md`](UnityAndroidBuildGuide.md)。
> 记录里出现的路径属于当时的机器；换机器后照抄格式追加一条新记录即可，不要改旧记录。

## 2026-08-09 — Android Release 构建通过（首次修复验证）

### 环境

| 项 | 值 |
| --- | --- |
| 日期 | 2026-08-09 |
| 系统 | Windows（Unity Hub 安装的 Editor） |
| Unity | 2022.3.62f3c1（见 `ProjectSettings/ProjectVersion.txt`） |
| Editor 安装根目录 | `D:\Unity\Editor`（该机器 Unity Hub 的安装位置） |
| Editor 内置 cmdline-tools | `6.0` |
| 代理 | `127.0.0.1:7890` |

### 修复项

1. 用 `Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1 -Action Install` 给 Editor 内置
   `sdkmanager.bat` 和用户级 Gradle 配置注入代理，解决 `Detecting Android SDK` 卡住。
2. 把 `Assets/StreamingAssets/游戏指南.txt` 改名为 `Assets/StreamingAssets/GameGuide.txt`，
   解决 Release lint 解包 AAR 时的 `java.nio.charset.MalformedInputException`。

### 结果

| 检查项 | 结果 |
| --- | --- |
| Android Release 构建 | 成功 |
| 构建错误数 | 0 |
| 产物 | `Builds/Android/My_ARPG-release.apk`（28,509,358 字节，2026-08-09 18:06 生成） |
| 包内 StreamingAssets 入口 | `assets/GameGuide.txt`，包内无非 ASCII 条目 |
| 签名校验 | 通过，但证书为 `CN=Android Debug` |
| 遗留待办 | 正式发布前必须换成项目自己的 keystore |

### 只读复核（2026-09-12）

对上面同一份产物做了一次不重新构建的复核：

```powershell
# 1) 产物与签名证书
Get-ChildItem Builds\Android
& '<build-tools 下的 apksigner.bat>' verify --print-certs Builds\Android\My_ARPG-release.apk

# 2) 包内条目与 ASCII 检查（.NET ZIP API，无需 7-Zip）
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path 'Builds\Android\My_ARPG-release.apk'))
$zip.Entries | Where-Object { $_.FullName -like 'assets/*' }        # 期望含 assets/GameGuide.txt
$zip.Entries | Where-Object { $_.FullName -match '[^\x00-\x7F]' }   # 期望无输出
$zip.Dispose()
```

复核结论与 2026-08-09 的记录一致：

- `Signer #1 certificate DN: C=US, O=Android, CN=Android Debug` —— keystore 仍未配置。
- `assets/GameGuide.txt` 存在。
- 非 ASCII 条目 0 条。

## 追加新记录模板

```markdown
## YYYY-MM-DD — <一句话结论>

### 环境

| 项 | 值 |
| --- | --- |
| Unity | <ProjectSettings/ProjectVersion.txt 的值> |
| Editor 安装根目录 | <第一步读出的 $editorRoot> |
| 代理 | <地址:端口，或「不需要代理」> |

### 修复项 / 变更项

1. ...

### 结果

| 检查项 | 结果 |
| --- | --- |
| Android Release 构建 | |
| 构建错误数 | |
| 产物 | |
| 包内 StreamingAssets 入口 | |
| 签名校验 | |
```

自检项的含义见指南的「构建后自检清单」。
