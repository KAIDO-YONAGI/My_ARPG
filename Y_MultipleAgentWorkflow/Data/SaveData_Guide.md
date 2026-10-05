# 存档身份体系与序列化契约 Guide

文档 ID：`DATA-SAVEDATA-GUIDE`
状态：`Active`
最后更新：`2026-10-05`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `存档`、`读档`、`SaveSystem`、`SaveData` | §2.1 三层结构、§2.2 落盘规则 |
| `字段改名`、`旧档兼容`、`JSON schema`、`JsonProperty` | §3 第 1~3 条 |
| `SaveKey`、`sceneID`、`场景键`、`GameSceneSO.ID` | §2.4、§3 第 4~6 条 |
| `SaveDefinition`、`lootsStatsDic`、`GUID 撞档`、`掉落实例` | §2.4 |
| `ISaveable`、`SaveRegistry`、`SaveableService`、`注册/注销` | §2.1、§3 第 13~15 条 |
| `persistentDataPath`、`SaveInfo`、`DeleteSave`、`路径越界` | §2.2、§2.6 |
| `Continue`、`坏档回退`、`GetLatestLoadableSavePath` | §2.6 |
| `IsLoadingSaveRequest`、`自动存档`、`重新开始` | §2.5、§3 第 8~10 条 |
| `保存面板`、`saveLoadButtonGroups`、`手动存档` | §2.7 |

## 2. 当前实现

### 2.1 三层结构：接口、静态注册表、CRTP 固定槽位

- **`ISaveable`**：契约有 3 个必需成员 `GetDataID()`、`SaveData(SaveData)`、`LoadData(SaveData)`，另有两个接口默认实现 `RegisterSaveable` 与 `UnRegisterSaveable`，内部转调静态表。全工程实现者只有 2 个：`Loot` 与 `SaveableService<T>` 的派生类 `StatsService`。
- **`SaveRegistry`**：静态 `List<ISaveable>`，`All` 以 `IReadOnlyList` 暴露，消费方需自行 `ToList()` 再遍历。`Add` 用 `Contains` 去重，`Remove` 只删第一处。`ResetStatics` 挂在 `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`，关闭 Domain Reload 时也每局清空；`Clear()` 供局间复位，当前无调用点。
- **`SaveableService<TSelf>`**：CRTP 基类，继承 `YSingleton<TSelf>`，走 `Awake` 到 `OnSingletonInitialized` 的钩子。基类在 `OnSingletonInitialized` 里注册、在 `OnDestroy` 里注销，并把 `GetDataID()` 固定返回 `null`，`null` 就是固定槽位身份的编码；服务把数据写进 `SaveData` 自己的字段，例如 `StatsService.SaveData` 写 `data.playerStatsData`。
- **`SaveDataManager`**：唯一持有运行态 `SaveData`，由 `GetData` 暴露。收集与分发共 4 个入口：`PrepareManualSaveData()` 手动存，`OnAutoSave(...)` 切场前自动存，`OnAutoLoad(...)` 场景加载完成后回灌，`LoadFromData(data)` 读档写入。`OnAutoLoad` 在 `saveable.GetDataID() == null` 时跳过，只回灌按 GUID 参与动态数据的对象；固定槽位服务的状态跨场景持久，读档时由 `LoadFromData` 应用。订阅关系是 `sceneLoadEventSO.LoadRequestEvent` 触发 `OnAutoSave`、`sceneLoadedEvent.SceneLoadedEvent` 触发 `OnAutoLoad`。
- **`SaveSystem`**：序列化与文件 I/O。`OnSaveEvent` 由 `DataSaveEventSO` 触发，转调 `WriteSave`。

### 2.2 落盘路径与文件名规则

- 目录固定为 `Application.persistentDataPath`。工程设置的 `companyName` 是 `YONAGI`、`productName` 是 `My_ARPG`，路径为 `C:\Users\<用户>\AppData\LocalLow\YONAGI\My_ARPG`。
- 文件名由 `{saveType}_{saveID}` 加 JSON 扩展名构成，`saveID` 取 `DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")`；`saveType` 是枚举插值 `Enum.ToString()`，取字面量 `SystemSave` 与 `PlayerSave`。
- 读取用同一模式：`Directory.GetFiles` 以 `{saveType}_` 通配加 JSON 扩展名过滤 `persistentDataPath`。
- **手动档与系统档分离**：`SystemSave` 由自动存档经 `SaveDataManager` 到 `DataSaveEventSO` 再到 `SaveSystem.WriteSave` 写出；`PlayerSave` 只由保存面板写出。两者互不覆盖，Continue 按钮只认 `SystemSave`，面板只列 `PlayerSave`。
- 内容为 `JsonConvert.SerializeObject(save, Formatting.Indented)`，无加密、无压缩、无版本号字段。

