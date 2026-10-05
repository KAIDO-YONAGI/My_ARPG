# 存档身份体系与序列化契约 Guide

文档 ID：`DATA-SAVEDATA-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本文件对「存档文件长什么样、存在哪、用什么身份索引、读档时发生什么、改什么会坏档」负责；**不负责**场景加载管线（见 `SceneFlow\`）、掉落物生成与对象池（见 `Gameplay\InventoryShop\`）、玩家数值规则与升级结算（见 `Gameplay\PlayerStats\`），只引用它们与存档的接缝。
上游来源：
- 代码：`Assets/Scripts/Gameplay/Save/`（`SaveSystem.cs`、`SaveData.cs`、`SaveDataManager.cs`、`SaveDefinition.cs`、`DynamicDataHandler.cs`、`SaveLoadCanvasManager.cs`）
- 代码：`Assets/Scripts/Contracts/{ISaveable,SaveRegistry,SaveableService,YSingleton,MyEnums}.cs`
- 代码：`Assets/Scripts/Gameplay/Inventory/Loot.cs`、`Assets/Scripts/Gameplay/Player/Services/StatsService.cs`、`Assets/Scripts/Gameplay/Player/Models/PlayerStatsData.cs`
- 代码：`Assets/Scripts/Pipeline/SO/GameSceneSO.cs`、`Assets/Scripts/Pipeline/Scene/{SceneDataForSave,SceneChanger}.cs`
- 实测存档：`%USERPROFILE%\AppData\LocalLow\YONAGI\My_ARPG`（本次只读解析 72 份 `SystemSave_*.json` + 1 份 `PlayerSave_*.json`）
- 历史线索：`Docs/My_ARPG_MVCS项目现状.md:19-20,45-55,116,198,216`（该文件本轮有未提交改动，行号已按 2026-10-05 工作区版本复核）、`Temp/doc-discovery/asset-data-pipeline.json`、git 提交 `e7c9ef8`

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

### 2.1 三层结构（接口 / 静态注册表 / CRTP 固定槽位）

- **`ISaveable`（`Assets/Scripts/Contracts/ISaveable.cs:1-18`）**：契约只有 3 个必需成员——`GetDataID()`（:4）、`SaveData(SaveData)`（:16）、`LoadData(SaveData)`（:17），外加两个**接口默认实现** `RegisterSaveable/UnRegisterSaveable`（:7-14），内部转调静态表。全工程实现者只有 2 个：`Loot`（`Loot.cs:7`）与 `SaveableService<T>` 的派生类 `StatsService`（`StatsService.cs:16`）。
- **`SaveRegistry`（`Contracts/SaveRegistry.cs:11-31`）**：`static List<ISaveable>`；`All` 以 `IReadOnlyList` 暴露（:16），消费方必须自行 `ToList()` 再遍历（`SaveDataManager.cs:64,81,122,145`）。`Add` 用 `Contains` 去重（:21-25），`Remove` 只删第一处（:27）。`ResetStatics` 挂在 `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`（:18-19），保证关闭 Domain Reload 时也每局清空；`Clear()`（:30）供局间复位，**当前无调用点**。
- **`SaveableService<TSelf>`（`Contracts/SaveableService.cs:11-30`）**：CRTP 基类，继承 `YSingleton<TSelf>`（`YSingleton.cs` 的 `Awake` → `OnSingletonInitialized` 钩子）。基类在 `OnSingletonInitialized` 里注册（:14-17）、在 `OnDestroy` 里注销（:19-23），并把 `GetDataID()` 固定返回 `null`（:25）——**`null` 就是"固定槽位身份"的编码**，服务把数据写进 `SaveData` 自己的字段（如 `StatsService.SaveData` 写 `data.playerStatsData`，`StatsService.cs:59`）。
- **`SaveDataManager`（`SaveDataManager.cs:12`）**：唯一持有运行态 `SaveData`（:25，`GetData` 暴露于 :15）。收集/分发共 4 个入口：`PrepareManualSaveData()`（:48-70，手动存）、`OnAutoSave(...)`（:71-118，切场前自动存）、`OnAutoLoad(...)`（:120-134，场景加载完成后回灌：`:129-130` 的 `if (saveable.GetDataID() == null) continue;` 跳过 `SaveableService` 派生的固定槽位服务，只回灌按 GUID 参与动态数据的对象。守卫的必要性：广播段 `OnAutoSave` 已把运行态抓成快照，而固定槽位服务的状态跨场景持久、读档时已由 `LoadFromData` 应用；不跳过就会把快照里的旧值覆盖回复活后的运行时状态——典型是重试复活回血 `PlayerDamageController.cs:48-57`）、`LoadFromData(data)`（:136-149，读档写入）。订阅关系在 :28-37：`sceneLoadEventSO.LoadRequestEvent → OnAutoSave`、`sceneLoadedEvent.SceneLoadedEvent → OnAutoLoad`。
- **`SaveSystem`（`SaveSystem.cs:29`）**：序列化与文件 I/O。`OnSaveEvent`（:44-47）由 `DataSaveEventSO` 触发（:36-43 订阅），转调 `WriteSave`。

### 2.2 落盘路径与文件名规则

- 目录固定为 `Application.persistentDataPath`（`SaveSystem.cs:55,179`）。由 `ProjectSettings/ProjectSettings.asset:15-16`（`companyName: YONAGI` / `productName: My_ARPG`）推出实测路径 `C:\Users\<用户>\AppData\LocalLow\YONAGI\My_ARPG`（本次已在该目录读到真实档）。
- 文件名 = `{saveType}_{saveID}.json`（`SaveSystem.cs:55`），`saveID = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")`（:50）。`saveType` 是枚举插值 = `Enum.ToString()`，即字面量 `SystemSave` / `PlayerSave`。
- 读取用同一模式：`Directory.GetFiles(persistentDataPath, $"{saveType}_*.json")`（:179）。
- **手动档与系统档分离**：`SystemSave` 由自动存档写（`SaveDataManager.cs:114` → 事件 → `SaveSystem.WriteSave`）；`PlayerSave` 只由保存面板写（`SaveLoadCanvasManager.cs:159`）。两者互不覆盖，Continue 按钮只认 `SystemSave`（`ContinueButton.cs:13-14`），面板只列 `PlayerSave`（`SaveLoadCanvasManager.cs:23,113`）。
- 内容为 `JsonConvert.SerializeObject(save, Formatting.Indented)`（:54），无加密、无压缩、无版本号字段。

