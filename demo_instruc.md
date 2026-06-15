# Demo 介绍视频制作建议

## 建议定位

- 这支视频不要做成“功能清单”，而是做成“我解决了哪些工程问题”。
- 推荐主线只讲 4 个核心点：`A* 寻路`、`UI 焦点/互斥控制`、`任务/对话的数据驱动逻辑`、`存档读档`。
- 推荐总时长控制在 `8 分 30 秒` 左右，最多不要超过 `10 分钟`。
- 推荐表达顺序统一成：`先展示效果 -> 再给编辑器配置 -> 最后贴关键代码`。

## 你的视频可以这样开场

可以直接用下面这段做开头：

“这个 demo 是我用 Unity 做的一个 2D ARPG 原型。相比单纯堆玩法，我更希望把它做成几个可复用的系统，包括 A* 寻路、UI 焦点管理、任务与对话逻辑、以及存档读档。下面我会重点从这几个技术点介绍这个 demo 是怎么搭起来的。”

## 推荐总时长分配

| 时间 | 阶段 | 重点 |
| --- | --- | --- |
| 0:00 - 0:35 | 开场 | 项目类型、快速玩法 montage、讲清主线 |
| 0:35 - 2:05 | A* 寻路 | 网格生成、避障、动态重寻路 |
| 2:05 - 3:35 | UI 焦点机制 | 多面板互斥、排序、拖拽置顶 |
| 3:35 - 5:55 | 任务 + 对话 + SO | 状态流转、条件判断、数据驱动 |
| 5:55 - 7:35 | 存档读档 | ISaveable、DataManager、JSON 保存 |
| 7:35 - 8:20 | 收尾总结 | 设计思路、亮点总结、可扩展性 |

如果你最后剪出来超过 9 分钟，优先压缩开场和收尾，不要压缩 4 个核心技术段。

## 全片统一的镜头模板

每一段都尽量按下面这个节奏录，成片会很清楚：

1. `先录 5~10 秒游戏效果`
2. `再录 8~12 秒编辑器配置`
3. `最后贴 6~10 秒关键代码`

代码不要整页滚动，最好每段只贴 `1~2 个方法`，每次只让观众看到 `8~15 行` 的关键逻辑。

---

## 阶段 1：开场（0:00 - 0:35）

### 这一段要传达什么

- 这是一个完整的 ARPG 原型，不只是单个玩法测试。
- 后面介绍不会面面俱到，而是挑最能体现工程能力的 4 个系统。

### 建议录制的游戏画面

- `StartingMenu` 进入游戏。
- 在 `Scene1` 录一段快速 montage：
- 角色移动/攻击。
- NPC 对话。
- 打开任务面板。
- 打开存档面板。
- 怪物或 NPC 绕开障碍移动。

### 建议录制的编辑器画面

- `Assets/Scenes/GameScene/PersistentScene.unity`
- `Assets/Scenes/GameScene/Scene1.unity`
- `Docs/UML` 目录下几张图快速闪过即可：
- `AStar_Pathfinding.png`
- `UI_System.png`
- `ScriptableObjects_and_Events.png`
- `Scene_SaveLoad_System.png`

### 这一段不要贴太多代码

- 开场尽量不贴代码。
- 如果你想补一句项目规模，可以轻描淡写提一下：
- 主代码约 `100` 个脚本，约 `6785` 行，UI 和系统层占比较高。

### 简短口播文案

“这个项目目前已经把场景切换、对话、任务、背包、商店、技能树和存档串起来了。下面我不会把所有功能都展开讲，而是重点介绍几个我觉得最能体现系统设计的部分。”

### 剪辑提示

- 节奏一定要快，别在开场停太久。
- 开场最好配轻微放大、框选或标题字幕，例如：`A* / UI Focus / Quest / Save`。

---

## 阶段 2：A* 寻路（0:35 - 2:05）

### 这一段要传达什么

