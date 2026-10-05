# Assets 域权威指南（GameSO 资产契约 · 命名与入库 · StreamingAssets）

文档 ID：`ASSETS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本文件只对 `Assets/GameSO/**` 的 ScriptableObject 资产契约（目录分布、数量、命名、手工创作流程）与 `Assets/StreamingAssets/**` 的构建硬约束负责；不负责各 SO 的运行时业务语义（见 Gameplay/*、SceneFlow/*）、不负责事件通道的收发用法（见 `EventChannels_Guide.md`）、不负责美术资源（Sprites/Animation/Prefabs）的导入设置。
上游来源：
- 代码：`Assets/Scripts/Pipeline/SO/**`、`Assets/Scripts/Pipeline/UI/OpenTxtWithSystem.cs`、`Assets/Scripts/Gameplay/Skills/SkillManager.cs`
- 资产：`Assets/GameSO/**`、`Assets/StreamingAssets/**`
- 旧文档（仅作线索）：`Temp/doc-discovery/asset-data-pipeline.json`、`Docs/UnityAndroidBuildGuide.md`、`Docs/UnityAndroidBuildVerification.md`

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `GameSO` / `ScriptableObject 资产` / `CreateAssetMenu` | 第 2 节（目录契约与数量） |
| `新增物品` / `ItemSO` / `Gold.asset` | 2.2 配置类资产清单 |
| `新增任务` / `QuestSO` | 2.2；任务判定语义见 Gameplay/Quest |
| `新增对话` / `DialogSO` / `RefuseDialogSO` | 2.3 对话资产的组织方式 |
| `SkillSO` / `skillName` / 技能开关 | 2.4 与第 4 节缺陷 2 |
| `GameSceneSO` / `场景资产` | 2.5；加载语义见 SceneFlow 域 |
| `资产命名` / `目录约定` | 第 3 节 |
| `StreamingAssets` / `GameGuide` | 2.6 与第 3 节硬约束 |
| `中文文件名` / `非 ASCII` / Android 构建失败 | 3.3 与 2.6 |
| `Config` 还是 `Event` | 2.1 资产分类 |
| `生成器` / 批量创建资产 | 2.7 与第 4 节缺陷 1 |

## 2. 当前实现

### 2.1 总体分类与统计方法

`Assets/GameSO` 下有 **59 个 `.asset`**。分类依据是每个资产 `m_Script` 指向的脚本 GUID（脚本 GUID 取自 `Assets/Scripts/Pipeline/SO/**/*.cs.meta`）：

- **配置/内容类：31 个**（ItemSO、QuestSO、DialogSO、RefuseDialogSO、CharacterSO、GameSceneSO、SkillSO、PlayerStatsSO）
- **运行时事件类：28 个**（12 个事件通道类，详见 `EventChannels_Guide.md`）

统计方法（本次核验实际执行，可复现）：对 `Assets/GameSO/**/*.asset` 逐个读 `m_Script` 的 guid，按脚本 `.cs.meta` 的 guid 反查类名后分组计数。注意**不要**用「文件名猜类」：`ChatSOs/.../PurpleBob.asset` 是 `CharacterSO` 而不是对话；`SkillButtonSO/CombatUnlock.asset` 的 `skillName` 实际是 `SwordSlash`。

### 2.2 配置类资产清单（31 个）

| 类 | 数量 | 代表路径 |
|---|---|---|
| `ItemSO` | 6 | `Assets/GameSO/ItemSO/Gold.asset`、`Mushroom.asset`、`Bow.asset`、`EXP.asset`、`Wood.asset`、`Meat.asset` |
| `QuestSO` | 3 | `Assets/GameSO/UI SO/QuestSO/DefaultQuest.asset`、`PickAndChat.asset`、`Shopping.asset` |
| `DialogSO` | 8 | `Assets/GameSO/ChatSOs/PurpleBobChats/NormalChats/PurpleBobs Greeting.asset` |
| `RefuseDialogSO` | 3 | `Assets/GameSO/ChatSOs/PurpleBobChats/ChatsWhileRefuse/DefaultChat.asset` |
| `CharacterSO` | 3 | `Assets/GameSO/ChatSOs/PurpleBobChats/PurpleBob.asset` |
| `GameSceneSO` | 5 | `Assets/GameSO/GameSceneSO/Scene1.asset` |
| `SkillSO` | 2 | `Assets/GameSO/UI SO/SkillButtonSO/MaxHealthBoost.asset` |
| `PlayerStatsSO` | 1 | `Assets/GameSO/PlayerStatsSO.asset` |
| `LocationSO` | **0** | 类定义存在（`Assets/Scripts/Pipeline/SO/LocationSO.cs:5`）但全工程无任何该类型资产 |

要点：

- `ItemSO` 是纯 `ScriptableObject`，**无稳定 id**（`Assets/Scripts/Pipeline/SO/ItemSO.cs:7-24` 只有 `itemName`/`icon`/数值字段），因此物品身份靠资产引用而非 GUID。
- `GuidSO` 提供 `[SerializeField] private string guid` 并在 `OnValidate` 里补发（`Assets/Scripts/Pipeline/SO/GuidSO.cs:7-19`），当前唯一使用者是 `CharacterSO`（`CharacterSO.cs:5`）。
- `PlayerStatsSO` 存"初始值"模板，运行时取拷贝：`CreateInitialData() => stats.Clone()`（`PlayerStatsSO.cs:11-17`），由 `Assets/Tests/Editor/PlayerStatsSOTests.cs:42-50` 断言"模板不应被运行时改动"。
- `GameSceneSO.SaveKey` 取 `.unity` 的资产 GUID（`GameSceneSO.cs:31-50`），`ID` 明确标注不可用于存档（`GameSceneSO.cs:12`）。场景加载语义属 SceneFlow 域，此处只声明资产契约。

### 2.3 对话资产的组织方式

`ChatSOs` 目录的层级是「**一个角色一个文件夹**」，但文件夹根放的是 `CharacterSO`，对话节点按用途再分子目录：

```
Assets/GameSO/ChatSOs/
├── BlueWarrior(Player)Chats/Warrior.asset          (CharacterSO)
├── YellowBobChats/YellowBob.asset                  (CharacterSO)
│                  YellowBob Greeting.asset         (DialogSO)
└── PurpleBobChats/PurpleBob.asset                  (CharacterSO)
                   PurpleBobDefaultChat.asset       (DialogSO)
                   NormalChats/                     (DialogSO ×2)
                   Options/                         (DialogSO ×4)
                   ChatsWhileRefuse/                (RefuseDialogSO ×3)
