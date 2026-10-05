# Assets Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Assets 域权威文档

### 写入的文件

- `Y_MultipleAgentWorkflow\Assets\Assets_Guide.md`（新建，ID `ASSETS-GUIDE`，Active）
- `Y_MultipleAgentWorkflow\Assets\EventChannels_Guide.md`（新建，ID `ASSETS-EVENTCHANNELS-GUIDE`，Active）
- `Y_MultipleAgentWorkflow\Assets\Router.md`（重写为项目中文模板，`BUS-ASSETS`，下级导航无子类）
- `Y_MultipleAgentWorkflow\Assets\DeveloperLog.md`（本条追加）

### 依据的证据路径

- 代码：`Assets/Scripts/Pipeline/SO/{ItemSO,QuestSO,DialogSO,RefuseDialogSO,CharacterSO,GameSceneSO,SkillSO,PlayerStatsSO,GuidSO,LocationSO}.cs`、`Assets/Scripts/Pipeline/SO/Events/*.cs`（12 个类）、`Assets/Scripts/Pipeline/UI/OpenTxtWithSystem.cs`、`Assets/Scripts/Pipeline/UI/SystemCanvasManagers/UIManager.cs:17-18,185-201,343-366`、`Assets/Scripts/Gameplay/Skills/{SkillManager,SkillSlot}.cs`、`Assets/Scripts/Contracts/MyEnums.cs:20-25,56-85`、`Assets/Tests/Editor/PlayerStatsSOTests.cs:42-50`
- 资产：`Assets/GameSO/**`（59 个 `.asset`）、`Assets/GameSO/Events/**`（28 个）、`Assets/StreamingAssets/GameGuide.txt`、`Assets/Scenes/GameScene/StartingMenu.unity:8682`
- 统计方法：以每个 `.asset` 的 `m_Script` guid 对照 `Assets/Scripts/Pipeline/SO/**/*.cs.meta` 的 guid 反查类名后分组；引用计数以 `.asset.meta` 的资产 guid 对 `Assets` 下 428 个 `.unity/.prefab/.asset` 语料（排除 `GameSO` 自身目录）做 GUID 匹配
- 旧文档（仅作线索）：`Temp/doc-discovery/asset-data-pipeline.json`、`Docs/UnityAndroidBuildGuide.md:72-95`、`Docs/UnityAndroidBuildVerification.md:23-34,49-58`

### 已核验项

- `Assets/GameSO` 共 59 个资产 = 配置类 31（ItemSO 6 / QuestSO 3 / DialogSO 8 / RefuseDialogSO 3 / CharacterSO 3 / GameSceneSO 5 / SkillSO 2 / PlayerStatsSO 1 / LocationSO 0）+ 事件类 28。
- 事件通道 = 12 个类 / 28 个资产（ToggleCanvasEventSO 10、VoidEventSO 6、InventorySlotsStatsSO 3、其余 9 类各 1）。
- 12 个通道类全部直接继承 `ScriptableObject`，无公共基类；泛型参数仅引用 SO/枚举，零处引用 Manager/Controller/View。
- 订阅表达式 43 处 / 20 个文件、发布调用 23 处 / 12 个文件；订阅统一 `OnEnable +=` / `OnDisable -=`。
- 10 个 toggle 资产的 `canvasToToggle` 值互不重复，唯一无资产的是枚举成员 `Default`。
- `Assets` 全树（含目录）非 ASCII 路径数 = 0；`StreamingAssets` 仅 `GameGuide.txt`（1372 B）。
- 全工程 `[CreateAssetMenu]` 22 处；`Assets/Editor/` 仅 `InputActionReferenceRebuilder.cs`（只重建 `ActionRefs/`，与 GameSO 无关）。

### 未核验项

- Android 上 `OpenTxtWithSystem` 是否确实失效（由包内路径 `assets/GameGuide.txt` 与 API 语义推断，未运行 Unity/未上真机）。
- 18 个零外部引用资产是否在运行时确实不可达（纯静态 GUID 计数）。
- `PurpleBob` 对话树当前是否无法从游戏内触发。
- `QuestObjective.currentAmount` 是否确实无任何代码读写。
- `ToggleESCEvent.asset` 缺失 `canvasToToggle` 是否有意为之。

### 发现的缺陷

1. 全部 GameSO 资产手工创建，无生成器、无校验（唯一生成器 `InputActionReferenceRebuilder` 只管输入引用）。
2. `SkillManager.cs:21-31` 以 `skillName` 字符串 switch 分派技能效果；`CombatUnlock.asset` 的 `skillName` 实为 `SwordSlash`，改名即静默失效。
3. 5 个 DialogSO 系列资产携带源码中已不存在的 `chatType` / `canOnlyBeTriggeredOnce` 字段（旧 schema 残留）。
4. 18 个 GameSO 资产零外部引用（含 3 个零引用事件通道 `VoidEvents/LoadData`、`VoidEvents/SaveData`、`InventorySlotsStatsEvents/SlotsUpdateRequest`）。
5. `LocationSO` 有类有 `CreateAssetMenu` 且被 `QuestObjective` 引用，但 0 个实例。
6. `StreamingAssets/GameGuide.txt` 在 Android 上因位于 APK 内而无法被现有读取路径访问（桌面正常）。
7. 与旧文档的冲突：`Temp/doc-discovery/asset-data-pipeline.json` 将 `PurpleBob.asset`/`YellowBob.asset`/`Warrior.asset` 记为 `DialogSO`（实为 `CharacterSO`），并因此把 DialogSO 记为 8+3 结构；本文档以 `m_Script` GUID 统计为准。
8. `InventorySlotsStatsSO` 类名不含"事件"语义，与配置资产语感混淆，且其 3 个资产中有 1 个零引用。

### 维护计数

`BUS-ASSETS` 维护计数保持 `0/5`（本次为建立，非维护轮次）。