### 2.3 JSON schema 即契约

顶层 `SaveFile`（`SaveSystem.cs:19-28`）= `{ saveInfo, data }`：

```
saveInfo: { saveID, saveType }                       // SaveData.cs:13-22
data: {
  lootsStatsDic: { "<GUID>": { position{x,y,z}, hasBeenPicked, moved } },  // SaveData.cs:9,34-50
  sceneIDAndPlayerPos: { sceneID, position{x,y,z} },                       // SaveData.cs:10,52-63
  playerStatsData: { damage … maxLevel }                                   // PlayerStatsData.cs:14-29
}
```

序列化器是 Newtonsoft.Json 3.2.1（`Packages/manifest.json:12`），**全工程 0 个 `[JsonProperty]`**（仅 `SaveSystem.cs:5,54,122,187` 使用 `JsonConvert`），因此 JSON 键名严格等于 C# 公有字段名。这一点被源码注释显式声明：`PlayerStatsData.cs:5-10` 写明"字段名就是存档 JSON 的键"（`SaveData.cs:39-42` 同样以注释声明 `moved` 的旧档兼容语义）。
向后兼容的实证：31 份旧档缺 `playerStatsData.maxLevel`，17 份旧档的 `lootsStatsDic` 条目缺 `moved`（如 `SystemSave_20260811_191455_390.json`，12 条掉落记录都没有该字段），反序列化后取默认值、读档仍成功——**加字段安全，改名/删字段不安全**。

### 2.4 双身份体系（场景键 / 动态物体键）

