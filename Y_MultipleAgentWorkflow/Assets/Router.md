# Assets Router

文档 ID：`BUS-ASSETS`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `GameSO` / `ScriptableObject 资产` / `CreateAssetMenu` / `资产目录约定` / `资产命名` | `Assets_Guide.md` |
| `ItemSO` / `QuestSO` / `DialogSO` / `RefuseDialogSO` / `CharacterSO` / `SkillSO` / `PlayerStatsSO` / `LocationSO` | `Assets_Guide.md` |
| `GameSceneSO 资产` / `场景资产` / `sceneType` | `Assets_Guide.md`，加载语义归 SceneFlow 域 |
| `StreamingAssets` / `GameGuide` / `中文文件名` / `非 ASCII 文件名` / Android 构建失败 | `Assets_Guide.md` |
| `生成器` / `批量创建资产` / `手工创建资产` | `Assets_Guide.md` |
| `事件通道` / `事件SO` / `EventSO` / `VoidEventSO` / `ToggleCanvasEventSO` | `EventChannels_Guide.md` |
| `订阅` / `OnEnable +=` / `OnDisable -=` / `Raise` / `发布` | `EventChannels_Guide.md` |
| `事件不生效` / `接线断了` / `Inspector 没拖` / `改名事件资产` | `EventChannels_Guide.md` 第 3.4 节检查清单 |
| `canvasToToggle` / `toggleCanvasEvents` | `EventChannels_Guide.md` |
| `InventorySlotsStatsSO` / `SlotsUpdateRequest` | `EventChannels_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Assets`
- 本业务根的文档目录
- GameSO 资产树
- StreamingAssets 内容

## 能力边界

**Active 能力**

- 声明并维护 GameSO 资产树的 ScriptableObject 资产契约：目录分布、按类数量、命名约定与手工创作流程，见 `Assets_Guide.md`。
- 声明并维护事件通道的资产侧契约：12 个通道类、28 个通道资产、订阅发布惯例与改名检查清单，见 `EventChannels_Guide.md`。
- 声明 StreamingAssets 内容的构建硬约束：文件名必须为 ASCII，在 Android 上它位于 APK 内、`System.IO` 无法直接读取，改完需复核包内条目。
- 提供零外部引用资产与 schema 漂移资产的现状清单，作为缺陷登记。

**不在本域范围，请转到对应 Router**

- 事件总线机制本身是否成立、事件与 MVC 及服务层的边界，归 Architecture 域。
- 场景组加载、`SceneChanger`、Additive 语义、传送接线，归 SceneFlow 域。
- 存档 schema、`SaveKey`、GUID 身份体系，归 Data 域。
- 各 SO 的业务判定语义，即任务进度、对话分支、背包与商店结算，归对应 Gameplay 子域。
- 美术资源 `Sprites`、`Animation`、`Prefabs` 的导入设置与体积，尚无权威文档。
- 输入动作配置目录与 `InputActionReferenceRebuilder` 脚本，尚无权威文档。

**需要用户确认的事项**

- 是否清理 18 个零外部引用的 GameSO 资产，其中含整棵 PurpleBob 对话树与 3 个零引用通道资产。删除属破坏性操作，且间接运行时引用的可能性待编辑器实测，须用户确认后进行。
- 是否为 GameSO 建立资产生成器或校验器；当前资产全部手工创建，没有校验。
- 是否重构 `SkillSO` 效果与 `SkillManager` 字符串 switch 之间的耦合。
- 是否移除 5 个 DialogSO 系列资产保留的 `chatType` 与 `canOnlyBeTriggeredOnce` 序列化残留。
- 是否改用 `UnityWebRequest` 解决 `OpenTxtWithSystem` 在 Android 上不可用的问题，或明确它只面向桌面平台。

**Proposal**

- 暂无。本 Router 与其 Guide 只描述当前实现事实；任何改造方案须经上述确认后另立 Design 文档。