- 你不是只写了一个“能跑的寻路函数”，而是把它拆成了 `网格层 + 算法层 + 路径消费层`。
- 这个系统不只会绕障碍，还考虑了 `斜向通行`、`安全边距`、`目标移动后的重寻路`。

### 建议录制的游戏画面

- 在 `Scene1` 或 `TestScene` 里录一个敌人/NPC 绕障碍移动的镜头。
- 如果方便，勾上 `Gizmos`，让 `MovementController` 画出的路径点和线直接显示出来。
- 最好录一个“目标位置变化后，路径重新调整”的镜头，这样比单纯走直线更有说服力。

### 建议录制的编辑器画面

- 打开 `Scene1.unity`，选中 `NodeMapManager`。
- 在 Inspector 里展示：
- `tilemaps`
- `obstacleLayers`
- 再切到 Scene 视图，简单展示 `Walkable` 和 `Obstacle` 对应的地图区域。
- 如果你想让结构更清楚，可以中间插一张 `Docs/UML/AStar_Pathfinding.png`。

### 建议贴的代码

- `Assets/Scripts/A Star/AStarNodeManager.cs`
- 重点看 `ProcessTile`、`ProcessColliderObstacles`、`ApplySafetyMargin`
- `Assets/Scripts/A Star/AStarPathFinder.cs`
- 重点看 `FindPath`、`CanWalkDiagonally`、`NoCoverObstacleNodes`
- `Assets/Scripts/A Star/MovementController.cs`
- 重点看 `GetPosToGo`、`ReFindWay`、`OnDrawGizmos`

### 最适合截图的代码片段

```csharp
if (startCell != optCell
    && NodeCellMap.ContainsKey(optCell)
    && NodeCellMap[optCell].GetNodeType() == AStarNodeType.Walkable
    && NoCoverObstacleNodes(startCell, optCell))
{
    startCell = optCell;
}
```

```csharp
if (NodeCellMap[neighborPos].GetNodeType() != AStarNodeType.Walkable) continue;
if (NodeCellMap[neighborPos].GetNodeType() == AStarNodeType.Walkable
    && !CanWalkDiagonally(cx, cy, dx[i], dy[i])) continue;
```

### 简短口播文案

“A* 这一块我没有把所有逻辑都堆在一个脚本里，而是分成了节点管理、寻路算法和路径消费三层。这样敌人或者 NPC 只要挂上 MovementController，就可以直接复用寻路能力。另外这里也处理了斜向穿角、贴墙过近和目标移动后的重寻路问题。”

### 剪辑提示

- 先放实机，再切代码，不要反过来。
- 代码镜头里只框出 `FindPath` 和 `ReFindWay` 的关键判断，不要把整文件滚完。

---

## 阶段 3：UI 焦点机制（2:05 - 3:35）

### 这一段要传达什么

- 你不是单纯“把 UI 打开了”，而是专门处理了 `多窗口共存时谁在最上层`、`ESC 关闭顺序`、`拖拽后自动置顶`。
- 这是很典型的工程化 UI 管理问题，实习面试里很好讲。

### 建议录制的游戏画面

- 连续打开 `背包`、`属性`、`技能树`、`任务` 或 `存档` 面板。
- 录一个面板重叠的场景。
- 用鼠标拖拽某个窗口标题栏，让它到最上层。
- 再按 `ESC`，让观众看到它是按“最后打开 / 当前焦点”顺序关闭的。

### 建议录制的编辑器画面

- 打开 `PersistentScene.unity`，选中 `UIManager`。
- 在 Inspector 里展示：
- `toggleCanvasEvents`
- `inputBindings`
- 再选中一个具体面板管理器，比如：
- `QuestManager`
- `SaveCanvasPanelManager`
- 展示它们如何通过 `CanvasGroup + Canvas` 来控制显示和 sorting order。
- 如果要更直观，可以插入 `Docs/UML/UI_System.png`。

