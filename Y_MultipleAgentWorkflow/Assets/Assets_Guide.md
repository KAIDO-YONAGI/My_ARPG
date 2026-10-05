# Assets 域权威指南：GameSO 资产契约 · 命名与入库 · StreamingAssets

文档 ID：`ASSETS-GUIDE`
状态：`Active`
最后更新：`2026-10-05`
核验日期：`2026-10-05`
权威范围：本文件只对 GameSO 目录下的 ScriptableObject 资产契约负责，涵盖目录分布、数量、命名与手工创作流程，另对 StreamingAssets 目录的构建硬约束负责。各 SO 的运行时业务语义见 Gameplay 与 SceneFlow 各子域，事件通道的收发用法见 `EventChannels_Guide.md`，美术资源的导入设置不在本文件范围。
上游来源：
- 配置类型：`ItemSO`、`QuestSO`、`DialogSO`、`RefuseDialogSO`、`CharacterSO`、`GameSceneSO`、`SkillSO`、`PlayerStatsSO`、`LocationSO`、`GuidSO`
- 运行时逻辑：`OpenTxtWithSystem`、`SkillManager`
- 资产集合：GameSO 目录下的全部 ScriptableObject 资产、StreamingAssets 目录下的指南文件

## 1. 触发线索

| 线索 | 指向 |
|---|---|
| `GameSO` / `ScriptableObject 资产` / `CreateAssetMenu` | 第 2 节，目录契约与数量 |
| `新增物品` / `ItemSO` / `Gold 物品资产` | 2.2 配置类资产清单 |
| `新增任务` / `QuestSO` | 2.2；任务判定语义见 Gameplay 的 Quest 子域 |
| `新增对话` / `DialogSO` / `RefuseDialogSO` | 2.3 对话资产的组织方式 |
| `SkillSO` / `skillName` / 技能开关 | 2.4 与第 4 节缺陷 2 |
| `GameSceneSO` / `场景资产` | 2.5；加载语义见 SceneFlow 域 |
| `资产命名` / `目录约定` | 第 3 节 |
| `StreamingAssets` / `GameGuide` | 2.6 与第 3 节硬约束 |
| `中文文件名` / `非 ASCII` / Android 构建失败 | 3.3 与 2.6 |
| `Config` 还是 `Event` | 2.1 资产分类 |
| `生成器` / `批量创建资产` | 2.7 与第 4 节缺陷 1 |

## 2. 当前实现

### 2.1 总体分类与统计方法

GameSO 目录下共有 **59 个 ScriptableObject 资产**，分类依据是每个资产 `m_Script` 指向的脚本 GUID：

- **配置/内容类：31 个**，覆盖 `ItemSO`、`QuestSO`、`DialogSO`、`RefuseDialogSO`、`CharacterSO`、`GameSceneSO`、`SkillSO`、`PlayerStatsSO`
- **运行时事件类：28 个**，覆盖 12 个事件通道类，清单见 `EventChannels_Guide.md`

统计方法可复现：逐个读资产的 `m_Script` guid，按脚本元数据的 guid 反查类名后分组计数。按文件名猜类会出错，例如 `PurpleBob` 是 `CharacterSO`，`CombatUnlock` 的 `skillName` 是 `SwordSlash`。

### 2.2 配置类资产清单（31 个）

| 类 | 数量 | 代表资产 |
|---|---|---|
| `ItemSO` | 6 | `Gold`、`Mushroom`、`Bow`、`EXP`、`Wood`、`Meat` |
| `QuestSO` | 3 | `DefaultQuest`、`PickAndChat`、`Shopping` |
| `DialogSO` | 8 | `PurpleBobs Greeting` 等 |
| `RefuseDialogSO` | 3 | `DefaultChat` 等 |
| `CharacterSO` | 3 | `PurpleBob` 等 |
| `GameSceneSO` | 5 | `MenuScene`、`Scene1`、`Scene2`、`TestScene`、`RetrySceneSO` |
| `SkillSO` | 2 | `MaxHealthBoost`、`CombatUnlock` |
| `PlayerStatsSO` | 1 | 玩家属性配置资产 |
| `LocationSO` | **0** | 类定义存在，全工程无该类型资产 |

要点：

- `ItemSO` 是纯 `ScriptableObject`，**无稳定 id**，字段只有 `itemName`、`icon` 与数值字段，物品身份靠资产引用。
- `GuidSO` 提供 `[SerializeField] private string guid`，并在 `OnValidate` 里补发，唯一使用者是 `CharacterSO`。
- `PlayerStatsSO` 存初始值模板，运行时取拷贝，`CreateInitialData()` 返回 `stats.Clone()`；`PlayerStatsSOTests` 断言模板不被运行时改动。
- `GameSceneSO.SaveKey` 取场景资产的 GUID，`ID` 标注为不可用于存档。场景加载语义属 SceneFlow 域，本文件只声明资产契约。

### 2.3 对话资产的组织方式

`ChatSOs` 目录按一个角色一个文件夹组织，文件夹根放该角色的 `CharacterSO`，对话节点按用途再分子目录：