- **场景键 = `GameSceneSO.SaveKey`**（`GameSceneSO.cs:31-50`）：编辑器下用 `AssetDatabase.FindAssets($"{sceneName} t:SceneAsset")` 取 `.unity` 文件的 asset GUID（:38-45）；非编辑器下返回 `name`（:48）。`GameSceneSO.ID`（:13）**明确不是存档键**：由 `OnValidate` 生成且不落盘（:52-55），跨会话会变，注释见 :12。
  实测对应关系：`Scene1.unity` = `2cda990e2423bbf4892e6590ba056729`、`Scene2.unity` = `3f115ced3216f104b801fcb47a064dec`、`StartingMenu.unity` = `39b4c5c3f91378d4b922675c04cb10bf`；与 `e7c9ef8^` 版本 `GameSceneSO.SaveKey => sceneReference.AssetGUID` 的取值一致，故 Addressables 时代的旧档仍可解析。
- **反查表**：`SceneDataForSave.gameScenes`（`SceneDataForSave.cs:6`）→ `SaveSystem.GetScene` 线性比对 `sceneID == scene.SaveKey`（`SaveSystem.cs:199-212`）。
- **动态物体键 = `SaveDefinition.ID`**（`SaveDefinition.cs:4-22`）：`persistentType = ReadWrite(0)` 时 `OnValidate` 仅在 ID 为空时补发 GUID（:11-17），`DontPersist(1)` 时强制清空（:18-21）。`Loot` 在 `Awake` 缓存该组件（`Loot.cs:42`），`GetDataID` 返回它（:198-202）。全工程只有 3 个预制体带 `SaveDefinition`：`Assets/Prefabs/Inventory/{Loot,Gold,MushroomRed}.prefab`，三者都烘焙了非空 ID 且 `persistentType: 0`。
- 运行时兜底：`Loot.RegisterSelf`（:86-98）在 ID 为空时补发 GUID（:93）——因为 `OnValidate` 只在编辑器跑。

### 2.5 读档时序与 `IsLoadingSaveRequest`

`SaveSystem.LoadSave`（:99-155）顺序固定为：校验路径/存在性/可加载性（:101-117）→ `File.ReadAllText` + 反序列化（:121-122）→ `SaveDataManager.LoadFromData`（:124，全量分发到注册表）→ `GetScene` 反查（:126）→ `IsLoadingSaveRequest = true`（:133）→ `SceneChanger.RequestSceneLoad([scene], pos, true)`（:136）→ 置回 `false`（:137）。
`SceneChanger.RequestSceneLoad` 内是 `RaiseLoadRequestEvent` 后接加载（`SceneChanger.cs:86-90`），**广播同步**，因此 `SaveDataManager.OnAutoSave` 整个执行都在标志位窗口内。
标志位只在 `SaveDataManager.cs:88` 被读取一次，作用是跳过"Menu→Location 视为开新局"的重置分支（:89-98）：
```csharp
bool isLoadingSaveRequest = SaveSystem.Instance.IsLoadingSaveRequest;
if (!isLoadingSaveRequest && lastSceneType == SceneType.Menu && sceneToLoadSO.sceneType == SceneType.Location)
{ dataToSave = new SaveData(); … DynamicDataHandler.ClearDynamicData(dataToSave); }   // :89-98
if (lastSceneType != SceneType.Menu) { … dataSavedEvent.RaiseDataSaveEvent(SystemSave); }  // :100-116
```
注意：省掉自动存档的直接原因是 `lastSceneType` 那一刻**仍是 Menu**（:27 初值 Menu，:117 才更新），标志位本身不参与 :100 的判定。

### 2.6 坏档回退与路径守卫

