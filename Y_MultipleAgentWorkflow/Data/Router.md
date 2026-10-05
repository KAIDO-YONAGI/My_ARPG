# Data Router

文档 ID：`BUS-DATA`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `存档`、`读档`、`SaveData`、`SaveSystem`、`SaveFile` | `SaveData_Guide.md` |
| `JSON schema`、`字段改名`、`旧档兼容`、`Newtonsoft` | `SaveData_Guide.md` |
| `SaveKey`、`sceneID`、`场景键`、`SceneDataForSave` | `SaveData_Guide.md` |
| `SaveDefinition`、`lootsStatsDic`、`GUID 撞档`、`掉落物 ID` | `SaveData_Guide.md` |
| `ISaveable`、`SaveRegistry`、`SaveableService`、`注册/注销` | `SaveData_Guide.md` |
| `persistentDataPath`、`DeleteSave`、`坏档回退`、`Continue` | `SaveData_Guide.md` |
| `IsLoadingSaveRequest`、`自动存档`、`手动存档`、`保存面板` | `SaveData_Guide.md` |
| `DynamicDataHandler`、`重新开始`、`局间复位` | `SaveData_Guide.md` |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Data`
- `path:Y_MultipleAgentWorkflow\Data\`
- `runtime:%USERPROFILE%\AppData\LocalLow\YONAGI\My_ARPG\`，存档文件目录，核验时可读；存在并发写入，只读使用

## 能力边界

**Active：事实确证，可直接引用**

- 存档三层结构：`ISaveable` 接口、`SaveRegistry` 静态注册表、`SaveableService<TSelf>` CRTP 固定槽位身份。
- 收集分发与序列化 I/O：`SaveDataManager`、`SaveSystem`。
- 落盘规则：`Application.persistentDataPath` 加 `{SaveType}_{yyyyMMdd_HHmmss_fff}` 形式的 JSON 文件；手动档 `PlayerSave` 与系统档 `SystemSave` 分离。
- 双身份体系：场景键取 `GameSceneSO.SaveKey`，即场景资产的 GUID；动态物体键取 `SaveDefinition.ID`。
- 坏档回退用 `IsLoadableSaveFile` 与 `LastOrDefault`；`DeleteSave` 带路径越界守卫。
- 确证缺陷：跨场景与同场景内 `SaveDefinition.ID` 重复、预制体烘焙非空 ID、28/72 份存档的场景键无法反查。

**Proposal：尚未实施，不作为现状**

- 任务、物品栏、背包三域接入动态存档，`SaveDataManager` 内有 TODO 注释。
- `SaveRegistry.Clear()` 的局间复位调用点，当前无调用方。
- `DynamicDataHandler.PrepareForNewGameLoad` 的接入，当前无调用方。

**需要用户确认的事项**

- 存档目录中的 72 份系统档与 28 份不可加载档是否清理；涉及真实玩家数据，本域不擅自删除。
- 修改任何 JSON 字段名、`SaveKey` 算法或 `SaveType` 成员名之前，先确认旧档保留策略，这三类改动都会静默坏档。
- 面板槽位上限 26 与手动存档无上限的增长策略是否需要调整。