- `BlueWarrior(Player)Chats` 的根放 `Warrior`。
- `YellowBobChats` 的根放 `YellowBob`，同目录另有 `YellowBob Greeting` 对话。
- `PurpleBobChats` 的根放 `PurpleBob` 与 `PurpleBobDefaultChat`；`NormalChats` 下 2 个对话，`Options` 下 4 个对话，`ChatsWhileRefuse` 下 3 个拒绝对话。

`RefuseDialogSO` 继承 `DialogSO` 并追加拒绝条件 `isDefaultChat`、`requireCharacters`、`requireItems`。`DialogSO` 通过 `parentDialog` 与 `nextDialogOptions` 互引用构成对话树，属于资产间引用，重命名或移动资产会断引用。

### 2.4 技能资产的字段级契约与字符串耦合

`SkillSO` 只有三个字段 `skillName`、`maxLevel`、`skillIcon`，技能效果由代码按字符串分派：

```csharp
// SkillManager 按 skillName 分发技能效果
string skillName = skillSlot.skillSO.skillName;
switch (skillName) {
    case "MaxHealthBoost": ...
    case "SwordSlash": ...
}
```

`skillName` 字符串是配置到行为的唯一关联键，资产文件名与它无关：`CombatUnlock` 资产的 `skillName` 是 `SwordSlash`，`MaxHealthBoost` 资产的 `skillName` 是 `MaxHealthBoost`。

### 2.5 GameSceneSO 资产（5 个）

5 个场景资产是 `MenuScene`、`Scene1`、`Scene2`、`TestScene`、`RetrySceneSO`，`sceneType` 取 `MyEnums.SceneType` 的 `Location`、`Menu`、`Retry`。这套资产必须与编辑器 Build Settings 里的场景列表同时维护，加载语义与前置校验见 SceneFlow 域，本文件只声明资产是场景组列表的元素。

`RetrySceneSO` 的 `sceneType` 是 `Retry`，当前零外部引用，也不在 Build Settings 场景列表内。

### 2.6 StreamingAssets 现状

StreamingAssets 目录下只有一个有效文件：`GameGuide` 指南文件，1372 字节，另有其元数据文件。全工程唯一读取点是 `OpenTxtWithSystem`，它把 `Application.streamingAssetsPath` 与指南文件名拼接，命中时用系统默认程序打开，缺失时 `Debug.LogError` 输出找不到游戏指南文件。

唯一调用点在 `StartingMenu` 场景的按钮上，绑定的目标类型是 `OpenTxtWithSystem`。

### 2.7 全部 GameSO 资产均为手工创建

全工程共 21 处 `[CreateAssetMenu]`，覆盖 SO 与事件的全部类型。唯一的编辑器资产生成器是 `InputActionReferenceRebuilder`，它只重建输入系统的 `InputActionReference`，与 GameSO 无关。

代价（当前事实）：

- 新增任一资产需要人工 Create、改名、拖引用；`ItemSO`、`QuestSO`、`SkillSO` 的 `CreateAssetMenu` 默认文件名分别是 `New Item`、`New Quest`、`NewSkill`，不重命名会留下无语义资产。
- `[CreateAssetMenu]` 的菜单分组不统一：物品在 `GameLootSO/ItemSO`，任务在顶层 `QuestSO`，地点在顶层 `LocationSO`，角色与对话在 `Dialog` 下，技能在 `SkillTree/Skill`，事件在 `Events` 下，玩家数值在 `Data` 下，场景在 `GameSceneSO` 下。菜单树按类型分组，资产目录树按用途分组，两者结构不同。
- 引用靠 Inspector 拖，没有校验脚本，断引用与漏接线都不会报错。

## 3. 约定与硬边界

### 3.1 目录与命名约定

- **目录**：GameSO 目录下按用途分目录。事件通道统一放在 Events 目录，再分 `VoidEvents`、`ToggleCanvasEvents`、`InventorySlotsStatsEvents`。
- **配置资产命名跟随类名或语义名**：`CharacterSO` 放在以角色名命名的文件夹根部；`ToggleCanvasEvents` 下的资产命名为 `Toggle<枚举后缀>Event`，与 `MyEnums.CanvasToToggle` 的成员一一对应，见 `EventChannels_Guide.md` 的 2.3。
- **`VoidEventSO` 资产必须带语义前缀**：类名不含语义，同语义复用时会分不清谁是谁。该规则写在 `VoidEventSO` 的源码注释里，要求资产名含语义前缀，例如 `SlashActionFinishedEventSO`，并禁止再创建无语义的资产名。
- **`GuidSO` 派生类不要手动改 `guid`**：`OnValidate` 只在为空时补发，手动清空会在下次 Inspector 校验时换成新值。

### 3.2 GameSO 中配置与运行时事件的边界