- `GetLatestLoadableSavePath`（:156-176）：`GetSavesPath` → `Array.Sort(files)`（:166，同目录下等价于按文件名升序 = 时间升序）→ `files.LastOrDefault(IsLoadableSaveFile)`（:168）取**最新的可加载档**，坏档自动向前回退。
- `IsLoadableSaveFile`（:181-197）：要求 `data != null && data.playerStatsData != null && GetScene(data) != null`；解析抛异常即 `false`（:192-196）。
- `DeleteSave`（:63-96）**有**路径越界守卫：`Path.GetFullPath` 后必须 `StartsWith(persistentDataPath)`（:71-77），否则拒绝。
- `LoadSave` **没有**对称守卫：只查 `File.Exists`（:107）+ schema 形状（:113），任意路径下形状合法的 JSON 都会被加载。

### 2.7 保存面板（`SaveLoadCanvasManager`）

26 个槽位由 Inspector 序列化（`PersistentScene.unity:12634` 起 26 条 `saveLoadButtonGroups`）。`saveType` 字段（`SaveLoadCanvasManager.cs:23`）**没有 `[SerializeField]`**，场景 YAML 中也不存在 `saveType:`（实测 0 处），故运行时恒为 `PlayerSave`。`SaveInfo`（:36-48）**没有 `[Serializable]`**，YAML 中同样无 `saveInfo:`（实测 0 处），因此槽位路径不入盘，完全靠 `OnEnable`/`OnToggleCanvas` 里的 `LoadInfoToSaveList()` 重建（:61-69,85-101,109-133）。按钮监听在 `Start` 一次性注册（:49-59）。Menu 场景下 Save 按钮退化为 Delete（:139-149,169-183）。

## 3. 约定与硬边界

