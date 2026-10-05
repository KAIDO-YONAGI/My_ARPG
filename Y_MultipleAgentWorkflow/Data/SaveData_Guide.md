# 存档身份体系与序列化契约 Guide

文档 ID：`DATA-SAVEDATA-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本文件对存档文件的结构、落盘位置、身份索引、读档流程，以及改动风险负责。场景加载管线见 SceneFlow 域，掉落物生成与对象池见物品与商店域，玩家数值规则与升级结算见玩家数值域，本文件只引用它们与存档的接缝。
上游来源：
- 存档代码：`SaveSystem`、`SaveData`、`SaveMetaData`、`SaveDataManager`、`SaveDefinition`、`DynamicDataHandler`、`SaveLoadCanvasManager`
- 契约代码：`ISaveable`、`SaveRegistry`、`SaveableService`、`YSingleton`、`MyEnums`
- 关联代码：`Loot`、`StatsService`、`PlayerStatsData`、`GameSceneSO`、`SceneDataForSave`、`SceneChanger`
- 实测存档：`%USERPROFILE%\AppData\LocalLow\YONAGI\My_ARPG`，只读解析过 72 份系统档与 1 份手动档

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `存档`、`读档`、`SaveSystem`、`SaveData` | §2.1 三层结构、§2.2 落盘规则 |
| `字段改名`、`旧档兼容`、`JSON schema`、`JsonProperty` | §3 第 1~3 条 |
| `SaveKey`、`sceneID`、`场景键`、`GameSceneSO.ID` | §2.4、§3 第 4~6 条 |
| `SaveDefinition`、`lootsStatsDic`、`GUID 撞档`、`掉落实例` | §2.4、§4 |
| `ISaveable`、`SaveRegistry`、`SaveableService`、`注册/注销` | §2.1、§3 第 13~15 条 |
| `persistentDataPath`、`SaveInfo`、`DeleteSave`、`路径越界` | §2.2、§2.6 |
| `Continue`、`坏档回退`、`GetLatestLoadableSavePath` | §2.6 |
| `IsLoadingSaveRequest`、`自动存档`、`重新开始` | §2.5、§3 第 8~10 条 |
| `保存面板`、`saveLoadButtonGroups`、`手动存档` | §2.7、§4 第 7~8 条 |

## 2. 当前实现

### 2.1 三层结构：接口、静态注册表、CRTP 固定槽位

- **`ISaveable`**：契约有 3 个必需成员 `GetDataID()`、`SaveData(SaveData)`、`LoadData(SaveData)`，另有两个接口默认实现 `RegisterSaveable` 与 `UnRegisterSaveable`，内部转调静态表。全工程实现者只有 2 个：`Loot` 与 `SaveableService<T>` 的派生类 `StatsService`。
- **`SaveRegistry`**：静态 `List<ISaveable>`，`All` 以 `IReadOnlyList` 暴露，消费方需自行 `ToList()` 再遍历。`Add` 用 `Contains` 去重，`Remove` 只删第一处。`ResetStatics` 挂在 `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`，关闭 Domain Reload 时也每局清空；`Clear()` 供局间复位，当前无调用点。
- **`SaveableService<TSelf>`**：CRTP 基类，继承 `YSingleton<TSelf>`，走 `Awake` 到 `OnSingletonInitialized` 的钩子。基类在 `OnSingletonInitialized` 里注册、在 `OnDestroy` 里注销，并把 `GetDataID()` 固定返回 `null`，`null` 就是固定槽位身份的编码；服务把数据写进 `SaveData` 自己的字段，例如 `StatsService.SaveData` 写 `data.playerStatsData`。
- **`SaveDataManager`**：唯一持有运行态 `SaveData`，由 `GetData` 暴露。收集与分发共 4 个入口：`PrepareManualSaveData()` 手动存，`OnAutoSave(...)` 切场前自动存，`OnAutoLoad(...)` 场景加载完成后回灌，`LoadFromData(data)` 读档写入。`OnAutoLoad` 在 `saveable.GetDataID() == null` 时跳过，只回灌按 GUID 参与动态数据的对象；广播段 `OnAutoSave` 已把运行态抓成快照，而固定槽位服务的状态跨场景持久、读档时由 `LoadFromData` 应用，跳过它才能避免用快照里的旧值覆盖复活后的运行时状态，典型是重试复活回血 `PlayerDamageController.OnRetryRequest`。订阅关系是 `sceneLoadEventSO.LoadRequestEvent` 触发 `OnAutoSave`、`sceneLoadedEvent.SceneLoadedEvent` 触发 `OnAutoLoad`。
- **`SaveSystem`**：序列化与文件 I/O。`OnSaveEvent` 由 `DataSaveEventSO` 触发，转调 `WriteSave`。

### 2.2 落盘路径与文件名规则

- 目录固定为 `Application.persistentDataPath`。工程设置的 `companyName` 是 `YONAGI`、`productName` 是 `My_ARPG`，因此实测路径为 `C:\Users\<用户>\AppData\LocalLow\YONAGI\My_ARPG`，该目录下能读到真实存档。
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

序列化器是 Newtonsoft 的 JSON 实现 3.2.1，**全工程 0 个 `[JsonProperty]`**，只用 `JsonConvert`，因此 JSON 键名严格等于 C# 公有字段名。源码注释也声明了这一点：`PlayerStatsData` 写明字段名就是存档 JSON 的键，`SaveData` 同样以注释声明 `moved` 的旧档兼容语义。

向后兼容的实证：31 份旧档缺 `playerStatsData.maxLevel`，17 份旧档的 `lootsStatsDic` 条目缺 `moved`，例如一份 2026-08-11 的系统档里 12 条掉落记录都没有该字段，反序列化后取默认值、读档仍成功。结论是加字段安全，改名或删字段不安全。

### 2.4 双身份体系：场景键与动态物体键

- **场景键 = `GameSceneSO.SaveKey`**：编辑器下用 `AssetDatabase.FindAssets` 以场景名加 `t:SceneAsset` 查场景资产的 GUID，非编辑器下返回 `name`。`GameSceneSO.ID` 由 `OnValidate` 生成且不落盘，跨会话会变，注释明确它不可用作存档键。
  实测对应关系：`Scene1` 场景对应 `2cda990e2423bbf4892e6590ba056729`，`Scene2` 场景对应 `3f115ced3216f104b801fcb47a064dec`，`StartingMenu` 场景对应 `39b4c5c3f91378d4b922675c04cb10bf`；这些值与场景资产 GUID 一致，早期版本的旧档仍可解析。
- **反查表**：`SceneDataForSave.gameScenes` 提供场景列表，`SaveSystem.GetScene` 线性比对 `sceneID == scene.SaveKey`。
- **动态物体键 = `SaveDefinition.ID`**：`persistentType` 为 `ReadWrite(0)` 时，`OnValidate` 仅在 ID 为空时补发 GUID；为 `DontPersist(1)` 时强制清空。`Loot` 在 `Awake` 缓存该组件，`GetDataID` 返回它。全工程只有 3 个预制体带 `SaveDefinition`，即 `Loot`、`Gold`、`MushroomRed`，三者都烘焙了非空 ID 且 `persistentType: 0`。
- 运行时兜底：`Loot.RegisterSelf` 在 ID 为空时补发 GUID，因为 `OnValidate` 只在编辑器跑。

### 2.5 读档时序与 IsLoadingSaveRequest

`SaveSystem.LoadSave` 的顺序固定为：校验路径、存在性与可加载性，读全文并反序列化，交给 `SaveDataManager.LoadFromData` 全量分发到注册表，`GetScene` 反查场景，置 `IsLoadingSaveRequest = true`，调 `SceneChanger.RequestSceneLoad([scene], pos, true)`，最后置回 `false`。

`SceneChanger.RequestSceneLoad` 内先 `RaiseLoadRequestEvent` 再加载，广播同步，因此 `SaveDataManager.OnAutoSave` 整个执行都在标志位窗口内。

标志位只在 `SaveDataManager` 的一处被读取，作用是跳过「Menu 到 Location 视为开新局」的重置分支：

```csharp
bool isLoadingSaveRequest = SaveSystem.Instance.IsLoadingSaveRequest;
if (!isLoadingSaveRequest && lastSceneType == SceneType.Menu && sceneToLoadSO.sceneType == SceneType.Location)
{ dataToSave = new SaveData(); … DynamicDataHandler.ClearDynamicData(dataToSave); }
if (lastSceneType != SceneType.Menu) { … dataSavedEvent.RaiseDataSaveEvent(SystemSave); }
```

注意：省掉自动存档的直接原因是 `lastSceneType` 那一刻仍是 Menu，它的初值就是 Menu，到写档之后才更新；标志位本身不参与第二段判定。

### 2.6 坏档回退与路径守卫

- `GetLatestLoadableSavePath`：先 `GetSavesPath`，再 `Array.Sort(files)`，同目录下等价于按文件名升序即时间升序，然后 `files.LastOrDefault(IsLoadableSaveFile)` 取最新的可加载档，坏档自动向前回退。
- `IsLoadableSaveFile`：要求 `data != null && data.playerStatsData != null && GetScene(data) != null`；解析抛异常即返回 false。
- `DeleteSave` 带路径越界守卫：`Path.GetFullPath` 后必须 `StartsWith(persistentDataPath)`，否则拒绝。
- `LoadSave` 只查 `File.Exists` 与 schema 形状，任意路径下形状合法的 JSON 都会被加载。

### 2.7 保存面板 SaveLoadCanvasManager

26 个槽位由 Inspector 序列化在常驻场景里。`saveType` 字段没有 `[SerializeField]`，场景 YAML 中也没有 `saveType:` 项，故运行时恒为 `PlayerSave`。`SaveInfo` 没有 `[Serializable]`，YAML 中同样没有 `saveInfo:` 项，因此槽位路径不入盘，完全靠 `OnEnable` 与 `OnToggleCanvas` 里的 `LoadInfoToSaveList()` 重建。按钮监听在 `Start` 一次性注册。Menu 场景下 Save 按钮退化为 Delete。

## 3. 约定与硬边界

1. **改公有字段名等于改 JSON 键，会静默坏档。** 涉及 `SaveData`、`SaveMetaData`、`LootStatus`、`SceneAndPosition`、`SerializableVector3` 与 `PlayerStatsData`，前五个类型集中在同一个存档数据类型文件里。没有 `[JsonProperty]` 兜底，改名后旧档对应值读成默认值，拾取状态变成未拾取、数值归零，且不报错。加字段安全，缺失字段取默认值。
2. **`SaveType` 的成员顺序就是 JSON 里的整数。** `SystemSave` 为 0、`PlayerSave` 为 1，实测 `"saveType": 0`。插入或重排成员会让旧档被当成另一种档。
3. **`SaveType` 的成员名同时是文件名前缀。** `SaveSystem` 用枚举 `ToString()` 拼文件名，改名后 `GetSavesPath` 找不到任何旧文件，表现为存档全部消失。
4. **场景键只能用 `SaveKey`，`GameSceneSO.ID` 不可用于存档。** `ID` 由 `OnValidate` 生成且不落盘，跨会话与打包都会变。改 `SaveKey` 的算法等于让全部旧档变成不可加载，`IsLoadableSaveFile` 的 `GetScene` 条件直接失败。
5. **`SaveKey` 的编辑器分支与非编辑器分支取值不同**：编辑器取场景资产 GUID，包体取 `GameSceneSO` 资产名，因此编辑器写出的档与 Player 包写出的档互不可读。此条为推断，见 §5。
6. **`lootsStatsDic` 的键是裸 GUID，没有场景维度。** 两个场景里的掉落物只要 ID 相同就是同一条存档记录。新增场景里的掉落物实例必须逐个改 ID，在场景里覆盖 `propertyPath: ID`，否则会继承预制体烘焙的 ID，并与同源实例共用一个条目。
7. **掉落物身份的三个入口分工不同。** `SaveDefinition.OnValidate` 只在 ID 为空时补发，且只在编辑器跑；`Loot.RegisterSelf` 只在 ID 为空时补发；`Loot.AssignNewIdentity` 在池取件时强制换发新 GUID 并删掉旧 ID 的条目；`Loot.Initialize` 保留原 ID，原地重掉的同一实体沿用原 ID。改任一处都会导致条目丢失或重进场景复活。
8. **手动存档必须走 `SaveDataManager.PrepareManualSaveData()` 再 `WriteSave`。** 前者重算 `sceneIDAndPlayerPos` 并让所有注册者写进 `dataToSave`；直接 `WriteSave` 会落盘上一次的 `sceneIDAndPlayerPos`。当前唯一调用点是面板。
9. **读档入口的顺序不可调整。** `LoadFromData` 必须在 `RequestSceneLoad` 之前，且标志位要包住 `RequestSceneLoad`。从其它地方直接 `RequestSceneLoad` 而不置标志时，ReadWrite 场景会走重置分支，把刚读出的 `lootsStatsDic` 清空。
10. **`OnAutoLoad` 只回灌按 GUID 参与动态数据的对象**，判据是 `GetDataID() != null`。广播段 `OnAutoSave` 先抓快照，跳过固定槽位服务才能避免用旧快照静默撤销加载窗口内的状态变更，典型是 `PlayerDamageController.OnRetryRequest` 的复活回血。新增固定槽位服务时，状态必须在 `LoadFromData` 阶段写完整，`OnAutoLoad` 不补固定槽位服务。
11. **`dataToSave` 与运行态是同一个对象。** `WriteSave` 直接序列化 `SaveDataManager.Instance.GetData`，`Loot.SaveData` 直接改这个字典。存档边界没有快照隔离，落盘前任何对 `dataToSave` 的写入都会进文件。
12. **保存文件名只有毫秒精度。** 格式为 `yyyyMMdd_HHmmss_fff`，两次保存落在同一毫秒会互相覆盖；`WriteSave` 无 try/catch，无目录创建。
13. **注册与注销必须幂等且成对。** `SaveRegistry.Add` 靠 `Contains` 去重，`Remove` 只删第一处，重复注册要靠调用方自己的 `registered` 标记。漏注销会让静态表跨场景持有已销毁对象。
14. **静态表的生命周期由引擎控制。** `ResetStatics` 在每次进 Play 前清空，`Awake` 顺序不作为依赖。
15. **读档位置与 `Vector3.zero` 语义重叠。** `SaveSystem` 只兜底 `null`，而 `SceneChanger` 把 `(0,0,0)` 视为使用场景初始点，坐标恰为零的合法存档位置无法区分。
16. **Location 到 Location 的自动存档不记玩家真实坐标。** `SaveDataManager` 取目标场景的 `initialPosition`，真实坐标只出现在手动存档与切回 Menu 的分支。

## 4. 已知缺陷与风险

1. **跨场景同 ID 共享状态**：`lootsStatsDic` 无场景维度，`Scene1` 场景与 `StartingMenu` 场景的 12 个 `SaveDefinition.ID` 覆盖完全一致，实测交集 12/12，`Scene1` 与 `Scene2` 的交集为 0。同一 ID 在不同场景被视为同一实体。
2. **同场景内已有撞档**：`StartingMenu` 场景有 14 个 ID 覆盖但只有 12 个不同值，`335295e3-999f-454a-a2a9-9f4ce12be647` 出现 3 次，即 3 个掉落实例共用一条存档记录。
3. **预制体烘焙了非空 ID**：`Loot`、`Gold`、`MushroomRed` 三个预制体各含 1 个 `persistentType: 0` 与非空 ID，例如 `Loot` 预制体是 `ea24051a-9a9e-4601-ae4d-2f1b50dc64ee`；`SaveDefinition.OnValidate` 只在 ID 为空时补发，新拖入场景且未手改 ID 的实例必然与同源实例撞档，场景覆盖 ID 是当前唯一的防线。
4. **28/72 份系统档的场景键无法反查**：实测 10 个不同的 GUID，例如 `fd6cb3ce…` 5 份、`0d57019f…` 6 份、`41818648…` 4 份，都不在当前任何 `SaveKey` 集合内，`GetScene` 返回 null，这些档被 `IsLoadableSaveFile` 判为不可加载，`Continue` 会静默回退到更早的档。
5. **`Continue` 会回到场景出生点**：Location 到 Location 的自动存档写的是目标场景 `initialPosition`。实测 27 份 `Scene2` 档中 24 份坐标恰为 `(10.5, 8, 0)`，等于 `Scene2` 场景资产的 `initialPosition`。
6. **在 Location 内读档仍会写出一份新系统档**：`LoadSave` 走 `RequestSceneLoad` 时 `lastSceneType` 已是 Location，`SaveDataManager` 照常 `RaiseDataSaveEvent(SystemSave)`，标志位只挡住重置分支。
7. **面板槽位与排序**：`GetSavesPath` 返回 `Directory.GetFiles` 的原始顺序且不排序，`LoadInfoToSaveList` 把它直接映射到 26 个槽位，首槽未必是最新档；第 27 份起的手动档在 UI 上不可见。只有 `GetLatestLoadableSavePath` 会排序。
8. **手动存档无上限、无覆盖**：每次点击都新建时间戳文件，保存后不刷新列表，实测目录已有 72 份系统档加 1 份手动档，清理入口只有 Menu 场景的 Delete。

次要风险：`DynamicDataHandler.PrepareForNewGameLoad` 与 `SaveRegistry.Clear()` 全工程无调用点，同一进程内重新开始不会清注册表；`WriteSave` 未判空 `SaveDataManager.Instance`，也未捕获 I/O 异常。

## 5. 未核验事项

- 假设：Player 包中 `SaveKey` 退化为 `GameSceneSO` 资产名，编辑器与包体写出的存档互不可读；当前依据 `SaveKey` 的分支实现判断，运行期行为待编辑器实测。
- 假设：跨场景重复 ID 的可见后果是 `Scene1` 中拾取的掉落物在 `StartingMenu` 中同样消失或保持已拾取；依据 ID 集合实测与 `Loot.LoadData` 逻辑推断，待运行验证。
- 假设：那 10 个 GUID 来自更早的身份算法，其来源在现有材料中无法定位。
- 假设：26 个槽位是面板的可见上限；槽数来自常驻场景的序列化数据，UI 行为待编辑器实测。
- 假设：`moved` 分支从未在真实游玩中生效，73 份存档里 `"moved": true` 出现 0 次，`Loot` 的位置回写路径待实测覆盖。
- 假设：`SaveDataManager.Instance` 为 null 时 `WriteSave` 会抛 `NullReferenceException`，按 `WriteSave` 的实现推断，待触发验证。
- 假设：同一毫秒内两次保存会互相覆盖，按文件名格式推断，待验证。
- 假设：`Array.Sort` 的字符串序等价于时间序；固定宽度格式下成立，实测文件清单与时间序一致，未做代码级验证。
