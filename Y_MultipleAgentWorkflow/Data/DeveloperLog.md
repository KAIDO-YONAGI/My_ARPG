# Data Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Data 权威文档

- 任务：为 Data 域（存档身份体系与序列化契约）建立权威 Guide，并按项目中文模板重写 Router。
- 写入的文件：
  - `Y_MultipleAgentWorkflow/Data/SaveData_Guide.md`（新建，ID `DATA-SAVEDATA-GUIDE`，Active，137 行）
  - `Y_MultipleAgentWorkflow/Data/Router.md`（重写为中文模板；原文 `Child routing = None/None`，无下级子类可保留）
  - `Y_MultipleAgentWorkflow/Data/DeveloperLog.md`（本条目）
- 依据的证据路径（本次全部实读）：
  - `Assets/Scripts/Gameplay/Save/{SaveSystem,SaveData,SaveDataManager,SaveDefinition,DynamicDataHandler,SaveLoadCanvasManager}.cs`
  - `Assets/Scripts/Contracts/{ISaveable,SaveRegistry,SaveableService,YSingleton,MyEnums}.cs`
  - `Assets/Scripts/Gameplay/Inventory/Loot.cs`、`Assets/Scripts/Gameplay/Player/Services/StatsService.cs`、`Assets/Scripts/Gameplay/Player/Models/PlayerStatsData.cs`
  - `Assets/Scripts/Pipeline/SO/GameSceneSO.cs`、`Assets/Scripts/Pipeline/Scene/{SceneDataForSave,SceneChanger}.cs`、`Assets/Scripts/Pipeline/UI/Buttons/ContinueButton.cs`
  - 资产侧：`Assets/GameSO/GameSceneSO/*.asset`(+meta)、`Assets/Scenes/GameScene/*.unity`(+meta)、`Assets/Prefabs/Inventory/{Loot,Gold,MushroomRed}.prefab`、`Assets/Scenes/GameScene/PersistentScene.unity:12626-12730`
  - 运行时存档（只读解析 72 份 `SystemSave_*.json` + 1 份 `PlayerSave_*.json`）：`%USERPROFILE%\AppData\LocalLow\YONAGI\My_ARPG`
  - 历史线索：`Docs/My_ARPG_MVCS项目现状.md:13-14,37-49,110,160,165,191,209`、`Temp/doc-discovery/asset-data-pipeline.json`、`git show e7c9ef8 --stat` 与 `git show e7c9ef8^:Assets/Scripts/Pipeline/SO/GameSceneSO.cs`
- 已核验（静态证据 + 存档文件实测）：
  - `lootsStatsDic` 只以 GUID 为键、无场景维度；`Scene1.unity` 与 `StartingMenu.unity` 的 12 个 `SaveDefinition.ID` override 集合完全一致（交集 12/12），`Scene1 ∩ Scene2 = 0`。
  - `StartingMenu.unity` 14 个 ID override 只有 12 个不同值，`335295e3-…` 出现 3 次（同场景内撞档）。
  - 三个掉落物预制体各烘焙 1 个非空 ID 且 `persistentType: 0`；全工程 `persistentType: 1`（DontPersist）零使用。
  - 72 份系统档中 28 份的场景键无法反查当前 `SaveKey`（10 个不同历史 GUID）→ 被判为不可加载；`Continue` 会静默回退。
  - 实测落盘路径 = `C:\Users\12248\AppData\LocalLow\YONAGI\My_ARPG`，与 `ProjectSettings.asset:15-16` 及 `Application.persistentDataPath` 一致。
  - 旧档缺新字段仍可反序列化（31 份缺 `playerStatsData.maxLevel`，17 份的掉落条目缺 `moved`）→ 加字段安全；全工程 0 个 `[JsonProperty]` → 公有字段名即 JSON 键。
  - `SaveLoadCanvasManager.saveType` 无 `[SerializeField]`、`SaveInfo` 无 `[Serializable]`（场景 YAML 中 `saveType:`/`saveInfo:` 各 0 处）→ 面板恒为 PlayerSave，槽位数据靠运行时重建。
- 未核验（已写入 Guide §5，均为「假设」）：打包后 `SaveKey` 退化为资产名造成编辑器/包体档互不可读；跨场景重复 ID 的运行时可见后果；10 个历史 sceneID 的来源；26 槽位是否为 UI 上限；`moved=true` 分支从未被真实游玩覆盖（73 份档中 0 次）；`WriteSave` 空单例异常；同毫秒覆盖；`Array.Sort` 字符串序等价时间序。
- 发现的缺陷（写入 Guide §4）：跨场景同 ID 共享状态；同场景 3 实例撞档；预制体烘焙非空 ID 的撞档陷阱；28 份档场景键失效；`Continue` 回到场景出生点而非玩家位置；Location 内读档仍写出新系统档；面板槽位 26 且 `GetSavesPath` 不排序；手动存档无上限无覆盖。次要：`PrepareForNewGameLoad` 与 `SaveRegistry.Clear()` 无调用点；`WriteSave` 未判空单例、未捕获 I/O 异常。
- 与旧文档冲突：`Docs/My_ARPG_MVCS项目现状.md:165` 称 `DynamicDataHandler.ClearDynamicData` 在"切往 Menu 时"触发，代码实际是 `lastSceneType == Menu && 目标为 Location`（Menu→Location，即开新局）时触发（`SaveDataManager.cs:89-98`）；以代码为准。
- 结论：Data 域权威文档已建立，可与 SceneFlow（场景键/加载入口）和 Gameplay 各子域（掉落物、数值）交叉引用。存档目录中的真实玩家数据本次只读，未做任何修改或删除。
- 维护计数：`0/5 -> 0/5`（新建业务根，未触发既有文档维护）。