1. **改公有字段名 = 改 JSON 键 = 静默坏档。** 涉及 `SaveData.cs:9-11`（`SaveData`）、`SaveData.cs:15-16`（`SaveMetaData`）、`SaveData.cs:37-42`（`LootStatus`）、`SaveData.cs:56-57`（`SceneAndPosition`）、`SaveData.cs:28`（`SerializableVector3`）、`PlayerStatsData.cs:14-29`；四个类型都定义在同一个文件 `SaveData.cs` 里。没有 `[JsonProperty]` 兜底，改名后旧档对应值读成默认值（拾取状态变"未拾取"、数值归零），**不报错**。加字段安全（缺失 → 默认值）。
2. **`SaveType` 的成员顺序就是 JSON 里的整数。** `SystemSave=0 / PlayerSave=1`（`MyEnums.cs:74-78`），实测 `"saveType": 0`。插入或重排成员会让旧档被当成另一种档。
3. **`SaveType` 的成员名同时是文件名前缀。** `SaveSystem.cs:55,179` 用枚举 `ToString()` 拼文件名，改名后 `GetSavesPath` 找不到任何旧文件 → 表现为"存档全没了"。
4. **场景键只能用 `SaveKey`，绝不能用 `GameSceneSO.ID`。** `ID` 由 `OnValidate` 生成且不落盘（`GameSceneSO.cs:12,52-55`），跨会话/打包会变。改 `SaveKey` 的算法等于让全部旧档变成不可加载（§2.6 的第 2 个条件直接失败）。
5. **`SaveKey` 的编辑器分支与非编辑器分支取值不同**（`.unity` GUID vs `GameSceneSO` 资产文件名，`GameSceneSO.cs:38-48`）——编辑器写出的档与 Player 包写出的档互不可读（同为推断，见 §5）。
6. **`lootsStatsDic` 的键是裸 GUID，没有场景维度**（`SaveData.cs:9`）。两个场景里的掉落物只要 ID 相同就是同一条存档记录。新增场景里的掉落物实例**必须**逐个改 ID（场景 YAML 的 `propertyPath: ID` override），否则会继承预制体烘焙的 ID 并与同源实例共用一个条目。
7. **掉落物身份的三个入口不要混用**：`SaveDefinition.OnValidate` 只在 ID 为空时补发（编辑器）；`Loot.RegisterSelf` 只在 ID 为空时补发（:93）；`Loot.AssignNewIdentity` 在池取件时**强制换发新 GUID 并删掉旧 ID 的条目**（:112-123）；`Loot.Initialize` **故意不换发**（:151-153，注释说明原地重掉的同一实体必须保留 ID）。改动其中任一处都会导致"条目丢失"或"重进场景复活"。
8. **手动存档必须走 `SaveDataManager.PrepareManualSaveData()` 再 `WriteSave`，不能只调 `WriteSave`。** 前者重算 `sceneIDAndPlayerPos`（`SaveDataManager.cs:62`）并让所有注册者写进 `dataToSave`（:64-67）；直接 `WriteSave` 会落盘上一次的 `sceneIDAndPlayerPos`。当前唯一调用点是面板（`SaveLoadCanvasManager.cs:154,159`）。
9. **读档入口的顺序不可调整**：`LoadFromData` 必须在 `RequestSceneLoad` 之前（`SaveSystem.cs:124,136`），且标志位要包住 `RequestSceneLoad`（:133-137）。若从其它地方直接 `RequestSceneLoad` 而不置标志，ReadWrite 场景会走 :89-98 的重置分支，把刚读出的 `lootsStatsDic` 清空。
10. **`OnAutoLoad` 只回灌"按 GUID 参与动态数据"的对象**（`GetDataID() != null`，`SaveDataManager.cs:129-130`）。这是工作区本轮新增的守卫：广播段（`OnAutoSave`）先抓快照，跳过固定槽位服务才能避免回灌用旧快照静默撤销加载窗口内的状态变更——典型是 `PlayerDamageController.OnRetryRequest` 的复活回血（`PlayerDamageController.cs:48-57`）。新增固定槽位服务时，状态必须在 `LoadFromData` 阶段写完整，不要指望 `OnAutoLoad` 补。
11. **`dataToSave` 与运行态是同一个对象**：`WriteSave` 直接序列化 `SaveDataManager.Instance.GetData`（`SaveSystem.cs:52`），`Loot.SaveData` 直接改这个字典（`Loot.cs:216-223`）。存档边界没有快照隔离，落盘前任何对 `dataToSave` 的写入都会进文件。
12. **保存文件名只有毫秒精度**（`yyyyMMdd_HHmmss_fff`，:50），两次保存落在同一毫秒会互相覆盖；`WriteSave` 无 try/catch、无目录创建。
13. **注册/注销必须幂等且成对**：`SaveRegistry.Add` 靠 `Contains` 去重（`SaveRegistry.cs:21-25`），`Remove` 只删第一处（:27），因此重复注册要靠调用方自己的 `registered` 标记（`Loot.cs:35,86-108`）。漏注销会让静态表跨场景持有已销毁对象。
14. **静态表的生命周期由引擎控制**：`ResetStatics`（`SaveRegistry.cs:18-19`）在每次进 Play 前清空。不要依赖 static 字段的初值，也不要在 `Awake` 顺序上做假设。
15. **读档位置会被 `Vector3.zero` 语义吃掉**：`SaveSystem.cs:130` 只兜底 `null`，而 `SceneChanger.cs:153` 把 `(0,0,0)` 视为"用场景初始点"。坐标恰为零的合法存档位置无法区分。
16. **自动存档在 Location→Location 时不记玩家真实坐标**：`SaveDataManager.cs:107-108` 取目标场景 `initialPosition`；真实坐标只出现在手动存档（:62）和"切回 Menu"分支（:109）。

## 4. 已知缺陷与风险