### 建议贴的代码

- `Assets/Scripts/UI/CanvasManagers/UIManager.cs`
- 重点看 `ToggleCanvas`、`ApplyFocusChange`、`RefreshFocusAfterClose`
- `Assets/Scripts/UI/CanvasManagers/ICanvasManager.cs`
- 重点看 `ToggleCanvas`、`RefreshCanvaOrder`
- `Assets/Scripts/UI/UIDrag.cs`
- 重点看 `OnDrag`
- `Assets/Scripts/UI/CanvasManagers/SaveCanvasPanelManager.cs`
- 重点看 `OpenPanel` 里手动 focus 的处理

### 最适合截图的代码片段

```csharp
currentFocusCanvas = target;
if (previousFocus != MyEnums.CanvasToToggle.Default &&
    previousFocus != target &&
    IsCanvasOpen(previousFocus))
{
    RaiseCanvasEvent(previousFocus, true);
}
```

```csharp
int order = state && UIManager.instance != null &&
            UIManager.instance.IsCanvasFocused(canvasToToggle)
    ? UIManager.FocusOrder
    : UIManager.DefaultOrder;
canvas.sortingOrder = order;
```

### 简短口播文案

“UI 这部分我比较想强调的是焦点管理。因为项目里会同时存在背包、任务、技能树、存档这些面板，如果只做简单开关，很容易出现层级混乱。所以我专门做了一个 UIManager 去维护打开顺序、当前焦点，以及 ESC 的关闭逻辑。拖拽窗口时也会自动把当前窗口提到最上层。”

### 剪辑提示

- 这一段很适合做左右分屏：左边实机拖拽窗口，右边显示 `ApplyFocusChange`。
- 不要讲太多 Unity 组件基础概念，重点只讲“为什么需要 focus 机制”。

---

## 阶段 4：任务 + 对话 + ScriptableObject 数据驱动（3:35 - 5:55）

### 这一段要传达什么

- 任务和对话不是写死在代码里的，而是放进了 `ScriptableObject` 里配置。
- 运行时再由 `QuestManager`、`DialogManager`、历史记录管理器去驱动状态变化。
- 这段最能体现“数据驱动 + 状态流转 + 事件解耦”。

### 推荐你用的实际演示素材

- 任务资产：`Assets/GameSO/UI SO/QuestSO/PickAndChat.asset`
- 对话资产：`Assets/GameSO/ChatSOs/PurpleBobChats/NormalChats/PurpleBobPickMushroom.asset`
- 条件拒绝对话：`Assets/GameSO/ChatSOs/PurpleBobChats/ChatsWhileRefuse/RefuseByPickMushRoom.asset`

### 建议录制的游戏画面

- 先和 NPC 对话，触发任务。
- 打开任务面板，看见目标列表。
- 去采集蘑菇，任务进度更新。
- 再回去对话，完成任务并发奖励。
- 如果时间够，可以补一个“条件不满足时进入拒绝对话”的镜头。

### 建议录制的编辑器画面

- 选中 `PickAndChat.asset`，展示：
- `questDescription`
- 两个 objective
- reward 列表
- 再选中 `PurpleBobPickMushroom.asset`，展示：
- `dialogLines`
- `nextDialogOptions`
- `refuseDialogs`
- 再切到 `PersistentScene` 中的 `QuestManager`。
- 中间可以插 `Docs/UML/ScriptableObjects_and_Events.png` 或 `Docs/UML/Dialog_Inventory_System.png`。

### 建议贴的代码