### 2.3 JSON schema 即契约

顶层 `SaveFile` 由 `saveInfo` 与 `data` 两段组成：

```
saveInfo: { saveID, saveType }
data: {
  lootsStatsDic: { "<GUID>": { position{x,y,z}, hasBeenPicked, moved } },
  sceneIDAndPlayerPos: { sceneID, position{x,y,z} },
  playerStatsData: { damage … maxLevel }
}
```

序列化器是 Newtonsoft 的 JSON 实现 3.2.1，全工程 0 个 `[JsonProperty]`，只用 `JsonConvert`，JSON 键名严格等于 C# 公有字段名。加字段安全，缺失字段取默认值。

### 2.4 双身份体系：场景键与动态物体键

- **场景键 = `GameSceneSO.SaveKey`**：编辑器下用 `AssetDatabase.FindAssets` 以场景名加 `t:SceneAsset` 查场景资产的 GUID，非编辑器下返回 `name`。`GameSceneSO.ID` 由 `OnValidate` 生成且不落盘，跨会话会变，不可用作存档键。
- **反查表**：`SceneDataForSave.gameScenes` 提供场景列表，`SaveSystem.GetScene` 线性比对 `sceneID == scene.SaveKey`。
- **动态物体键 = `SaveDefinition.ID`**：`persistentType` 为 `ReadWrite(0)` 时，`OnValidate` 仅在 ID 为空时补发 GUID；为 `DontPersist(1)` 时强制清空。`Loot` 在 `Awake` 缓存该组件，`GetDataID` 返回它。全工程只有 3 个预制体带 `SaveDefinition`，即 `Loot`、`Gold`、`MushroomRed`，三者都烘焙了非空 ID 且 `persistentType: 0`。
- 运行时兜底：`Loot.RegisterSelf` 在 ID 为空时补发 GUID，`OnValidate` 只在编辑器跑。

### 2.5 读档时序与 IsLoadingSaveRequest

`SaveSystem.LoadSave` 的顺序固定为：校验路径、存在性与可加载性，读全文并反序列化，交给 `SaveDataManager.LoadFromData` 全量分发到注册表，`GetScene` 反查场景，置 `IsLoadingSaveRequest = true`，调 `SceneChanger.RequestSceneLoad([scene], pos, true)`，最后置回 `false`。

`SceneChanger.RequestSceneLoad` 内先 `RaiseLoadRequestEvent` 再加载，广播同步，`SaveDataManager.OnAutoSave` 整个执行都在标志位窗口内。

标志位只在 `SaveDataManager` 的一处被读取，作用是跳过「Menu 到 Location 视为开新局」的重置分支：

```csharp
bool isLoadingSaveRequest = SaveSystem.Instance.IsLoadingSaveRequest;
if (!isLoadingSaveRequest && lastSceneType == SceneType.Menu && sceneToLoadSO.sceneType == SceneType.Location)
{ dataToSave = new SaveData(); … DynamicDataHandler.ClearDynamicData(dataToSave); }
if (lastSceneType != SceneType.Menu) { … dataSavedEvent.RaiseDataSaveEvent(SystemSave); }
```

`lastSceneType` 的初值是 Menu，到写档之后才更新。

### 2.6 坏档回退与路径守卫

- `GetLatestLoadableSavePath`：先 `GetSavesPath`，再 `Array.Sort(files)`，同目录下等价于按文件名升序即时间升序，然后 `files.LastOrDefault(IsLoadableSaveFile)` 取最新的可加载档，坏档自动向前回退。
- `IsLoadableSaveFile`：要求 `data != null && data.playerStatsData != null && GetScene(data) != null`；解析抛异常即返回 false。
- `DeleteSave` 带路径越界守卫：`Path.GetFullPath` 后必须 `StartsWith(persistentDataPath)`，否则拒绝。
- `LoadSave` 只查 `File.Exists` 与 schema 形状。

### 2.7 保存面板 SaveLoadCanvasManager

26 个槽位由 Inspector 序列化在常驻场景里。`saveType` 字段没有 `[SerializeField]`，场景 YAML 中也没有 `saveType:` 项，运行时恒为 `PlayerSave`。`SaveInfo` 没有 `[Serializable]`，YAML 中同样没有 `saveInfo:` 项，槽位路径不入盘，靠 `OnEnable` 与 `OnToggleCanvas` 里的 `LoadInfoToSaveList()` 重建。按钮监听在 `Start` 一次性注册。Menu 场景下 Save 按钮退化为 Delete。