1. **跨场景同 ID 共享状态**：`lootsStatsDic` 无场景维度（`SaveData.cs:9`），而 `Scene1.unity` 与 `StartingMenu.unity` 的 12 个 `SaveDefinition.ID` override **完全一致**（实测交集 12/12），`Scene1 ∩ Scene2 = 0`。同一 ID 在不同场景被视为同一实体。
2. **同场景内也已有撞档**：`StartingMenu.unity` 有 14 个 ID override 但只有 12 个不同值——`335295e3-999f-454a-a2a9-9f4ce12be647` 出现 3 次，即 3 个掉落实例共用一条存档记录。
3. **预制体烘焙了非空 ID**：`{Loot,Gold,MushroomRed}.prefab` 各含 1 个 `persistentType: 0` + 非空 ID（如 `Loot.prefab` = `ea24051a-9a9e-4601-ae4d-2f1b50dc64ee`），而 `SaveDefinition.cs:13-16` 只在 ID 为空时补发 → 新拖入场景且未手改 ID 的实例必然与同源实例撞档（场景覆盖 ID 是当前唯一的防线）。
4. **28/72 份系统档的场景键已无法反查**：实测 10 个不同的历史 GUID（如 `fd6cb3ce…` 5 份、`0d57019f…` 6 份、`41818648…` 4 份）不在当前任何 `SaveKey` 集合内 → `GetScene` 返回 null → 这些档被 `IsLoadableSaveFile` 判为不可加载，`Continue` 会静默回退到更早的档。
5. **`Continue` 会回到场景出生点**：Location→Location 自动存档写的是目标场景 `initialPosition`（`SaveDataManager.cs:107-108`）。实测 27 份 `Scene2` 档中 24 份坐标恰为 `(10.5, 8, 0)` = `Scene2.asset` 的 `initialPosition`。
6. **在 Location 内读档仍会写出一份新系统档**：`LoadSave` 走 `RequestSceneLoad` 时 `lastSceneType` 已是 Location，`SaveDataManager.cs:100-116` 照常 `RaiseDataSaveEvent(SystemSave)`；标志位只挡住了重置分支。
7. **面板槽位与排序**：`GetSavesPath`（`SaveSystem.cs:179`）返回 `Directory.GetFiles` 原始顺序且不排序，`LoadInfoToSaveList`（`SaveLoadCanvasManager.cs:109-133`）把它直接映射到 26 个槽位 → 首槽未必是最新档；第 27 份起的手动档在 UI 上不可见。只有 `GetLatestLoadableSavePath` 会排序。
8. **手动存档无上限、无覆盖**：每次点击都新建时间戳文件（`SaveLoadCanvasManager.cs:159`），保存后不刷新列表，实测目录已有 72 份 SystemSave + 1 份 PlayerSave，清理入口只有 Menu 场景的 Delete。

次要风险：`DynamicDataHandler.PrepareForNewGameLoad`（`DynamicDataHandler.cs:5-8`）与 `SaveRegistry.Clear()`（`SaveRegistry.cs:30`）**全工程无调用点**，同一进程内"重新开始"不会清注册表；`WriteSave` 未判空 `SaveDataManager.Instance`（`SaveSystem.cs:52`）也未捕获 I/O 异常。

## 5. 未核验事项

- 假设：Player 包中 `SaveKey` 退化为 `GameSceneSO` 资产名，导致编辑器与包体写出的存档互不可读（未运行 Unity 验证，仅据 `GameSceneSO.cs:38-48` 的分支推断）。
- 假设：跨场景重复 ID 的可见后果是"在 `Scene1` 拾取的掉落物在 `StartingMenu` 中同样消失/保持已拾取"（据 ID 集合实测 + `Loot.LoadData` 逻辑推断，未运行验证）。
- 假设：那 10 个历史 `sceneID` 来自 Addressables 之前的身份算法；未能在 git 历史中定位其来源（`git log -S` 在本机沙箱下无法执行），故来源不可考。
- 假设：26 个槽位是面板的可见上限（槽数来自 `PersistentScene.unity:12634` 的序列化数据，未运行 Unity 验证 UI 行为）。
- 假设：`moved` 分支从未在真实游玩中生效——73 份存档中 `"moved": true` 出现 0 次，`Loot.cs:240-243` 的位置回写路径未被实测覆盖。
- 假设：`SaveDataManager.Instance` 为 null 时 `WriteSave` 会抛 `NullReferenceException`（按 `SaveSystem.cs:52` 推断，未触发验证）。
- 假设：同一毫秒内两次保存会互相覆盖（按 `yyyyMMdd_HHmmss_fff` 命名推断，未验证）。
- 假设：`Array.Sort` 的字符串序等价于时间序（固定宽度格式下成立，实测文件清单与时间序一致，未经代码级验证）。