- `Assets/Scripts/UI/CanvasManagers/QuestManager.cs`
- 重点看 `QuestProgressData`
- `OnReFreshQuestState`
- `QuestStateChanged`
- `UpdateObjectiveProgress`
- `Assets/Scripts/UI/DialogScripts/DialogManager.cs`
- 重点看 `MatchConditionsToStartDialog`
- `HasRefuseConditions`
- `EndDialog`
- `Assets/Scripts/UI/DialogScripts/HistoryManager/ConversationHistoryManager.cs`
- `Assets/Scripts/UI/DialogScripts/HistoryManager/ItemHistoryManager.cs`
- `Assets/Scripts/ScriptableObjects/QuestSO.cs`
- `Assets/Scripts/ScriptableObjects/DialogSO.cs`

### 最适合截图的代码片段

```csharp
if (obj.targetItem != null)
{
    newAmount = ItemHistoryManager.instance.GetItemQuantity(obj.targetItem);
}
else if (obj.targetCharacter != null &&
         ConversationHistoryManager.instance.HasChatedWith(obj.targetCharacter))
{
    newAmount = obj.requiredAmount;
}
```

```csharp
foreach (var refuse in dialog.refuseDialogs)
{
    bool shouldRefuse = !HasRefuseConditions(refuse);
    if (shouldRefuse)
    {
        StartRefuseDialog(refuse);
        return false;
    }
}
```

### 简短口播文案

“任务和对话这块我尽量做成了数据驱动。比如任务目标、奖励、对话选项和拒绝条件都放在 ScriptableObject 里配置。运行时 QuestManager 负责维护状态，DialogManager 负责根据历史记录和道具数量决定能不能进入某段对话。这样做的好处是后面扩内容时，很多逻辑不用再改底层代码。”

### 剪辑提示

- 这一段不要把任务、对话、事件拆成 3 段讲，合在一起更紧凑。
- 重点是让观众看到：`资产配置 -> 游戏内触发 -> 状态变化` 这一条链路。

---

## 阶段 5：存档 / 读档（5:55 - 7:35）

### 这一段要传达什么

- 你的存档不是“按按钮直接存个位置”，而是有完整的数据收集流程。
- 可保存对象通过 `ISaveable` 注册到 `DataManager`，再统一交给 `SaveSystem` 序列化成 JSON。
- 这里还有一些值得讲的小细节：`自动存档`、`菜单场景下 Save 变 Delete`、`坏档过滤`、`删除路径校验`。

### 建议录制的游戏画面

- 在场景里打开 `Save/Load` 面板，存一次档。
- 然后移动角色、拾取道具或切一次场景。
- 再读档，回到之前状态。
- 最后切回菜单场景，展示同一套界面里 `Save` 按钮变成 `Delete`。

### 建议录制的编辑器画面

- 在 `PersistentScene` 中选中：
- `DataManager`
- `SaveSystem`
- `SaveCanvasPanelManager`
- 再打开 `Loot` prefab 或某个可拾取物，说明它实现了 `ISaveable`。
- 如果你愿意加一个 5 秒补充镜头，可以用 VSCode 或资源管理器打开一份 JSON 存档，快速展示：
- `playerStatsData`
- `sceneIDAndPlayerPos`
- `lootsStatsDic`
- 可插入 `Docs/UML/Scene_SaveLoad_System.png`。

### 建议贴的代码

- `Assets/Scripts/SaveAndLoad/ISaveable.cs`
- `Assets/Scripts/SaveAndLoad/DataManager.cs`
- 重点看 `PrepareManualSaveData`
- `OnAutoSave`
- `LoadFromData`
- `Assets/Scripts/SaveAndLoad/SaveSystem.cs`
- 重点看 `WriteSave`
- `DeleteSave`
- `LoadSave`
- `IsLoadableSaveFile`
- `Assets/Scripts/Inventory/Items/Loot.cs`
- 重点看 `SaveData` / `LoadData`

### 最适合截图的代码片段

```csharp
foreach (var saveable in saveables.ToList())
{
    saveable.SaveData(dataToSave);
}
```

```csharp
string saveID = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
string path = Application.persistentDataPath + $"/{saveType}_{saveID}.json";
File.WriteAllText(path, json);
```