## 3. 约定与硬边界

1. **改公有字段名等于改 JSON 键。** 涉及 `SaveData`、`SaveMetaData`、`LootStatus`、`SceneAndPosition`、`SerializableVector3` 与 `PlayerStatsData`，前五个类型集中在同一个存档数据类型文件里。没有 `[JsonProperty]` 兜底，改名后旧档对应值读成默认值。加字段安全，缺失字段取默认值。
2. **`SaveType` 的成员顺序就是 JSON 里的整数。** `SystemSave` 为 0、`PlayerSave` 为 1，存档 JSON 中写为 `"saveType": 0`。插入或重排成员会让旧档被当成另一种档。
3. **`SaveType` 的成员名同时是文件名前缀。** `SaveSystem` 用枚举 `ToString()` 拼文件名，改名后 `GetSavesPath` 找不到任何旧文件。
4. **场景键只能用 `SaveKey`，`GameSceneSO.ID` 不可用于存档。** `ID` 由 `OnValidate` 生成且不落盘，跨会话与打包都会变。改 `SaveKey` 的算法等于让全部旧档变成不可加载，`IsLoadableSaveFile` 的 `GetScene` 条件直接失败。
5. **`SaveKey` 的编辑器分支与非编辑器分支取值不同**：编辑器取场景资产 GUID，包体取 `GameSceneSO` 资产名。
6. **`lootsStatsDic` 的键是裸 GUID，没有场景维度。** 两个场景里的掉落物只要 ID 相同就是同一条存档记录。新增场景里的掉落物实例必须逐个改 ID，在场景里覆盖 `propertyPath: ID`，否则会继承预制体烘焙的 ID，并与同源实例共用一个条目。
7. **掉落物身份的三个入口分工不同。** `SaveDefinition.OnValidate` 只在 ID 为空时补发，且只在编辑器跑；`Loot.RegisterSelf` 只在 ID 为空时补发；`Loot.AssignNewIdentity` 在池取件时强制换发新 GUID 并删掉旧 ID 的条目；`Loot.Initialize` 保留原 ID，原地重掉的同一实体沿用原 ID。
8. **手动存档必须走 `SaveDataManager.PrepareManualSaveData()` 再 `WriteSave`。** 前者重算 `sceneIDAndPlayerPos` 并让所有注册者写进 `dataToSave`；直接 `WriteSave` 会落盘上一次的 `sceneIDAndPlayerPos`。当前唯一调用点是面板。
9. **读档入口的顺序不可调整。** `LoadFromData` 必须在 `RequestSceneLoad` 之前，且标志位要包住 `RequestSceneLoad`。从其它地方直接 `RequestSceneLoad` 而不置标志时，ReadWrite 场景会走重置分支，把刚读出的 `lootsStatsDic` 清空。
10. **`OnAutoLoad` 只回灌按 GUID 参与动态数据的对象**，判据是 `GetDataID() != null`。广播段 `OnAutoSave` 先抓快照。新增固定槽位服务时，状态必须在 `LoadFromData` 阶段写完整，`OnAutoLoad` 不补固定槽位服务。
11. **`dataToSave` 与运行态是同一个对象。** `WriteSave` 直接序列化 `SaveDataManager.Instance.GetData`，`Loot.SaveData` 直接改这个字典。落盘前任何对 `dataToSave` 的写入都会进文件。
12. **保存文件名只有毫秒精度。** 格式为 `yyyyMMdd_HHmmss_fff`，两次保存落在同一毫秒会互相覆盖。
13. **注册与注销必须幂等且成对。** `SaveRegistry.Add` 靠 `Contains` 去重，`Remove` 只删第一处，重复注册要靠调用方自己的 `registered` 标记。
14. **静态表的生命周期由引擎控制。** `ResetStatics` 在每次进 Play 前清空，`Awake` 顺序不作为依赖。
15. **读档位置与 `Vector3.zero` 语义重叠。** `SaveSystem` 只兜底 `null`，而 `SceneChanger` 把 `(0,0,0)` 视为使用场景初始点。
16. **Location 到 Location 的自动存档不记玩家真实坐标。** `SaveDataManager` 取目标场景的 `initialPosition`，真实坐标只出现在手动存档与切回 Menu 的分支。
