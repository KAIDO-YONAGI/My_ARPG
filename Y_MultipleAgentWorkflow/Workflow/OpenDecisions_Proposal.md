# My_ARPG 待用户决策清单

文档 ID：`WF-OPEN-DECISIONS`  
状态：`Proposal`  
最后更新：`2026-10-05`  
权威范围：把当前已确证、但需要用户拍板才能推进的事项集中登记，避免它们散落在各域
DeveloperLog 里被漏读。  
**本文件不拥有业务事实。** 每一条只写决策点、归属域与出处；事实、证据与行号由被引用的
权威文档负责，冲突时以那份文档为准。

## 决策清单

| 编号 | 决策点 | 归属域 | 出处 | 影响面 |
|---|---|---|---|---|
| `DEC-01` | 掉落物的存档键没有场景维度：`lootsStatsDic` 以预制体上烘焙的 `SaveDefinition.ID` 为键，同一掉落预制体在多场景复用时共用一个键。是否改为「场景 + ID」复合键，或给每个场景副本独立 ID？ | `Data` | `Data\SaveData_Guide.md` | `Assets/Scripts/Gameplay/Save/**`、`Assets/Prefabs/Inventory/*.prefab`、相关场景 |
| `DEC-02` | `RetrySceneSO.asset` 已成为零引用孤儿资产（`SceneChanger` 已不再订阅重试事件，重试改由 `PlayerDamageController` 订阅 `VoidEventSO RetryRequestEvent`）。删除，还是保留为旧 schema 的历史资产？ | `SceneFlow` / `Assets` | `SceneFlow\SceneFlow_Guide.md`、`Assets\Assets_Guide.md` | `Assets/GameSO/GameSceneSO/OtherScenes/RetrySceneSO.asset` |
| `DEC-03` | `InitialScene` 的常驻场景列表被误迁到 `MenuScene`，`PersistentScene` 因此不会被加载。是否修？（静态推断，未运行 Unity 闭环） | `SceneFlow` | `SceneFlow\SceneFlow_Guide.md` | `Assets/Scenes/InitialScene.unity`、`ProjectSettings/EditorBuildSettings.asset` |
| `DEC-04` | 单例预算：旧现状文档认定合规集合为 6 个，工程内实际有 19 个具体 `YSingleton<T>` 派生类。是否立项收敛（合并/下沉），还是接受现状并把预算口径改写为实际值？ | `Architecture` | `Architecture\Composition\Composition_Guide.md` | `Assets/Scripts/Contracts/YSingleton.cs` 及全部单例调用点 |
| `DEC-05` | 两份文档仍把「引入 asmdef」当作待办，但工程 0 个 asmdef、跨层引用只靠纪律约束。是否排期拆分程序集？ | `Architecture` | `Architecture\AssemblyPlan\AssemblyPlan_Proposal.md` | `Assets/**`、`ProjectSettings/` |
| `DEC-06` | xLua 热更 MVP 是否推进？当前 `Assets/Plugins/` 只有 `Newtonsoft.Json.dll`，无 xLua 源码与 native 插件，来源与版本需你指定。 | `Architecture` | `Architecture\AssemblyPlan\AssemblyPlan_Proposal.md` | `Assets/Plugins/`、`Assets/Scripts/**` |
| `DEC-07` | 反向依赖是否立项消除：`Assets/Scripts/Pipeline/SO/PlayerStatsSO.cs` 是唯一的管线层→玩法层引用；`Contracts/ICanvasManager.cs` 的默认方法直接调 `UIManager` 单例。 | `Architecture` | `Architecture\Layering\Layering_Guide.md`、`Architecture\Composition\Composition_Guide.md` | `Assets/Scripts/Pipeline/SO/`、`Assets/Scripts/Contracts/` |
| `DEC-08` | `Docs/游戏指南.txt` 与 `Assets/StreamingAssets/GameGuide.txt` 当前字节完全相同（SHA256 `7862539400…`，共 2 份）。是否删除 `Docs` 副本，收敛为「`StreamingAssets` 为唯一正文」？ | `Assets` | `Assets\Assets_Guide.md`、根 `Router.md` 索引（`REF-GAMEGUIDE-DUPLICATE`） | `Docs/游戏指南.txt` |
| `DEC-09` | 发布签名：`ProjectSettings.asset` 的 `androidUseCustomKeystore: 0`，且 `AndroidKeystoreName` / `AndroidKeyaliasName` 为空（:274-275、:283）。产物只有调试签名，是否配置自有 keystore？ | `Build` | `Build\AndroidBuild_Guide.md` | `ProjectSettings/ProjectSettings.asset`、发布产物 |
| `DEC-10` | `Packages/manifest.json` 的 `com.coplaydev.unity-mcp` 指向仓库外且被 gitignore 的 `file:../../../Materials/…`，全新克隆解析不了依赖。是否改为 registry 版本、或从依赖中移除？ | `Build` | `Build\ProjectConfig_Guide.md` | `Packages/manifest.json`、`Packages/packages-lock.json` |
| `DEC-11` | `ProjectSettings/EditorSettings.asset:25` 为 `m_EnterPlayModeOptionsEnabled: 0`，即 Domain Reload 保持默认开启；旧叙述称「已关闭 Domain Reload」。是否要关闭（会改变全部静态状态的复位行为）？ | `Build` | `Build\ProjectConfig_Guide.md` | `ProjectSettings/EditorSettings.asset`、`Assets/Scripts/Contracts/SaveRegistry.cs` |
| `DEC-12` | 存档样本数已漂移：存档目录实测 73 份 `SystemSave` + 1 份 `PlayerSave`，文档 §2.3 的口径快照记 72+1。是否刷新分母（会改动已发布的实测口径）？ | `Data` | `Data\SaveData_Guide.md` | 仅文档 |
| `DEC-13` | 无 CI（仓库无 `.github/`），`Builds/`、`Logs/` 被 gitignore，构建与测试证据只在本机；唯一的 headless 测试命令硬编码绝对路径且退出码不可靠。是否引入 CI，或先把测试命令改造成「读 `testResults.xml` 判定」的脚本？ | `Build` | `Build\TestBaseline_Guide.md`、`Workflow\Project_Validation_Guide.md` | `Tools/`、仓库配置 |

## 使用约定

- 本表条目在用户明确拍板前**不得自行实施**；改动 `Assets/**`、`ProjectSettings/**`、
  `Packages/**` 或删除既有文件都属于需要用户确认的动作。
- 用户拍板后：实施该动作的 Agent 负责把结论写进归属域的权威文档与 DeveloperLog，并把
  这里的对应行改为「已决策」并注明结论与日期；未拍板的条目保留。
- 新增决策点前先检索本表，不重复登记同一问题。