```csharp
if (!fullSavePath.StartsWith(fullPersistentPath, StringComparison.OrdinalIgnoreCase))
{
    Debug.LogWarning($"Save file is outside persistentDataPath: {savePath}");
    return false;
}
```

### 简短口播文案

“存档这部分我做成了注册式的结构。实现了 ISaveable 的对象会统一交给 DataManager 收集数据，再由 SaveSystem 负责写入 JSON。这样新增一个可保存对象时，只需要实现接口，不需要把保存逻辑到处手动接线。另外这里我也做了坏档过滤和删除路径校验，保证读档和删档更安全。”

### 剪辑提示

- 这一段比起大段代码，更适合 `流程图 + 一小段关键代码 + 一闪而过的 JSON`。
- 不用详细讲 Newtonsoft.Json，只要说明“最终落成 JSON 文件”就够了。

---

## 阶段 6：收尾总结（7:35 - 8:20）

### 这一段要传达什么

- 这个 demo 的价值不只是“做了几个功能”，而是已经有了比较清晰的系统边界。
- 你具备把功能做成可扩展结构的意识。

### 建议录制的画面

- 回放前面 4 个系统各 1~2 秒的精彩镜头。
- 或者用一张 UML 图做背景，叠加总结字幕。

### 简短口播文案

“这个 demo 目前最核心的部分，是我把玩法背后的系统关系梳理出来了。比如寻路做成了可复用组件，UI 有统一焦点管理，任务和对话做成了数据驱动，存档也做成了统一注册和序列化流程。后续如果继续扩展内容，我会优先在这些系统上继续迭代，而不是推倒重来。”

### 如果你想再补一句未来优化

可以只选一句，不要讲多：

- “A* 后面还可以继续优化成更高效的 open list 结构。”
- “任务目标类型后面还可以继续扩展，比如位置触发或击杀目标。”
- “UI 这部分后面还可以补更完整的动画和移动端适配。”

---

## 可选补充段：场景切换和持久场景（时长不够 7 分钟时再加）

如果你发现整片只有 6 分多钟，可以补一个 `30~40 秒` 的场景切换段。

### 建议讲法

- `InitialScene` 启动后会异步加载 `PersistentScene`
- `SceneChanger` 负责切场景、淡入淡出和玩家位置切换
- `PersistentScene` 里挂着 UI、任务、存档这类跨场景系统

### 建议录制的编辑器画面

- `Assets/Scenes/InitialScene.unity`
- `Assets/Scenes/GameScene/PersistentScene.unity`
- `Assets/Scripts/Scene/SceneChanger.cs`

### 简短口播文案

“项目里还有一个比较关键的基础设施是持久场景。像 UIManager、QuestManager、SaveSystem 这类跨场景都要存在的系统，会放在 PersistentScene 里统一管理，这样场景内容和全局系统可以拆得更开一些。”

---

## 最后给你的录制顺序建议

不要按最终成片顺序录，建议按下面顺序准备素材：

1. 先录所有 `游戏实机`
2. 再录 `Unity 编辑器` 的 Inspector / Scene / Hierarchy
3. 最后再录 `代码镜头`
4. 剪辑时统一按“效果 -> 编辑器 -> 代码”拼起来

这样效率最高，也最不容易漏素材。

## 需要避开的坑

- 不要一上来就讲代码，先让别人看到效果。
- 不要把每个系统都讲成“从头实现教程”，你的视频是介绍，不是教学。
- 不要贴整屏代码，面试官和老师不会认真看完一整页。
- 不要把背包、商店、技能树都展开讲，否则 10 分钟一定超。
- 任务和对话一定要合并讲，不然节奏会碎。

## 一句话版总思路

如果你最后只记住一句话，那这支视频就按这个逻辑讲：

“我这个 demo 不只是把玩法做出来了，更重要的是把寻路、UI、任务对话和存档这些核心系统拆开并串联起来了。”
