# Data Router

文档 ID：`BUS-DATA`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `存档`、`读档`、`SaveData`、`SaveSystem`、`SaveFile` | `SaveData_Guide.md` §2.1、§2.2、§2.3 |
| `JSON schema`、`字段改名`、`旧档兼容`、`Newtonsoft` | `SaveData_Guide.md` §2.3、§3.1–§3.3 |
| `SaveKey`、`sceneID`、`场景键`、`SceneDataForSave` | `SaveData_Guide.md` §2.4、§3.4–§3.5 |
| `SaveDefinition`、`lootsStatsDic`、`GUID 撞档`、`掉落物 ID` | `SaveData_Guide.md` §2.4、§3.6–§3.7 |
| `ISaveable`、`SaveRegistry`、`SaveableService`、`注册/注销` | `SaveData_Guide.md` §2.1、§3.13–§3.14 |
| `persistentDataPath`、`DeleteSave`、`坏档回退`、`Continue` | `SaveData_Guide.md` §2.2、§2.6 |
| `IsLoadingSaveRequest`、`自动存档`、`手动存档`、`保存面板` | `SaveData_Guide.md` §2.5、§2.7、§3.8–§3.9 |
| `DynamicDataHandler`、`重新开始`、`局间复位` | `SaveData_Guide.md` §2.1、§2.5 |

## 下级导航

| 子类 | Router |
|---|---|
| 无 | 无 |

## 并发资源

- `workflow:Data`
- `path:Y_MultipleAgentWorkflow\Data\`
- `runtime:%USERPROFILE%\AppData\LocalLow\YONAGI\My_ARPG\`，存档文件目录；存在并发写入，只读使用

## 能力边界

**Active 能力**

- 存档三层结构：`ISaveable` 接口、`SaveRegistry` 静态注册表、`SaveableService<TSelf>` CRTP 固定槽位身份。
- 收集分发与序列化 I/O：`SaveDataManager`、`SaveSystem`。
- 落盘规则：`Application.persistentDataPath` 加 `{SaveType}_{yyyyMMdd_HHmmss_fff}` 形式的 JSON 文件；手动档 `PlayerSave` 与系统档 `SystemSave` 分离。
- 双身份体系：场景键取 `GameSceneSO.SaveKey`，即场景资产的 GUID；动态物体键取 `SaveDefinition.ID`。
- 坏档回退用 `IsLoadableSaveFile` 与 `LastOrDefault`；`DeleteSave` 带路径越界守卫。
