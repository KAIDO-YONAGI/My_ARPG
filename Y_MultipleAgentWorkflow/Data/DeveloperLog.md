# Data Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Data 权威文档

- 任务：为 Data 域建立权威 Guide，记录存档身份体系与序列化契约，并按项目中文模板重写 Router。
- 写入的文件：
  - `SaveData_Guide.md`，新建，文档 ID `DATA-SAVEDATA-GUIDE`，状态 Active。
  - `Router.md`，重写为中文模板，无下级子类。
  - `DeveloperLog.md`，本条目。
- 依据：
  - 存档代码：`SaveSystem`、`SaveData`、`SaveDataManager`、`SaveDefinition`、`DynamicDataHandler`、`SaveLoadCanvasManager`。
  - 契约代码：`ISaveable`、`SaveRegistry`、`SaveableService`、`YSingleton`、`MyEnums`。
  - 使用方代码：`Loot`、`StatsService`、`PlayerStatsData`。
  - 场景管线代码：`GameSceneSO`、`SceneDataForSave`、`SceneChanger`、`ContinueButton`。
  - 资产：`GameSceneSO` 资产、`GameScene` 场景、常驻场景、`Loot`、`Gold`、`MushroomRed` 预制体。
  - 运行时存档目录 `%USERPROFILE%\AppData\LocalLow\YONAGI\My_ARPG`，只读解析 72 份 `SystemSave_*.json` 与 1 份 `PlayerSave_*.json`。
- 已核验：
  - `lootsStatsDic` 以 GUID 为键，没有场景维度；`Scene1` 与 `StartingMenu` 两个场景的 12 个 `SaveDefinition.ID` 覆盖集合完全一致，`Scene1` 与 `Scene2` 的交集为 0。
  - `StartingMenu` 场景的 14 个 ID 覆盖只有 12 个不同值，`335295e3-…` 出现 3 次。
  - 三个掉落物预制体各烘焙 1 个非空 ID，`persistentType` 为 `0`；全工程没有 `persistentType` 为 `1` 的配置。
  - 72 份系统档中 28 份的场景键在当前 `SaveKey` 中反查不到，涉及 10 个历史 GUID，这些档被 `IsLoadableSaveFile` 判为不可加载，`Continue` 回退到较新的可加载档。
  - 实测落盘路径为 `C:\Users\12248\AppData\LocalLow\YONAGI\My_ARPG`，与播放器设置里的 `companyName`、`productName` 及 `Application.persistentDataPath` 一致。
  - 旧档缺新字段仍可反序列化，31 份缺 `playerStatsData.maxLevel`，17 份的掉落条目缺 `moved`；全工程没有 `[JsonProperty]`，公有字段名即 JSON 键。
  - `SaveLoadCanvasManager.saveType` 没有 `[SerializeField]`，`SaveInfo` 没有 `[Serializable]`，场景 YAML 中没有 `saveType:` 与 `saveInfo:` 项，面板恒为 `PlayerSave`，槽位数据靠运行时重建。
- 结论：Data 域权威文档建立完成，与 SceneFlow 的场景键与加载入口、Gameplay 的掉落物与数值子域交叉引用。存档目录中的真实玩家数据本轮只读。
- 维护计数：`0/5`，新建业务根，未触发既有文档维护。