```

`RefuseDialogSO` 继承 `DialogSO` 并追加拒绝条件（`RefuseDialogSO.cs:5-11`：`isDefaultChat`、`requireCharacters`、`requireItems`）。`DialogSO` 自身通过 `parentDialog`/`nextDialogOptions` 互引用构成对话树（`DialogSO.cs:10, 18, 31`），属于**资产间引用**，重命名/移动会断引用。

### 2.4 技能资产的字段级契约与隐藏耦合

`SkillSO` 只有三个字段（`SkillSO.cs:9-11`：`skillName`、`maxLevel`、`skillIcon`）。但技能效果**不在资产里**，而在代码的字符串 switch：

```csharp
// Assets/Scripts/Gameplay/Skills/SkillManager.cs:19-31
string skillName = skillSlot.skillSO.skillName;
switch (skillName) {
    case "MaxHealthBoost": ...
    case "SwordSlash": ...
}
```

即：`skillName` 字符串是"配置 → 行为"的唯一关联键，且**资产文件名与它无关**。现状实证：`CombatUnlock.asset` 的 `skillName: SwordSlash`（`Assets/GameSO/UI SO/SkillButtonSO/CombatUnlock.asset:13,15`），而 `MaxHealthBoost.asset` 的 `skillName: MaxHealthBoost`。

### 2.5 GameSceneSO 资产（5 个）

`Assets/GameSO/GameSceneSO/{MenuScene, Scene1, Scene2}.asset`、`OtherScenes/{TestScene, RetrySceneSO}.asset`。`sceneType` 取 `MyEnums.SceneType`（`Location/Menu/Retry`，`Assets/Scripts/Contracts/MyEnums.cs:20-25`）。这套资产必须与 `ProjectSettings/EditorBuildSettings.asset` 中的场景列表同时维护——加载语义与前置校验见 SceneFlow 域，此处仅声明"资产是场景组列表的元素"。其中 `OtherScenes/RetrySceneSO.asset`（`sceneType: 2` = `Retry`，见该资产 `:21` 与 `MyEnums.cs:20-25`）**已不再被 `SceneChanger` 引用**，也不在 Build Settings 场景列表内，当前是零引用的历史资产。

### 2.6 StreamingAssets 现状

`Assets/StreamingAssets` 下**只有一个有效文件**：`GameGuide.txt`（1372 字节）+ 其 `.meta`。全工程唯一读取点是：

```csharp
// Assets/Scripts/Pipeline/UI/OpenTxtWithSystem.cs:9-18
string path = Path.Combine(Application.streamingAssetsPath, "GameGuide.txt");
if (File.Exists(path)) Process.Start(path);          // 用系统默认程序打开
else Debug.LogError("找不到游戏指南文件：" + path);
```

唯一调用者：`Assets/Scenes/GameScene/StartingMenu.unity:8682`（`m_TargetAssemblyTypeName: OpenTxtWithSystem`）。

### 2.7 全部 GameSO 资产均为手工创建

全工程 `Assets` 下 `[CreateAssetMenu]` 共 21 处（覆盖 SO/Events 全部类型，逐文件检索 `[CreateAssetMenu` 实测）；唯一的资产生成器是 `Assets/Editor/InputActionReferenceRebuilder.cs`，它只重建 `Assets/Settings/Input/ActionRefs/` 下的 `InputActionReference`，与 `GameSO` 无关（`Assets/Editor/` 下仅此一个文件）。

代价（当前事实，非计划）：

- 新增任一资产需人工 `Create` + 改名 + 拖引用；`ItemSO`/`QuestSO`/`SkillSO` 的 `CreateAssetMenu` 默认文件名分别是 `New Item`、`New Quest`、`NewSkill`（`ItemSO.cs:6`、`QuestSO.cs:5`、`SkillSO.cs:6`），不重命名会留下无语义资产。
- `[CreateAssetMenu]` 的菜单分组不统一：物品在 `GameLootSO/ItemSO`、任务在顶层 `QuestSO`、地点在顶层 `LocationSO`、角色/对话在 `Dialog/*`、技能在 `SkillTree/Skill`、事件在 `Events/*`、玩家数值在 `Data/*`、场景在 `GameSceneSO/*`。菜单树与实际目录树（`GameSO/<用途>/`）**不是一回事**。
- 引用只能靠 Inspector 拖，没有校验脚本；断引用与漏接线都不会报错（见第 4 节）。

## 3. 约定与硬边界

### 3.1 目录与命名约定

- **目录**：`Assets/GameSO/<用途>/`。事件通道统一在 `Assets/GameSO/Events/`，再分 `VoidEvents/`、`ToggleCanvasEvents/`、`InventorySlotsStatsEvents/`。
- **配置资产命名跟随类名或语义名**：`CharacterSO` 放在以角色名命名的文件夹根部；`ToggleCanvasEvents/*.asset` 命名为 `Toggle<枚举后缀>Event`（与 `MyEnums.CanvasToToggle` 的成员一一对应，见 `EventChannels_Guide.md` 2.3）。
- **`VoidEventSO` 资产必须带语义前缀**：类名不含语义，同语义复用时会分不清谁是谁。该规则写在源码注释里（`Assets/Scripts/Pipeline/SO/Events/VoidEventSO.cs:4-5`）："资产名必须含语义前缀，如 SlashActionFinishedEventSO…禁止再创建无语义的资产名"。
- **`GuidSO` 派生类不要手动改 `guid`**：由 `OnValidate` 仅在为空时补发（`GuidSO.cs:12-19`），手动清空会在下次 Inspector 校验时悄然换成新值。

### 3.2 GameSO 中"配置"与"运行时事件"的边界

- `Assets/GameSO/Events/**` 下的 28 个资产是**运行时事件通道**：不承载业务数据，只承载"谁在听"。它们的状态（C# event 委托链）**不落盘**——`public event Action<...>` 不是 `[SerializeField]`，因此资产文件里看不到订阅者，Unity 也不会序列化它们。
- 其余 31 个是**配置/内容**：内容随资产文件落盘并进包。
- 硬边界：**不要**把运行时状态写进 SO 资产（例如把当前任务进度写进 `QuestSO`）。`QuestSO.QuestObjective.currentAmount` 就是反例——字段已无代码读写，运行期进度由 `QuestManager` 的 `questProgress` 字典维护，源码注释已明确标注（`QuestSO.cs:64-67, 84-86`）。

### 3.3 StreamingAssets 硬约束（违反会坏构建或运行时静默失效）

1. **该目录下的文件名必须是 ASCII。** 本工程曾用 `Assets/StreamingAssets/游戏指南.txt`，导致 Android Release 构建在 AGP 解包 AAR 阶段抛 `java.nio.charset.MalformedInputException` / `malformed input off : 8, length : 1`，堆栈落在 `com.android.builder.aar.AarExtractor`；修复方式是改名为 ASCII 的 `GameGuide.txt`（证据：`Docs/UnityAndroidBuildGuide.md:72-95`、`Docs/UnityAndroidBuildVerification.md:23-24`）。**文件内容仍可用 UTF-8 中文**，受约束的只是**文件名**。
2. **不要靠关闭 `lintVital` 绕过**：文档明确要求不要用关 lint 来掩盖 AAR 结构问题（`Docs/UnityAndroidBuildGuide.md:95`）。
3. **Android 上该目录在 APK 内，不能用 `System.IO`/`Process` 直接当普通文件访问。** 这是 `Assets/StreamingAssets/GameGuide.txt` 在包内是 `assets/GameGuide.txt`（`Docs/UnityAndroidBuildVerification.md:33`）的直接推论：`Path.Combine(Application.streamingAssetsPath, ...)` 产生的路径在 Android 上无法用 `File.Exists` 命中，因此 `OpenTxtWithSystem.cs:11-18` 会走 `LogError` 分支。正规读法（`UnityWebRequest`）本工程尚未采用。
4. **改完包内条目必须复核**：既有复核脚本直接扫 ZIP 条目，要求 `assets/GameGuide.txt` 存在且非 ASCII 条目为 0（`Docs/UnityAndroidBuildVerification.md:49-58`）。

现状核验：`Assets` 全树（含目录名）非 ASCII 路径数 = **0**（本次逐项扫描 `.FullName` 与 `[^\x00-\x7F]` 匹配）。

## 4. 已知缺陷与风险

1. **全部资产手工创建，无生成器、无校验。** 直接后果是第 2.7 节的代价与下文的漂移/死资产长期存在；新增资产完全依赖人的记忆。证据：`Assets/Editor/` 仅 `InputActionReferenceRebuilder.cs`（只管 `ActionRefs/`）。
2. **技能效果的字符串耦合。** `SkillManager.cs:21-31` 用 `skillName` switch 分派；改 `skillName`、改资产名或在代码外新增技能，都不会有编译期错误，只会**静默无效果**。当前 `SwordSlash` 这个键对应的资产却叫 `CombatUnlock.asset`，极易被误改名。
3. **5 个 DialogSO 系列资产携带已不存在的字段。** 资产里序列化着 `chatType`、`canOnlyBeTriggeredOnce`，而 `DialogSO`/`RefuseDialogSO` 中已无这两个字段（`DialogSO.cs:8-19`、`RefuseDialogSO.cs:6-11`）：`PurpleBobDefaultChat.asset:15-16`、`Options/{KnowMoreDialog,NoIntersted,RefuseToGiveMushroom}.asset`、`ChatsWhileRefuse/RefuseByCharacter.asset`。`PurpleBobDefaultChat.asset:31` 还多带第三个遗留字段 `refusingDialogs`（现行字段名是 `DialogSO.refuseDialogs`，`DialogSO.cs:14`）。这些遗留字段并非全为 0：`Options/{KnowMoreDialog,NoIntersted,RefuseToGiveMushroom}.asset:15` 的 `chatType` 实测均为 `2`，其余为 `0`；当前无任何代码读取它们，暂未造成数据损失；但它是**旧 schema 残留**，一旦有人以为它们生效并去填值，配置会被静默丢弃。此现象与 `Temp/doc-discovery/asset-data-pipeline.json` 把 `PurpleBob.asset`/`YellowBob.asset`/`Warrior.asset` 记为 `DialogSO`（实为 `CharacterSO`）是同一批历史记录的偏差——**以本次按 `m_Script` GUID 的统计为准**。
4. **18 个 GameSO 资产零外部引用**（对 `Assets` 下 382 个 `.unity/.prefab/.asset` 语料做 GUID 计数，排除 `GameSO` 自身目录）：`ItemSO/EXP`、`ItemSO/Wood`、`GameSceneSO/OtherScenes/{TestScene,RetrySceneSO}`（`RetrySceneSO.asset` **已不再被引用**：`SceneChanger` 的重试复活编排已迁至 `PlayerDamageController`，全工程只剩该资产自身，无任何场景/预制体/资产/代码落点）、`Events/VoidEvents/{LoadData,SaveData}`、`Events/InventorySlotsStatsEvents/SlotsUpdateRequest`，以及 `ChatSOs` 下的 11 个（含 3 个 `CharacterSO` 与整棵 PurpleBob 对话树）。**注意**：`ItemSO/EXP`、`ItemSO/Wood` 等很可能是"由其他资产引用但当前引用链已断"或"经运行时代码/场景实例化间接引用"，本次为静态 GUID 计数，未运行 Unity 验证其真实可达性。
5. **`LocationSO` 有类无资产。** 类存在且带 `CreateAssetMenu`（`LocationSO.cs:5`），`QuestObjective` 也引用了它（`QuestSO.cs:28`），但全工程 0 个 `LocationSO` 实例，说明"地点类任务目标"这条配置链路从未落地。
6. **`StreamingAssets` 只有 `GameGuide.txt`，且其唯一读取路径在 Android 上不可用**（见 3.3-3）。桌面/Editor 正常，Android 上是"点按钮只打印一条 LogError"。

## 5. 未核验事项

- 假设：`Process.Start` 在 Android 上不可用、且 `Application.streamingAssetsPath` 在 Android 上位于 APK 内因而 `File.Exists` 返回 `false`（未运行 Unity/未上真机验证；本指南 3.3-3 的结论由包内条目 `assets/GameGuide.txt` 与 API 语义推导）。若要结论落地，需在 Android 构建上实测 `OpenGuide` 的日志。
- 假设：第 4 节缺陷 4 中列出的 18 个"零引用"资产确实在运行时不可达（未运行 Unity；仅做资产文本 GUID 计数，`Resources.Load`/`Addressables`/代码内字符串加载等间接路径未被覆盖——不过本工程代码中未见 `Resources.Load`，`Addressables` 无运行时调用）。
- 假设：`PurpleBob` 对话树当前无法从游戏内触发（其根 `CharacterSO` 与对话节点均零引用；未运行 Unity 验证 NPC 触发链路）。
- 假设：`QuestObjective.currentAmount` 确实无任何代码读写（依据 `QuestSO.cs:64-67, 84-86` 的源码注释与本次对 `QuestManager` 的 grep；未做全量符号级调用图验证）。