- Events 目录下的 28 个资产是**运行时事件通道**，只承载谁在听，不承载业务数据。事件字段是 C# event 委托链，未标注 `[SerializeField]`，因此不落盘，资产里看不到订阅者，Unity 也不序列化它们。
- 其余 31 个是**配置/内容**，随资产落盘并进包。
- 硬边界：运行时状态由服务层持有，SO 资产只落盘配置。`QuestSO.QuestObjective.currentAmount` 字段无代码读写，运行期进度由 `QuestManager` 的 `questProgress` 字典维护，源码注释已标注。

### 3.3 StreamingAssets 硬约束

违反下列约束会坏构建或让运行时静默失效。

1. **该目录下的文件名必须是 ASCII。** 非 ASCII 文件名会让 Android Release 构建在 AGP 解包 AAR 阶段抛 `java.nio.charset.MalformedInputException`，报 `malformed input off : 8, length : 1`，堆栈落在 `com.android.builder.aar.AarExtractor`。文件内容可以用 UTF-8 中文，受约束的只是文件名。
2. **不要靠关闭 `lintVital` 绕过 AAR 结构问题。**
3. **Android 上该目录位于 APK 内，`System.IO` 与 `Process` 无法当普通文件访问。** 包内条目挂在 APK 的资源目录下，`Path.Combine(Application.streamingAssetsPath, ...)` 产生的路径在 Android 上无法用 `File.Exists` 命中，因此 `OpenTxtWithSystem` 会走 `LogError` 分支。正规读法是 `UnityWebRequest`，本工程尚未采用。
4. **改完包内条目必须复核**：复核脚本直接扫 ZIP 条目，要求指南文件条目存在，且非 ASCII 条目为 0。

现状核验：全工程路径含目录名的非 ASCII 数量为 **0**。

## 4. 已知缺陷与风险

1. **全部资产手工创建，无生成器、无校验。** 直接后果是 2.7 的代价与下文的漂移、零引用资产长期存在，新增资产完全依赖人的记忆。全工程唯一的资产生成器是 `InputActionReferenceRebuilder`，它只处理输入系统的 `InputActionReference`。
2. **技能效果的字符串耦合。** `SkillManager` 用 `skillName` switch 分派，改 `skillName`、改资产名或在代码外新增技能都不会有编译期错误，只会静默无效果。键 `SwordSlash` 对应的资产是 `CombatUnlock`，容易被误改名。
3. **5 个 DialogSO 系列资产带有两个遗留字段。** 资产里序列化着 `chatType` 与 `canOnlyBeTriggeredOnce`，而 `DialogSO` 与 `RefuseDialogSO` 类型上没有这两个字段，涉及 `PurpleBobDefaultChat`、`Options` 下的 `KnowMoreDialog`、`NoIntersted`、`RefuseToGiveMushroom` 与 `ChatsWhileRefuse` 下的 `RefuseByCharacter`。`PurpleBobDefaultChat` 还多带第三个遗留字段 `refusingDialogs`，现行字段名是 `DialogSO.refuseDialogs`。这些遗留字段不全为 0：`KnowMoreDialog`、`NoIntersted`、`RefuseToGiveMushroom` 的 `chatType` 值为 2，其余为 0；当前无代码读取它们，暂未造成数据损失，一旦有人以为它们生效并去填值，配置会被静默丢弃。本文件的分类以 `m_Script` GUID 统计为准。
4. **18 个 GameSO 资产零外部引用**，统计方式是对全部场景、预制体与资产按 GUID 计数并排除 GameSO 自身目录：`EXP` 与 `Wood` 两个物品资产，`TestScene` 与 `RetrySceneSO` 两个场景资产，`LoadData`、`SaveData`、`SlotsUpdateRequest` 三个事件通道资产，以及 `ChatSOs` 目录下的 11 个，含 3 个 `CharacterSO` 与整棵 PurpleBob 对话树。`RetrySceneSO` 当前只被自身引用，重试复活编排由 `PlayerDamageController` 承担。
5. **`LocationSO` 有类无资产。** 类定义存在且带 `CreateAssetMenu`，`QuestObjective` 也引用它，全工程 0 个 `LocationSO` 实例，地点类任务目标的配置链路未落地。
6. **StreamingAssets 只有 `GameGuide` 指南文件，其唯一读取路径在 Android 上不可用。** 桌面与编辑器正常，Android 上点按钮只打印一条 LogError。

## 5. 未核验事项

- 假设：`Process.Start` 在 Android 上不可用，且 `Application.streamingAssetsPath` 在 Android 上位于 APK 内，`File.Exists` 返回 false。该结论由包内条目与 API 语义推导，Android 构建上 `OpenGuide` 的运行期行为待编辑器实测。
- 假设：第 4 节列出的 18 个零引用资产在运行时不可达。本次为资产文本 GUID 计数，未运行 Unity，`Resources.Load`、Addressables 与代码内字符串加载等间接路径未覆盖，本工程代码中未见 `Resources.Load`，Addressables 无运行时调用。
- 假设：PurpleBob 对话树当前无法从游戏内触发。其根 `CharacterSO` 与对话节点均零引用，NPC 触发链路待运行验证。
- 假设：`QuestObjective.currentAmount` 无任何代码读写。依据源码注释与对 `QuestManager` 的检索判断，未做全量符号级调用图验证。
