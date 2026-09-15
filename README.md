# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-000000?logo=unity)
![License](https://img.shields.io/badge/License-GPL--3.0-blue)
![Language](https://img.shields.io/badge/C%23-74%25-512BD4?logo=csharp)

一个基于 Unity 2022.3.62f3c1 开发的 2D 俯视角 ARPG 原型项目，涵盖对话、任务、商店、背包、技能树与存读档等完整 RPG 系统的事件驱动架构实现。

> **English:** [README.en.md](README.en.md)

## 特性

- **完整的 RPG 系统闭环**：场景切换、NPC 对话（分支 + 条件 + 历史）、任务系统（状态机）、商店、背包、技能树、存读档。
- **事件驱动架构**：基于 ScriptableObject 事件通道的跨系统解耦通信。
- **三层解耦 A\* 寻路**：网格管理 / 寻路算法 / MovementController 各自独立，挂载即可用。
- **2D 战斗**：俯视角探索，近战与远程两种模式。
- **工程化实践**：Addressables 场景加载、Newtonsoft.Json 存档、Android 导出排障文档。

## 演示视频

- [【Unity】28届 ARPG Demo项目展示](https://www.bilibili.com/video/BV1sCGH6iErJ/)（Bilibili，发布于 2026-05-25）

> 该视频介绍的是**早期版本**的项目。此后项目持续迭代，操作方式、系统实现与场景内容均有调整，视频内容可能与当前版本不符或已过时，仅供参考；一切以本 README 和仓库代码为准。

## 目录

- [运行环境与快速开始](#运行环境与快速开始)
- [操作说明](#操作说明)
- [项目结构](#项目结构)
- [核心系统架构](#核心系统架构)
- [分层架构与重构现状](#分层架构与重构现状)
- [构建指南](#构建指南)
- [ScriptableObject 使用建议](#scriptableobject-使用建议)
- [已知限制](#已知限制)
- [借物表](#借物表)
- [许可证](#许可证)

## 运行环境与快速开始

**环境要求**

- 推荐使用 Unity 2022.3.62f3c1 打开项目。
- 主要依赖包括 Addressables、Cinemachine、Input System、TextMesh Pro 和 Unity 2D 功能包。

**启动流程**

1. 用 Unity Hub 打开项目，等待包依赖和 Addressables 数据导入完成。
2. 从 `Assets/Scenes/InitialScene.unity` 启动（默认构建入口，会在启动时异步加载 `PersistentScene`）。
3. 标题菜单位于 `Assets/Scenes/GameScene/StartingMenu.unity`，主要游戏场景为 `Assets/Scenes/GameScene/Scene1.unity` 和 `Scene2.unity`。
4. `Assets/Scenes/TestScene.unity` 可用于独立调试，但要验证完整流程时更建议从 `InitialScene` 启动。

## 操作说明

**按键**

| 按键 | 功能 |
|---|---|
| `WASD` | 移动角色 |
| `Q` | 切换远程 / 近战模式 |
| `J` | 弓箭手射箭 |
| `K` | 剑士挥砍 |
| `1` | 打开能力面板 |
| `2` | 打开技能面板（左键技能槽消耗技能点解锁技能） |
| `F` | 与商店交互 |
| `T` | 打开或关闭 NPC 对话（左键推进对话或选择选项） |
| `C` | 打开任务菜单 |
| `ESC` | 打开退出菜单（返回标题、存档或退出游戏） |

**界面通用操作**：大多数面板支持点击 `X` 关闭，并可拖动顶部标题栏调整位置；左下角集成菜单可以打开大多数界面，但部分功能需要先靠近 NPC、商店或任务板触发。

**物品与存档**

- 左键物品栏：未打开商店时使用物品，打开商店时出售物品；右键丢弃。
- 游戏场景内可以 `Save` / `Load`，菜单场景内 `Save` 会切换为 `Delete`。
- 丢弃的物品在场景切换、重载或 `Retry` 后不会保留。
- 木桥走到尽头可以切换场景，角色死亡后会自动弹出 `GameOver` 菜单。

## 项目结构

脚本按层分三个顶层目录：跨功能的契约、按功能域组织的玩法层、与玩法无关的管线基建。

- `Assets/Scripts/Contracts`：跨层契约与基建——`ISaveable`、`IDamageable`、`ICanvasManager`、静态存档注册表 `SaveRegistry`、单例基类 `YSingleton`。
- `Assets/Scripts/Gameplay`：按功能域分目录，域内再按层分 `Models/`、`Services/`、`Controllers/`、`Views/`。
  - `Player/`：玩家数值、移动、战斗、装备。
  - `Quest/`、`Dialog/`、`Inventory/`、`Shop/`、`Skills/`：任务、对话、背包、商店、技能树。
  - `Units/`：敌人、NPC 与商店 NPC 行为。
  - `Save/`：`SaveDataManager` 与存读档流程。
  - `Grid/`：网格数据。
- `Assets/Scripts/Pipeline`：与玩法无关的基建，含 A\* 寻路、场景切换与加载、配置资产与事件通道、UI 基建。

分层约定见 [分层架构与重构现状](#分层架构与重构现状)。

## 核心系统架构

### 存档系统

采用 `ISaveable` 接口 + 注册表模式统一管理所有可持久化对象。

- **`ISaveable`** 接口定义 `SaveData(Data)` / `LoadData(Data)` 方法，实现该接口的类（Loot、InventoryManager 等）在激活时注册到 `SaveRegistry`。
- **`SaveRegistry`** 位于 `Contracts/SaveRegistry.cs`，是静态注册表，先于一切场景实例存在，注册与注销在任何生命周期阶段调用都安全，不受 `Awake` 顺序影响；`SaveDataManager` 只做收集与分发，存读档时遍历 `SaveRegistry.All`。
- **`SaveSystem`** 负责序列化（Newtonsoft.Json）和文件 I/O，支持手动存档与自动系统档分离：自动存档在场景切换时触发，手动存档由玩家操作触发。
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

## 分层架构与重构现状

项目采用轻量 MVCS 分层：Model 承载一个数据聚合的状态与规则，Service 是域的写入口，Controller 承担翻译，View 只写控件。玩家数值这条线按此分层，其余功能域是原有的 Manager 形态。

- **写路径**：按钮点击、SO 事件通道、碰撞这类输入源进入输入侧 Controller，被翻译成意图，经 Service 这个唯一写入口落进 Model，聚合内规则在 Model 里执行。
- **读路径**：Model 状态变化发 C# 事件，显示侧 Controller 把数据翻译成显示参数，View 用 `SetXxx` 写控件。
- **三类边界**：规则只读自己字段的进 Model；跨聚合、管生命周期与存档的进 Service；持久状态必须经 Service 写进 Model，绕过 Model 的状态读路径刷不出来，也存不进存档。

完整准则、九步迁移流程、三层测试策略、风险与完成标准在个人知识库 `D:\My_Docs\1TODOFiles\Learning\` 的 `MVCS重构方法论.md` 与 `MVCS笔记.md`，这两份不绑定具体工程，不随本仓库分发。本工程的迁移现状、已确认缺陷与剩余清单收在 [`Docs/`](Docs/README.md) 的 `My_ARPG_MVCS项目现状.md`、`My_ARPG_重构优化清单_未解决.md`、`My_ARPG_重构优化清单_已解决.md`。

> **项目状态：分层重构冻结于 tag `arpg-arch-final`。** 玩家数值线按四层组织；任务、对话、背包与商店、存档与场景编排、移动战斗寻路保留原有的 Manager 形态。技能域是保留的验证点：技能点属于数值聚合，消耗发生在技能聚合，「一次写要原子地改动两个数据聚合」这条准则在本工程只有它能验证。

## 构建指南

> 本节只保留最关键的结论，Android 完整排障见 Docs 目录文档。

### Addressables 要点

- **数据构建器必须为 Packed Mode**：项目已开启 `Build Addressables on Player Build`，但前提是活动数据构建器为 Packed Mode（`m_ActivePlayerDataBuilderIndex = 3`）。误切回 Use Asset Database / Simulate Groups 会导致导出包中场景缺失或运行时报错。
- **失效引用会直接卡断构建**：组里引用了已删除/重命名资源时构建会报错甚至整体失败。改资源名/路径后，记得同步检查 Addressables 组，或重新打开 `Window > Asset Management > Addressables > Groups` 让它刷新。
- **Content Update 依赖 `addressables_content_state.bin`**：该文件按平台存放在 `Windows/`、`Android/` 等目录，且已被 `.gitignore` 忽略；换机器或清理后若丢失，需先做一次 Clean Build 重建。
- **入口场景必须在 Build Settings 里**：打包后的运行入口是 `InitialScene`（不在 Addressables 组内，由 Player Settings 直接打进包），它再通过 `GameSceneSO.sceneReference` 异步加载其余场景；缺失会导致空包启动。
- 远程组产物输出到 `ServerData/[BuildTarget]`；本仓库默认本地构建（`m_CCDEnabled = 0`），无需远端。若将来启用远程目录，须保证 LoadPath 与实际托管地址一致。

### Android 导出

- **Android Release 构建（2026-08-09 已验证通过）**：SDK 检测卡住源于 `sdkmanager` 未继承代理；Release lint 失败源于 `StreamingAssets` 中的非 ASCII 文件名。代理配置、卸载、环境自检与排障步骤见 [`Docs/UnityAndroidBuildGuide.md`](Docs/UnityAndroidBuildGuide.md)（**不绑定具体机器**：先按「第一步：确认环境」读出你机器上的真实路径与代理端口再照做），构建结果存档见 [`Docs/UnityAndroidBuildVerification.md`](Docs/UnityAndroidBuildVerification.md)。
- 当前测试 APK 使用 Debug 证书，正式发布前需配置项目专用 keystore。

## ScriptableObject 使用建议

- **资源名 / 路径改动后检查引用**：项目大量依赖 SO 作为数据容器与事件通道（`DialogSO`、`QuestSO`、`GameSceneSO`、各种 `*EventSO` 等）。重命名或移动 SO 后，引用它的字段可能变成 `Missing` 并在运行时静默失效，建议改名后按 GUID / `Missing` 批量核对一次。
- **事件 SO 的订阅与注销必须成对**：统一约定在 `OnEnable` 里 `+=` 订阅、`OnDisable` 里 `-=` 注销（参见 `SaveDataManager`、`SceneChanger`、`PlayerBow` 等）。新增订阅者务必遵守，否则场景切换 / 对象销毁后会出现重复触发或空引用。
- **`GameSceneSO.ID` 与 `GuidSO` 的 GUID 生成后勿改**：存档体系（`ISaveable`/`SaveRegistry`）通过 ID 关联对象，清空或改动 GUID 会导致存档找不到目标。注意 `OnValidate` 仅在编辑器下运行，不要依赖它在运行时生成 ID。
- **`GameSceneSO.sceneReference` 必须赋值**：它是 `AssetReference`，必须指向已纳入 Addressables 的场景资产，为空会在运行时抛 `InvalidKeyException` 之类错误。
- **避免在 SO 实例上存游戏运行时状态**：SO 是共享资产，运行时数据应放在专门的运行时类里（如 `QuestProgressData`），否则多份引用共享同一份被篡改的数据，且容易污染编辑器中的资产值。

## 已知限制

- 标题场景中的 `Settings` 入口目前仍未实现。
- 部分菜单依赖交互范围或上下文状态，不是任何时刻都能直接打开。
- 当前文档以现有工程和 `GameGuide.txt` 为准，若后续功能调整请同步更新。

## 借物表

- 美术资源：Tiny Swords by Pixel Frog https://pixelfrog-assets.itch.io/tiny-swords ，基于资产包许可使用，不单独再分发。

## 许可证

- 许可证信息见根目录 `LICENSE`。
