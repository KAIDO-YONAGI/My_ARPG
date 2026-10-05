# My_ARPG 待用户决策清单

文档 ID：`WF-OPEN-DECISIONS`  
状态：`Proposal`  
最后更新：`2026-10-05`  
权威范围：集中登记已确证、需用户拍板才能推进的事项，避免它们散落在各域 DeveloperLog 里被漏读。  
**本文件不拥有业务事实。** 每一条只写决策点、归属域与出处；事实与证据由被引用的权威文档负责，冲突时以那份文档为准。

## 决策清单

| 编号 | 决策点 | 归属域 | 出处 | 影响面 |
|---|---|---|---|---|
| `DEC-01` | 掉落物的存档键只有预制体维度：`lootsStatsDic` 以预制体上烘焙的 `SaveDefinition.ID` 为键，同一掉落预制体在多场景复用时共用一个键。是否改为「场景 + ID」复合键，或给每个场景副本独立 ID？ | `Data` | `Data\SaveData_Guide.md` | `SaveDataManager` 与 `SaveDefinition`、掉落物预制体、相关场景 |
| `DEC-02` | `RetrySceneSO` 重试场景资产零引用：重试请求事件资产由 `PlayerDamageController` 订阅处理，`SceneChanger` 的订阅列表里没有它。删除，还是保留为备用资产？ | `SceneFlow` / `Assets` | `SceneFlow\SceneFlow_Guide.md`、`Assets_Guide.md` | 重试场景配置资产 `RetrySceneSO` |
| `DEC-03` | `InitialScene` 的常驻场景列表写在 `MenuScene` 上，`PersistentScene` 因此不会被加载。是否修正？静态推断，运行期行为待编辑器实测。 | `SceneFlow` | `SceneFlow\SceneFlow_Guide.md` | 常驻场景 `InitialScene`、`MenuScene` 与工程构建场景列表 |
| `DEC-04` | 单例预算：现状文档认定的合规集合为 6 个，工程内实际有 19 个具体 `YSingleton<T>` 派生类。是否立项收敛为合并与下沉，还是接受现状并把预算口径改写为实际值？ | `Architecture` | `Architecture\Composition\Composition_Guide.md` | `YSingleton` 契约与全部单例调用点 |
| `DEC-05` | 程序集划分：文档把「引入 asmdef」当作待办，工程当前 0 个 asmdef，跨层引用靠纪律约束。是否排期拆分程序集？ | `Architecture` | `Architecture\AssemblyPlan\AssemblyPlan_Proposal.md` | 全部产品代码与工程设置 |
| `DEC-06` | xLua 热更 MVP 是否推进？插件目录当前只有 `Newtonsoft.Json` 库，xLua 源码与 native 插件的来源与版本需你指定。 | `Architecture` | `Architecture\AssemblyPlan\AssemblyPlan_Proposal.md` | 插件目录与全部产品代码 |
| `DEC-07` | 反向依赖是否立项消除：`PlayerStatsSO` 是唯一的管线层到玩法层引用；`ICanvasManager` 的默认方法直接调 `UIManager` 单例。 | `Architecture` | `Architecture\Layering\Layering_Guide.md`、`Architecture\Composition\Composition_Guide.md` | 管线层配置类型与契约层 |
| `DEC-08` | 游戏指南正文有两份副本：`Docs` 目录下的中文文本与流式资源目录 `StreamingAssets` 下的 `GameGuide`，当前字节完全相同，SHA256 以 `7862539400` 开头。是否删除 `Docs` 副本，收敛为流式资源目录下的唯一正文？ | `Assets` | `Assets_Guide.md`、根 `Router.md` 索引 `REF-GAMEGUIDE-DUPLICATE` | 游戏指南正文副本 |
| `DEC-09` | 发布签名：工程 Player 设置的 `androidUseCustomKeystore` 为 0，`AndroidKeystoreName` 与 `AndroidKeyaliasName` 为空，产物使用 Android 调试签名。是否配置自有 keystore？ | `Build` | `Build\AndroidBuild_Guide.md` | 工程 Player 设置与发布产物 |
| `DEC-10` | 包清单里的 `com.coplaydev` MCP 编辑器包指向仓库外、且被忽略规则排除的本地 file 路径，全新克隆解析不了该依赖。是否改为 registry 版本，或从依赖中移除？ | `Build` | `Build\ProjectConfig_Guide.md` | 包清单与锁文件 |
| `DEC-11` | 工程编辑器设置的 `m_EnterPlayModeOptionsEnabled` 为 0，Domain Reload 保持默认开启。是否关闭？关闭会改变全部静态状态的复位行为。 | `Build` | `Build\ProjectConfig_Guide.md` | 工程编辑器设置与 `SaveRegistry` |
| `DEC-12` | 存档样本数与文档口径不一致：存档目录实测 73 份 `SystemSave` 与 1 份 `PlayerSave`，存档指南 §2.3 的口径快照记 72 加 1。是否刷新分母？刷新会改动已发布的实测口径。 | `Data` | `Data\SaveData_Guide.md` | 仅文档 |
| `DEC-13` | 仓库内没有 CI 配置，构建产物目录与日志目录由忽略规则排除，构建与测试证据只在本机；唯一的 headless 测试命令把绝对路径写死，退出码不可靠。是否引入 CI，或先把测试命令改造成读结果 XML 判定的脚本？ | `Build` | `Build\TestBaseline_Guide.md`、`Workflow\Project_Validation_Guide.md` | 验证脚本与仓库配置 |

## 使用约定

- 本表条目在用户明确拍板前**不得自行实施**；改动产品代码、工程设置与包清单，或删除既有
  文件，都属于需要用户确认的动作。
- 用户拍板后：实施该动作的 Agent 负责把结论写进归属域的权威文档与 DeveloperLog，并把
  这里的对应行改为「已决策」并注明结论与日期；未拍板的条目保留。
- 新增决策点前先检索本表，不重复登记同一问题。
