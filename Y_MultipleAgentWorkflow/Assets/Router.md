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
| `GameSceneSO 资产` / `场景资产` / `sceneType` | `Assets_Guide.md`（加载语义归 SceneFlow） |
| `StreamingAssets` / `GameGuide` / `中文文件名` / `非 ASCII 文件名` / Android 构建失败 | `Assets_Guide.md` |
| `生成器` / `批量创建资产` / `手工创建资产` | `Assets_Guide.md` |
| `事件通道` / `事件SO` / `EventSO` / `VoidEventSO` / `ToggleCanvasEventSO` | `EventChannels_Guide.md` |
| `订阅` / `OnEnable +=` / `OnDisable -=` / `Raise` / `发布` | `EventChannels_Guide.md` |
| `事件不生效` / `接线断了` / `Inspector 没拖` / `改名事件资产` | `EventChannels_Guide.md`（第 3.4 检查清单） |
| `canvasToToggle` / `toggleCanvasEvents` | `EventChannels_Guide.md` |
| `InventorySlotsStatsSO` / `SlotsUpdateRequest` | `EventChannels_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Assets`
- `path:Y_MultipleAgentWorkflow\Assets\`
- `path:Assets\GameSO\`
- `path:Assets\StreamingAssets\`

## 能力边界

**Active 能力**

- 声明并维护 `Assets/GameSO/**` 的 ScriptableObject 资产契约：目录分布、按类数量、命名约定、手工创作流程（`Assets_Guide.md`）。
- 声明并维护事件通道的资产侧契约：12 个通道类 / 28 个通道资产清单、订阅发布惯例、改名检查清单（`EventChannels_Guide.md`）。
- 声明 `Assets/StreamingAssets/**` 的构建硬约束：文件名必须 ASCII、Android 上位于 APK 内不可用 `System.IO` 直接访问、改完需复核包内条目。
- 提供"零外部引用资产"与"schema 漂移资产"的现状清单（作为缺陷登记，不是改造计划）。

**不在本域范围（请转到对应 Router）**

- 事件总线机制本身是否成立、事件与 MVC/服务层的边界 → Architecture。
- 场景组加载、`SceneChanger`、Additive 语义、传送接线 → SceneFlow。
- 存档 schema、`SaveKey`、GUID 身份体系 → Data。
- 各 SO 的业务判定语义（任务进度、对话分支、背包/商店结算）→ 对应 Gameplay 子域。
- 美术资源（`Sprites`/`Animation`/`Prefabs`）的导入设置与体积 → 尚未建立权威文档。
- `Assets/Settings/Input/**` 与 `Assets/Editor/InputActionReferenceRebuilder.cs` → 尚未建立权威文档。

**需要用户确认的事项**

- 是否清理 18 个零外部引用的 GameSO 资产（含整棵 PurpleBob 对话树与 3 个零引用通道资产）：删除属破坏性操作，且存在"间接运行时引用"的未核验可能，须用户确认后进行。
- 是否为 `GameSO` 建立资产生成器/校验器（当前全部手工创建、无校验）。
- 是否重构 `SkillSO` 效果的字符串 switch 耦合（`SkillManager.cs:21-31`）。
- 是否移除 5 个 DialogSO 系列资产中已不存在的 `chatType`/`canOnlyBeTriggeredOnce` 残留字段。
- 是否按正规方式（`UnityWebRequest`）修复 Android 上 `OpenTxtWithSystem` 不可用的问题，或明确其仅面向桌面。

**Proposal**

- 暂无。本 Router 与其 Guide 只描述当前实现事实；任何改造方案须经上述确认后另立 Design 文档。
