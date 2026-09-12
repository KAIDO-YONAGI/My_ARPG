# skill 草案：unity-android-build-diagnostics

> 这是从 [`UnityAndroidBuildGuide.md`](UnityAndroidBuildGuide.md) 拆出来的**元数据与工作流草案**，
> 供制作可复用 skill 时使用。本机已存在同名 skill（`unity-android-build-diagnostics`，配套本仓库的
> `Tools/UnityAndroid` 脚本）；本文件是仓库侧的原始草案，便于换机器或换 agent 时重建。

## 元数据

```yaml
---
name: unity-android-build-diagnostics
description: Diagnose and fix Unity Android SDK detection hangs, sdkmanager/Gradle proxy issues, and Release AAR lint extraction failures on Windows. Use when Unity is stuck at Detecting Android SDK or fails in lintVitalAnalyzeRelease, ExtractAarTransform, or AarExtractor.
---
```

## 触发场景

- Unity 卡在 `Detecting Android SDK` / `Checking Android SDK and components`。
- Android 构建失败或卡住，落在 `lintVitalAnalyzeRelease`、`ExtractAarTransform`、`AarExtractor`。
- 出现 `java.nio.charset.MalformedInputException` / `malformed input off` 堆栈。
- `StreamingAssets` 里的中文文件名导致构建失败。
- 需要给 `sdkmanager` 或 Gradle 配代理。

## 建议工作流

1. 先收集 Unity 版本、Editor 路径、代理监听端口、Editor.log 和完整 Gradle 堆栈。
   （路径**从本机读出来**，见指南「第一步：确认环境」，不要照抄任何示例路径。）
2. 优先搜索 Unity Discussions、Issue Tracker、Android/Gradle 官方资料和已知问题。
3. 直接复现 `sdkmanager --list`，不要只根据 Unity 进度条猜测。
4. 检查用户级 Gradle 配置 `%USERPROFILE%\.gradle\gradle.properties` 和 Editor 内置 `sdkmanager.bat`。
5. 对 AAR 先做 ZIP 完整性和条目编码检查。
6. 修改后必须验证：SDK Manager、Unity Console、完整 Release 构建、APK 签名。

## 配套资源

- 配置与排障正文：[`UnityAndroidBuildGuide.md`](UnityAndroidBuildGuide.md)
- 历史验证记录：[`UnityAndroidBuildVerification.md`](UnityAndroidBuildVerification.md)
- 一键配置脚本：[`../Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1`](../Tools/UnityAndroid/Configure-UnityAndroidProxy.ps1)
