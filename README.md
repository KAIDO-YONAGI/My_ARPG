# My_ARPG

一个基于 Unity 2022.3.62f3c1 开发的 2D ARPG 原型项目。

> **English:** [README.en.md](README.en.md)

## 项目概述

- 当前版本围绕场景切换、NPC 对话、任务系统、商店、背包、技能树和存档读档展开。

- 游戏采用 2D 俯视角探索与战斗，包含近战与远程两种战斗模式。

## 运行环境

- 推荐使用 Unity 2022.3.62f3c1 打开项目。

- 主要依赖包括 Addressables、Cinemachine、Input System、TextMesh Pro 和 Unity 2D 功能包。

## 快速开始

- 用 Unity Hub 打开项目后，先等待包依赖和 Addressables 数据导入完成。

- 默认构建入口是 `Assets/Scenes/InitialScene.unity`，它会在启动时异步加载 `PersistentScene`。

- 标题菜单场景位于 `Assets/Scenes/GameScene/StartingMenu.unity`，主要游戏场景位于 `Assets/Scenes/GameScene/Scene1.unity` 和 `Assets/Scenes/GameScene/Scene2.unity`。

- `Assets/Scenes/TestScene.unity` 可用于独立调试，但要验证完整流程时更建议从 `InitialScene` 启动。

## 主要功能

- 跨场景加载与切换，带淡入淡出效果和持久场景管理。

- NPC 对话支持分支、条件检查和历史记录。

- 任务系统包含任务板、任务日志、目标、奖励和状态刷新。

- 背包与物品系统支持拾取、使用、出售、丢弃和部分持久化。

- 商店、技能树、状态面板和集成菜单已经打通到同一套 UI 管理逻辑。

- 存档系统支持在游戏场景中保存和读取，在菜单场景中删除存档。

- A* 寻路用于敌人与 NPC 的部分移动逻辑。

## 操作说明

- `WASD`：移动角色。

- `Q`：切换远程 / 近战模式。

- `J`：弓箭手射箭。

- `K`：剑士挥砍。

- `1`：打开能力面板。

- `2`：打开技能面板，并可左键技能槽消耗技能点解锁技能。

- `F`：与商店交互。

- `T`：打开或关闭 NPC 对话，并可左键推进对话或选择选项。

- `C`：打开任务菜单。

- `ESC`：打开退出菜单，可返回标题、存档或退出游戏。

- 大多数面板支持点击 `X` 关闭，并可拖动顶部标题栏调整位置。

- 左下角集成菜单可以打开大多数界面，但部分功能需要先靠近 NPC、商店或任务板触发。

## 物品与存档说明

- 左键物品栏时，未打开商店会使用物品，打开商店时会出售物品。

- 右键物品栏会丢弃物品。

- 游戏场景内可以 `Save` / `Load`，菜单场景内 `Save` 会切换为 `Delete`。

- 丢弃的物品在场景切换、重载或 `Retry` 后不会保留。

- 木桥走到尽头可以切换场景，角色死亡后会自动弹出 `GameOver` 菜单。

## 项目结构

- `Assets/Scripts/UI`：UI 管理、对话、任务板、商店、技能树和菜单交互。

- `Assets/Scripts/Units`：敌人、NPC 和商店 NPC 的行为脚本。

- `Assets/Scripts/Player`：玩家移动、战斗、装备切换、时间与属性管理。

- `Assets/Scripts/Inventory`：背包、物品槽、拾取物和使用逻辑。

- `Assets/Scripts/SaveAndLoad`：数据结构、存档接口和存读档流程。

- `Assets/Scripts/Scene` 与 `Assets/Scripts/A Star`：场景切换和寻路相关逻辑。

- `Assets/Scripts/ScriptableObjects`：任务、场景、对话和事件类 SO 定义。

## 核心系统架构

### 存档系统

采用 `ISaveable` 接口 + 注册表模式统一管理所有可持久化对象。

- **`ISaveable`** 接口定义 `SaveData(Data)` / `LoadData(Data)` 方法，实现该接口的类（Loot、InventoryManager 等）在激活时自动注册到 `DataManager`。
- **`DataManager`** 维护 `List<ISaveable>` 注册表，场景切换时统一调用所有已注册对象的存/读方法。
- **`SaveSystem`** 负责序列化（Newtonsoft.Json）和文件 I/O，支持手动存档与自动系统档分离。
- 自动存档在场景切换时触发；手动存档由玩家操作触发。
- 存档安全：删除前校验路径不超出 `persistentDataPath`；加载时自动跳过损坏存档并回退到最近的完整档。

