# 项目配置与构建输入

文档 ID：`BUILD-PROJECTCONFIG-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：只负责“重现阶段一份可构建工程所需的最小事实”——Unity 版本、包依赖、构建场景列表与顺序、Player/Build 配置项原值、程序集结构。Android 排障见 `AndroidBuild_Guide.md`，测试内容见 `TestBaseline_Guide.md`；枚举值的引擎语义见 §5。
上游来源：编辑器版本记录、包清单与锁定清单、构建场景列表资产、播放器设置资产、编辑器设置资产、本机 Unity 生成的程序集工程、Addressables 配置资产、工程自述文档。

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `Unity 版本` | §2.1 |
| `包版本` / `manifest 依赖` / `包清单` | §2.2 |
| `asmdef` / `预定义程序集` / `Assembly-CSharp` | §2.3 |
| `构建场景列表` / `场景顺序` / `入口场景 InitialScene` | §2.4 |
| `Player Settings` / `Product Name` / `包名` / `minSdk` / `targetSdk` | §2.5 |
| `Addressables` | §2.6 |
| `domain reload` / `序列化模式` | §2.7 |
| `全新克隆能不能跑起来` | §3、§4 D1 |

## 2. 当前实现

### 2.1 Unity 版本

编辑器版本记录为 `m_EditorVersion: 2022.3.62f3c1`、`m_EditorVersionWithRevision: 2022.3.62f3c1 (1623fc0bbb97)`。工程自述文档推荐用 Unity 2022.3.62f3c1 打开项目。

### 2.2 包依赖

包清单里的关键非模块依赖如下，版本取清单原值：

| 包 | 版本 |
|---|---|
| `com.coplaydev.unity-mcp` | 10.0.0，`file:` 依赖指向仓库外的本地目录 |
| `com.unity.addressables` | 1.22.3 |
| `com.unity.cinemachine` | 2.10.5 |
| `com.unity.collab-proxy` | 2.11.3 |
| `com.unity.feature.2d` | 2.0.1 |
| `com.unity.ide.rider` | 3.0.40 |
| `com.unity.ide.visualstudio` | 2.0.22 |
| `com.unity.inputsystem` | 1.14.2 |
| `com.unity.memoryprofiler` | 1.1.12 |
| `com.unity.nuget.newtonsoft-json` | 3.2.1 |
| `com.unity.test-framework` | 1.4.6 |
| `com.unity.textmeshpro` | 3.0.9 |
| `com.unity.timeline` | 1.7.7 |
| `com.unity.ugui` | 1.0.0 |
| `com.unity.visualscripting` | 1.9.4 |

其余条目是内置模块包，版本统一为 1.0.0。锁定清单与包清单同在仓库中。

**可复现性缺口**：`com.coplaydev.unity-mcp` 指向仓库外的本地目录，该目录在忽略规则内，全新克隆无法解析这一条依赖。

### 2.3 程序集结构：0 个 asmdef

- 工程内递归检索无 asmdef —— 产品代码全部落在预定义程序集里。
- 本机 Unity 生成的编辑器程序集工程给出直接证据：程序集名为 `Assembly-CSharp-Editor`，`Compile Include` 同时收录 5 个测试文件与编辑器侧的 `InputActionReferenceRebuilder`，并 `ProjectReference` 到 `Assembly-CSharp`；两者都引用 `nunit.framework`，由扩展 NUnit 包提供。
- 这些程序集工程由 Unity 生成且落在忽略规则内，只在本机存在，作用是证明“测试确实进了预定义编辑器程序集”。
- 后果见 §3.3 与 `TestBaseline_Guide.md`。

### 2.4 构建场景列表与顺序

构建场景列表资产里有 6 条场景，全部 `enabled: 1`，按文件出现顺序即 Build Settings 顺序：

| 序 | 场景 |
|---|---|
| 0 | `InitialScene` |
| 1 | `PersistentScene` |
| 2 | `StartingMenu` |
| 3 | `Scene1` |
| 4 | `Scene2` |
| 5 | `TestScene` |

6 个场景资产在磁盘上均存在，本机实测确认。`InitialScene` 为首位且是构建入口；`TestScene` 末位，定位为独立调试场景。

列表另挂了两个配置对象，一个是 Addressables 的配置对象，一个是 Input System 项目设置的配置对象；二者都通过构建场景列表序列化引用。

### 2.5 Player / Build 配置项原值

以下原值取自播放器设置资产：

| 项 | 原值 |
|---|---|
| `companyName` | `YONAGI` |
| `productName` | `My_ARPG` |
| `bundleVersion` | `1.0` |
| `applicationIdentifier.Android` | `com.defaultcompany.my_arpg` |
| `AndroidBundleVersionCode` | `1` |
| `AndroidMinSdkVersion` | `22` |
| `AndroidTargetSdkVersion` | `33` |
| `AndroidIsGame` | `1` |
| `AndroidValidateAppBundleSize` / `AndroidAppBundleSizeToValidate` | `1` / `150` |
| `mobileMTRendering.Android` | `1` |
| `stripEngineCode` | `1` |
| `useCustomMainManifest` … `useCustomProguardFile` 共 8 项 | 全 `0` |
| `AndroidTargetArchitectures` | `2` |
| `AndroidTargetDevices` | `0` |
| `AndroidBuildApkPerCpuArchitecture` | `0` |
| `androidUseCustomKeystore` | `0` |
| `AndroidKeystoreName` / `AndroidKeyaliasName` | 空 |
| `AndroidMinifyRelease` / `AndroidMinifyDebug` | `0` / `0` |
| `scriptingBackend.Android` | `1` |
| `apiCompatibilityLevel` | `6` |
| `apiCompatibilityLevelPerPlatform` | `{}` |
| `activeInputHandler` | `1` |

另：`clonedFromGUID`、`templatePackageId` 记为 2D 模板包 7.0.4、`templateDefaultScene` 指向 `SampleScene` —— 工程源自 2D 模板。`SampleScene` 不在场景列表内，磁盘上也不存在，全树按 `SampleScene*` 检索零命中，属模板残留引用。

`scriptingDefineSymbols` 为空表，`apiCompatibilityLevelPerPlatform` 为空表，`additionalIl2CppArgs` 为空。

### 2.6 Addressables：已安装、无运行时代码调用

- 配置资产齐全：Addressables 设置资产、内置数据组、`Scenes` 组、4 个 DataBuilder、Android / WebGL / Windows 三个平台的内容状态记录。
- 产品代码里对 Addressables 的引用只有 2 条注释：`GameSceneSO` 的注释说明它的 GUID 取值与 Addressables 方案的 `sceneReference.AssetGUID` 完全一致；`SaveData` 的注释说明存的是 `GameSceneSO.SaveKey`，即 Addressables 资产 GUID。**没有任何代码调用 Addressables 运行时 API。**
- 结论：这是遗留配置，不影响构建输入的最小事实集。是否需要“打包前构建 Addressables 内容”属于未确认项，见 `..\Workflow\Project_Validation_Guide.md`。

### 2.7 编辑器行为设置

编辑器设置资产给出两条行为事实：

- `m_SerializationMode: 2` —— 资产序列化为文本。
- `m_EnterPlayModeOptionsEnabled: 0` —— **Domain Reload 处于开启状态**，`m_EnterPlayModeOptions: 3` 当前不生效。
- static 状态纪律按 Domain Reload 开启的情形理解；与项目现状文档的口径差异登记在 §4 D3。

## 3. 约定与硬边界

1. **改播放器设置资产会静默改变构建产物契约**：`companyName` 与 `productName` 决定产物文件名与 Application 标识，产物名即由 `productName` 派生，见 `AndroidBuild_Guide.md` §2.4；`applicationIdentifier.Android` 决定安装升级链。改包名后已装旧包的存档与升级路径断裂。
2. **场景顺序是有语义的，不能重排**：`InitialScene` 必须是索引 0，它是构建入口；`TestScene` 保持末位，不参与正式流程。`SceneChanger` 与 `InitialLoad` 依赖场景在构建场景列表内，并用 `Application.CanStreamedLevelBeLoaded` 预检，缺失场景会被 `LogError` 跳过。
3. **不要给测试加 asmdef 而不改引用**：当前 0 asmdef，现有 52 个用例靠 `Tests/Editor` 这一目录名落进预定义编辑器程序集 `Assembly-CSharp-Editor`。一旦引入 asmdef，这些文件会改换程序集，需要显式引用 test-framework 与产品程序集，否则编译或枚举失败。详见 `TestBaseline_Guide.md`。
4. **不要依赖仓库外依赖能解析**：包清单里的 `file:` 依赖指向被忽略的仓库外 `Materials` 目录。全新克隆上编辑器会报包解析失败，这属于已知状态，行为可预期。
5. **IDE 用的 MSBuild 目标文件只影响 IDE，不影响 Unity**：它把测试源文件从 MSBuild 项目模型移除，文件内注释明确“Unity 自己的编译、以及 Unity Test Runner 完全不受影响”。后果：IDE 里测试文件没有补全与跳转，但能编译能跑；**IDE 里搜不到不等于测试被排除**。
6. **包清单与锁定清单必须一起改**：只改包清单会让两者不一致，Unity 会在下次解析时重写锁定清单。

## 4. 已知缺陷与风险

- **D1（可复现性缺口）**：包清单把 unity-mcp 指向仓库外、被忽略规则覆盖的 `Materials` 目录；全新克隆无法按原样解析依赖。
- **D2（模板残留）**：`templateDefaultScene` 指向不在场景列表内的 `SampleScene`，该场景资产在工程内也不存在；`clonedFromGUID` 与 `templatePackageId` 保留模板 GUID 与包 ID。
- **D3（文档与配置的口径差）**：项目现状文档按“Domain Reload 关闭”叙述 static 状态纪律；编辑器设置资产的当前值是 Domain Reload 开启（§2.7），本指南以当前值为准。
- **D4（枚举值语义未确认）**：`scriptingBackend.Android`、`AndroidTargetArchitectures`、`apiCompatibilityLevel`、`activeInputHandler`、`m_SerializationMode` 都只按原始值记录，语义见 §5。
- **D5**：Addressables 疑似死配置。整套 Addressables 数据在仓库里，而运行时代码零调用，`GameSceneSO` 与 `SaveData` 两处仅为注释 → 读者会误以为场景走 Addressables 加载，实际由 `SceneChanger` 加构建场景列表承担。

## 5. 未核验事项

- 假设：`scriptingBackend.Android: 1` = IL2CPP，`AndroidTargetArchitectures: 2` = ARM64；仅记录序列化原值，语义待编辑器实测。
- 假设：`apiCompatibilityLevel: 6` = .NET Standard 2.1，待编辑器实测。
- 假设：`activeInputHandler: 1` = Input System Package 新输入系统，待编辑器实测。
- 假设：`m_SerializationMode: 2` = Force Text，待编辑器实测。
- 假设：unity-mcp 是开发期工具依赖、不需要进入发布包，待编辑器实测。
- 假设：`Scenes` 组列出的 5 个地址与运行时无关，这 5 个地址是 Scene1、StartingMenu、Scene2、PersistentScene、TestScene，**不含 InitialScene**，待编辑器实测。
- 假设：锁定清单的当前内容与包清单一致；未逐字段比对，只做了存在性确认。