### 对话系统

基于 `DialogSO` ScriptableObject 构建树状对话图，支持条件分支和历史记录。

- 每个 `DialogSO` 节点包含对话行（`dialogLines`）和子选项（`nextDialogOptions`），形成对话树。
- 条件分支通过 `RefuseDialogSO` 实现：对话开始前检查前置条件（角色是否对话过、物品是否拾取够数量），不满足则展示拒绝对话。
- `onlyTriggeredOnce` 标记实现一次性对话，`ConversationHistoryManager` 记录对话历史。
- `ItemHistoryManager` 跟踪物品拾取历史，供条件检查和任务目标使用。

### 任务系统

基于状态机的任务管理，支持多目标类型和自动状态推进。

- 任务状态机：`Idle → Accepted → IsToComplete → Completed`，带 `Decline` 分支可回退到 `Accepted`。
- `QuestProgressData` 内部类用 `Dictionary<QuestObjective, int>` 管理每个目标的当前进度。
- 目标类型支持物品拾取数量检查（通过 `ItemHistoryManager`）和角色对话检查（通过 `ConversationHistoryManager`）。
- 目标达成后自动推进状态到 `IsToComplete`；完成任务自动通过事件系统发放奖励到背包。

### A* 寻路系统

采用三层解耦架构：网格管理、寻路算法、路径消费各自独立，NPC 和敌人只需挂载 `MovementController` 即可获得寻路能力。

- **`AStarNodeManager`** — 网格数据层。从 Tilemap 和 Collider2D 自动构建节点地图，支持可步行/障碍节点标记，提供世界坐标↔网格坐标转换和安全边距（避免贴墙移动）。
- **`AStarPathFinder`** — 寻路算法层。标准 A* 实现，支持 8 方向移动、对角线通行检查（`CanWalkDiagonally`）、起点优化（`NoCoverObstacleNodes` 直接直线移动到最优起点）。
- **`MovementController`** — 路径消费层。可挂载到任意 GameObject，提供 `GetPosToGo()` 获取当前目标点、`ArrivedPos()` 消费节点。内置重寻路机制（目标移动超过阈值时自动重建路径，新旧路径比较后决定是否替换）和冷却计时器防止频繁重算。Scene View 中通过 Gizmos 可视化路径。

### 事件驱动

系统间通信通过 ScriptableObject 事件通道解耦。

- 定义了多种事件 SO（`VoidEventSO`、`DataSaveEventSO`、`QuestOptionsEventSO`、`SceneLoadEventSO` 等），广播方 Raise 事件，接收方订阅回调。
- 存档、任务奖励、场景加载、UI 切换等跨系统操作均通过事件传递，避免直接引用。

## 构建建议

> 本节记录在导出 / 打包过程中容易踩坑的点，尤其是 Addressables 与 ScriptableObject（SO）相关的注意事项。

### Addressables 相关

- **构建入口与数据构建器**：项目已开启 `Build Addressables on Player Build`（`AddressableAssetSettings` 中 `m_BuildAddressablesWithPlayerBuild = 1`），打包时会自动构建 Addressables。但前提是**活动数据构建器为 Packed Mode**（`m_ActivePlayerDataBuilderIndex = 3`）。如果误切回 Use Asset Database / Simulate Groups 模式，运行时场景会无法被实际打包出来，导致导出包中场景缺失或运行时报错。

- **失效 / 缺失的引用会直接卡断构建**：Addressables 组里如果引用了已被删除或重命名的资源，构建阶段会报错甚至整体失败。当前 `Scenes` 组里残留了一条指向 `Assets/Scenes/Menu.unity` 的引用，而实际菜单场景是 `Assets/Scenes/GameScene/StartingMenu.unity` —— 这类失效引用（路径错或 GUID 失效）需要在导出前清理。**改资源名 / 路径后，记得同步检查 Addressables 组，或重新打开 `Window > Asset Management > Addressables > Groups` 让它刷新。**

- **内容更新前的 Content Update 依赖 `addressables_content_state.bin`**：做增量内容更新（Content Update Build）前，项目目录下需要存在有效的 `addressables_content_state.bin`（每个平台一份，如 `Windows/`、`Android/`、`WebGL/`）。该文件已在 `.gitignore` 中被忽略，换机器或清理后若丢失，需先做一次 Clean Build 重建它，否则 Content Update 会失败或行为异常。

- **`ServerData/` 与本地运行**：Remote 组的产物会输出到 `ServerData/[BuildTarget]`。本仓库默认走本地构建、不启用远程托管（`m_CCDEnabled = 0`），所以不必依赖远端；但若将来开启远程目录，务必保证 `Local.LoadPath` / `Remote.LoadPath` profile 与实际托管地址一致，否则运行时找不到资源。

- **场景加载入口**：打包后的运行入口是 `InitialScene`（不在 Addressables 组内，由 Player Settings 直接打进包），它再通过 `GameSceneSO.sceneReference`（`AssetReference`）异步加载 `PersistentScene` 与游戏场景。因此 **入口场景必须保留在 Build Settings 的 Scenes 列表里**，否则空包启动。

### Android 导出

- **Android Release 构建已于 2026-08-09 验证通过**：SDK 检测卡住是 `sdkmanager` 未使用本机代理；Release lint 失败是 `StreamingAssets` 中的非 ASCII 文件名导致 AAR 条目解码异常。全局代理配置、卸载和排障步骤见 [`Docs/UnityAndroidGlobalBuildFix.md`](Docs/UnityAndroidGlobalBuildFix.md)。当前测试 APK 使用 Debug 证书，正式发布前仍需配置项目专用 keystore。

### ScriptableObject（SO）使用建议

- **资源名 / 路径改动后检查引用**：项目大量依赖 SO 作为数据容器与事件通道（`DialogSO`、`QuestSO`、`GameSceneSO`、各种 `*EventSO` 等）。重命名或移动 SO 资源后，Inspector 里引用它的字段可能变成 `Missing`，运行时静默失效。建议改名后用搜索（如按 GUID / `Missing`）批量核对一次。

- **事件 SO 的订阅与注销必须成对**：自定义事件通道（`VoidEventSO`、`DataSaveEventSO`、`QuestOptionsEventSO` 等）通过 `UnityAction` 委托广播。本项目统一约定在 `OnEnable` 里 `+=` 订阅、`OnDisable` 里 `-=` 注销（参见 `DataManager`、`RetryManager`、`PlayerBow` 等）。**新增订阅者务必遵守此约定**，否则场景切换 / 对象销毁后会出现重复触发或空引用。

- **`GameSceneSO.ID` 与 `GuidSO` 的稳定标识**：`GameSceneSO` 在 `OnValidate` 里用 `System.Guid` 自动生成 `ID`；`GuidSO` 也会在为空时自动填充 GUID 并 `SetDirty`。这意味着 **SO 的 GUID 一旦生成就不应手动清空或随意改动**，否则存档（`ISaveable`/`DataManager` 体系通过 ID 关联对象）会找不到对应目标。同时注意：`OnValidate` 仅在编辑器下运行，不要依赖它在打包后的运行时生成 ID。

- **`GameSceneSO.sceneReference` 必须赋值**：`sceneReference` 是 `AssetReference`，必须指向已纳入 Addressables 的场景资产。若为空，场景加载流程会在运行时抛出 `InvalidKeyException` 之类错误。新建 `GameSceneSO` 后，先把对应场景拖进 `sceneReference`，并确认该场景已出现在 `Scenes` 组里。

- **避免直接在 SO 实例上存“游戏运行时状态”**：SO 是共享资产。本项目把运行时状态放在专门的运行时类里（如 `QuestProgressData` 用 `Dictionary<QuestObjective,int>` 跟踪进度），而不是写回 `QuestSO`。**不要把会变化的运行时数据直接塞进 SO 字段**，否则多份引用共享同一份被篡改的数据，且容易污染编辑器中的资产值。

## 已知限制

- 标题场景中的 `Settings` 入口目前仍未实现。

- 部分菜单依赖交互范围或上下文状态，不是任何时刻都能直接打开。

- 当前文档以现有工程和 `GameGuide.txt` 为准，若后续功能调整请同步更新。

## 借物表

- 美术资源：Tiny Swords by Pixel Frog https://pixelfrog-assets.itch.io/tiny-swords ，基于资产包许可使用，不单独再分发。

## 许可证

- 许可证信息见根目录 `LICENSE`。
