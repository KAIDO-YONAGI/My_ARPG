# My_ARPG

一个基于 Unity 2022.3.62f3c1 开发的 2D ARPG 原型项目。
A 2D ARPG prototype built with Unity 2022.3.62f3c1.

## 项目概述
## Overview

- 当前版本围绕场景切换、NPC 对话、任务系统、商店、背包、技能树和存档读档展开。
- The current version centers on scene transitions, NPC dialogue, quests, shops, inventory, skill trees, and save/load.

- 游戏采用 2D 俯视角探索与战斗，包含近战与远程两种战斗模式。
- The game uses 2D top-down exploration and combat, with both melee and ranged modes.

## 运行环境
## Requirements

- 推荐使用 Unity 2022.3.62f3c1 打开项目。
- Open the project with Unity 2022.3.62f3c1.

- 主要依赖包括 Addressables、Cinemachine、Input System、TextMesh Pro 和 Unity 2D 功能包。
- Main dependencies include Addressables, Cinemachine, Input System, TextMesh Pro, and the Unity 2D feature set.

## 快速开始
## Quick Start

- 用 Unity Hub 打开项目后，先等待包依赖和 Addressables 数据导入完成。
- After opening the project in Unity Hub, wait for package dependencies and Addressables data to finish importing.

- 默认构建入口是 `Assets/Scenes/InitialScene.unity`，它会在启动时异步加载 `PersistentScene`。
- The default build entry is `Assets/Scenes/InitialScene.unity`, which asynchronously loads `PersistentScene` on startup.

- 标题菜单场景位于 `Assets/Scenes/GameScene/StartingMenu.unity`，主要游戏场景位于 `Assets/Scenes/GameScene/Scene1.unity` 和 `Assets/Scenes/GameScene/Scene2.unity`。
- The title menu scene is located at `Assets/Scenes/GameScene/StartingMenu.unity`, and the main gameplay scenes are `Assets/Scenes/GameScene/Scene1.unity` and `Assets/Scenes/GameScene/Scene2.unity`.

- `Assets/Scenes/TestScene.unity` 可用于独立调试，但要验证完整流程时更建议从 `InitialScene` 启动。
- `Assets/Scenes/TestScene.unity` can be used for isolated testing, but `InitialScene` is the better entry point for validating the full flow.

## 主要功能
## Main Features

- 跨场景加载与切换，带淡入淡出效果和持久场景管理。
- Cross-scene loading and transitions with fade effects and persistent-scene management.

- NPC 对话支持分支、条件检查和历史记录。
- NPC dialogue supports branching, condition checks, and history tracking.

- 任务系统包含任务板、任务日志、目标、奖励和状态刷新。
- The quest system includes quest boards, a quest log, objectives, rewards, and state refresh.

- 背包与物品系统支持拾取、使用、出售、丢弃和部分持久化。
- The inventory and item system supports pickup, use, selling, dropping, and partial persistence.

- 商店、技能树、状态面板和集成菜单已经打通到同一套 UI 管理逻辑。
- Shops, skill trees, stat panels, and the integrated menu are wired into one UI management flow.

- 存档系统支持在游戏场景中保存和读取，在菜单场景中删除存档。
- The save system supports saving and loading in gameplay scenes, and deleting saves in the menu scene.

- A* 寻路用于敌人与 NPC 的部分移动逻辑。
- A* pathfinding is used by parts of the enemy and NPC movement logic.

## 操作说明
## Controls

- `WASD`：移动角色。
- `WASD`: Move the character.

- `Q`：切换远程 / 近战模式。
- `Q`: Switch between ranged and melee modes.

- `J`：弓箭手射箭。
- `J`: Fire an arrow in archer mode.

- `K`：剑士挥砍。
- `K`: Perform a sword slash in melee mode.

- `1`：打开能力面板。
- `1`: Open the stats panel.

- `2`：打开技能面板，并可左键技能槽消耗技能点解锁技能。
- `2`: Open the skill panel, and left-click a skill slot to spend points and unlock skills.

- `F`：与商店交互。
- `F`: Interact with a shop.

- `T`：打开或关闭 NPC 对话，并可左键推进对话或选择选项。
- `T`: Open or close NPC dialogue, and left-click to advance dialogue or choose options.

- `C`：打开任务菜单。
- `C`: Open the quest menu.

- `ESC`：打开退出菜单，可返回标题、存档或退出游戏。
- `ESC`: Open the exit menu to return to title, save, or quit the game.

- 大多数面板支持点击 `X` 关闭，并可拖动顶部标题栏调整位置。
- Most panels can be closed with `X` and dragged by their top bar.

- 左下角集成菜单可以打开大多数界面，但部分功能需要先靠近 NPC、商店或任务板触发。
- The lower-left integrated menu can open most interfaces, but some functions require approaching an NPC, shop, or quest board first.

## 物品与存档说明
## Inventory and Save Notes

- 左键物品栏时，未打开商店会使用物品，打开商店时会出售物品。
- Left-clicking inventory items uses them when the shop is closed and sells them when the shop is open.

- 右键物品栏会丢弃物品。
- Right-clicking inventory items drops them.

- 游戏场景内可以 `Save` / `Load`，菜单场景内 `Save` 会切换为 `Delete`。
- In gameplay scenes you can `Save` / `Load`, and in the menu scene `Save` changes to `Delete`.

- 丢弃的物品在场景切换、重载或 `Retry` 后不会保留。
- Dropped items are not preserved after scene transitions, reloads, or `Retry`.

- 木桥走到尽头可以切换场景，角色死亡后会自动弹出 `GameOver` 菜单。
- Walking to the end of a wooden bridge can trigger a scene change, and the `GameOver` menu appears automatically when the character dies.

## 项目结构
## Project Structure

- `Assets/Scripts/UI`：UI 管理、对话、任务板、商店、技能树和菜单交互。
- `Assets/Scripts/UI`: UI management, dialogue, quest boards, shops, skill trees, and menu interactions.

- `Assets/Scripts/Units`：敌人、NPC 和商店 NPC 的行为脚本。
- `Assets/Scripts/Units`: Behavior scripts for enemies, NPCs, and shopkeepers.

- `Assets/Scripts/Player`：玩家移动、战斗、装备切换、时间与属性管理。
- `Assets/Scripts/Player`: Player movement, combat, equipment switching, time control, and stat management.

- `Assets/Scripts/Inventory`：背包、物品槽、拾取物和使用逻辑。
- `Assets/Scripts/Inventory`: Inventory, slots, loot, and item-use logic.

- `Assets/Scripts/SaveAndLoad`：数据结构、存档接口和存读档流程。
- `Assets/Scripts/SaveAndLoad`: Data structures, save interfaces, and the save/load flow.

- `Assets/Scripts/Scene` 与 `Assets/Scripts/A Star`：场景切换和寻路相关逻辑。
- `Assets/Scripts/Scene` and `Assets/Scripts/A Star`: Scene transition and pathfinding logic.

- `Assets/Scripts/ScriptableObjects`：任务、场景、对话和事件类 SO 定义。
- `Assets/Scripts/ScriptableObjects`: ScriptableObject definitions for quests, scenes, dialogue, and events.

## 核心系统架构
## Core System Architecture

### 存档系统 | Save System

采用 `ISaveable` 接口 + 注册表模式统一管理所有可持久化对象。

The save system uses an `ISaveable` interface + registry pattern to manage all persistable objects.

- **`ISaveable`** 接口定义 `SaveData(Data)` / `LoadData(Data)` 方法，实现该接口的类（Loot、InventoryManager 等）在激活时自动注册到 `DataManager`。
- **`DataManager`** 维护 `List<ISaveable>` 注册表，场景切换时统一调用所有已注册对象的存/读方法。
- **`SaveSystem`** 负责序列化（Newtonsoft.Json）和文件 I/O，支持手动存档与自动系统档分离。
- 自动存档在场景切换时触发；手动存档由玩家操作触发。
- 存档安全：删除前校验路径不超出 `persistentDataPath`；加载时自动跳过损坏存档并回退到最近的完整档。
- **`ISaveable`** defines `SaveData(Data)` / `LoadData(Data)`. Implementations (Loot, InventoryManager, etc.) self-register with `DataManager` on enable.
- **`DataManager`** holds a `List<ISaveable>` registry and invokes save/load on all entries during scene transitions.
- **`SaveSystem`** handles serialization (Newtonsoft.Json) and file I/O, with separate manual and automatic system saves.
- Auto-save triggers on scene transitions; manual save triggers from player actions.
- Safety: delete validates paths stay within `persistentDataPath`; loading skips corrupt files and falls back to the latest valid save.

### 对话系统 | Dialogue System

基于 `DialogSO` ScriptableObject 构建树状对话图，支持条件分支和历史记录。

The dialogue system builds tree-structured dialogue graphs from `DialogSO` ScriptableObjects, with conditional branching and history tracking.

- 每个 `DialogSO` 节点包含对话行（`dialogLines`）和子选项（`nextDialogOptions`），形成对话树。
- 条件分支通过 `RefuseDialogSO` 实现：对话开始前检查前置条件（角色是否对话过、物品是否拾取够数量），不满足则展示拒绝对话。
- `onlyTriggeredOnce` 标记实现一次性对话，`ConversationHistoryManager` 记录对话历史。
- `ItemHistoryManager` 跟踪物品拾取历史，供条件检查和任务目标使用。
- Each `DialogSO` node holds dialogue lines (`dialogLines`) and child options (`nextDialogOptions`), forming a dialogue tree.
- Conditional branching via `RefuseDialogSO`: before starting a dialogue, prerequisites are checked (character spoken to, items collected). Failure shows a refusal dialogue instead.
- `onlyTriggeredOnce` enables one-shot dialogues. `ConversationHistoryManager` tracks dialogue history.
- `ItemHistoryManager` tracks item pickup history for condition checks and quest objectives.

### 任务系统 | Quest System

基于状态机的任务管理，支持多目标类型和自动状态推进。

State-machine-based quest management with multi-objective types and automatic state progression.

- 任务状态机：`Idle → Accepted → IsToComplete → Completed`，带 `Decline` 分支可回退到 `Accepted`。
- `QuestProgressData` 内部类用 `Dictionary<QuestObjective, int>` 管理每个目标的当前进度。
- 目标类型支持物品拾取数量检查（通过 `ItemHistoryManager`）和角色对话检查（通过 `ConversationHistoryManager`）。
- 目标达成后自动推进状态到 `IsToComplete`；完成任务自动通过事件系统发放奖励到背包。
- Quest state machine: `Idle → Accepted → IsToComplete → Completed`, with a `Decline` branch that reverts to `Accepted`.
- `QuestProgressData` inner class uses `Dictionary<QuestObjective, int>` to track per-objective progress.
- Objective types include item pickup counts (via `ItemHistoryManager`) and character conversation checks (via `ConversationHistoryManager`).
- Completing all objectives auto-promotes to `IsToComplete`; finishing a quest auto-rewards items through the event system.

### A* 寻路系统 | A* Pathfinding System

采用三层解耦架构：网格管理、寻路算法、路径消费各自独立，NPC 和敌人只需挂载 `MovementController` 即可获得寻路能力。

Three-layer decoupled architecture: grid management, pathfinding algorithm, and path consumption are independent. NPCs and enemies only need a `MovementController` component to use pathfinding.

- **`AStarNodeManager`** — 网格数据层。从 Tilemap 和 Collider2D 自动构建节点地图，支持可步行/障碍节点标记，提供世界坐标↔网格坐标转换和安全边距（避免贴墙移动）。
- **`AStarPathFinder`** — 寻路算法层。标准 A* 实现，支持 8 方向移动、对角线通行检查（`CanWalkDiagonally`）、起点优化（`NoCoverObstacleNodes` 直接直线移动到最优起点）。
- **`MovementController`** — 路径消费层。可挂载到任意 GameObject，提供 `GetPosToGo()` 获取当前目标点、`ArrivedPos()` 消费节点。内置重寻路机制（目标移动超过阈值时自动重建路径，新旧路径比较后决定是否替换）和冷却计时器防止频繁重算。Scene View 中通过 Gizmos 可视化路径。
- **`AStarNodeManager`** — Grid data layer. Auto-builds a node map from Tilemaps and Collider2Ds, with walkable/obstacle marking, world↔cell coordinate conversion, and safety margins to prevent wall-hugging.
- **`AStarPathFinder`** — Algorithm layer. Standard A* with 8-directional movement, diagonal pass-through checks (`CanWalkDiagonally`), and start-point optimization (`NoCoverObstacleNodes` for direct line-of-sight shortcuts).
- **`MovementController`** — Consumption layer. Attachable to any GameObject, provides `GetPosToGo()` for the current waypoint and `ArrivedPos()` to consume nodes. Includes automatic path rebuilding (triggers when the target moves beyond a threshold, compares old vs. new path before swapping) and a cooldown timer to prevent excessive recalculations. Path visualized via Gizmos in Scene View.

### 事件驱动 | Event-Driven Architecture

系统间通信通过 ScriptableObject 事件通道解耦。

Inter-system communication is decoupled through ScriptableObject event channels.

- 定义了多种事件 SO（`VoidEventSO`、`DataSaveEventSO`、`QuestOptionsEventSO`、`SceneLoadEventSO` 等），广播方 Raise 事件，接收方订阅回调。
- 存档、任务奖励、场景加载、UI 切换等跨系统操作均通过事件传递，避免直接引用。
- Various event SOs (`VoidEventSO`, `DataSaveEventSO`, `QuestOptionsEventSO`, `SceneLoadEventSO`, etc.) decouple broadcasters from subscribers.
- Cross-system operations—saving, quest rewards, scene loading, UI toggling—all flow through events to avoid direct references.

## 构建建议
## Build Notes

> 本节记录在导出 / 打包过程中容易踩坑的点，尤其是 Addressables 与 ScriptableObject（SO）相关的注意事项。
> This section documents pitfalls encountered during export / build, especially around Addressables and ScriptableObjects (SOs).

### Addressables 相关 | Addressables

- **构建入口与数据构建器**：项目已开启 `Build Addressables on Player Build`（`AddressableAssetSettings` 中 `m_BuildAddressablesWithPlayerBuild = 1`），打包时会自动构建 Addressables。但前提是**活动数据构建器为 Packed Mode**（`m_ActivePlayerDataBuilderIndex = 3`）。如果误切回 Use Asset Database / Simulate Groups 模式，运行时场景会无法被实际打包出来，导致导出包中场景缺失或运行时报错。
  - **Build entry & data builder**: `Build Addressables on Player Build` is enabled (`m_BuildAddressablesWithPlayerBuild = 1`), so Addressables are built automatically during export — **provided the active data builder is Packed Mode** (`m_ActivePlayerDataBuilderIndex = 3`). Switching back to "Use Asset Database" / "Simulate Groups" leaves scenes unbuilt: the exported package will be missing scenes or fail at runtime.

- **失效 / 缺失的引用会直接卡断构建**：Addressables 组里如果引用了已被删除或重命名的资源，构建阶段会报错甚至整体失败。当前 `Scenes` 组里残留了一条指向 `Assets/Scenes/Menu.unity` 的引用，而实际菜单场景是 `Assets/Scenes/GameScene/StartingMenu.unity` —— 这类失效引用（路径错或 GUID 失效）需要在导出前清理。**改资源名 / 路径后，记得同步检查 Addressables 组，或重新打开 `Window > Asset Management > Addressables > Groups` 让它刷新。**
  - **Stale / missing references break the build outright**: An Addressables group that still references a deleted or renamed asset will error out — sometimes aborting the entire build. The `Scenes` group currently retains a reference to `Assets/Scenes/Menu.unity`, while the real menu scene is `Assets/Scenes/GameScene/StartingMenu.unity`. Clean up such stale entries (wrong path or broken GUID) before exporting. **After renaming or moving assets, always re-check the Addressables groups, or reopen `Window > Asset Management > Addressables > Groups` to let it refresh.**

- **内容更新前的 Content Update 依赖 `addressables_content_state.bin`**：做增量内容更新（Content Update Build）前，项目目录下需要存在有效的 `addressables_content_state.bin`（每个平台一份，如 `Windows/`、`Android/`、`WebGL/`）。该文件已在 `.gitignore` 中被忽略，换机器或清理后若丢失，需先做一次 Clean Build 重建它，否则 Content Update 会失败或行为异常。
  - **Content Update depends on `addressables_content_state.bin`**: An incremental Content Update requires a valid `addressables_content_state.bin` per platform (`Windows/`, `Android/`, `WebGL/`). It is git-ignored, so after a machine switch or cleanup it may be missing — run a Clean Build first to regenerate it, or Content Update will fail or behave unexpectedly.

- **`ServerData/` 与本地运行**：Remote 组的产物会输出到 `ServerData/[BuildTarget]`。本仓库默认走本地构建、不启用远程托管（`m_CCDEnabled = 0`），所以不必依赖远端；但若将来开启远程目录，务必保证 `Local.LoadPath` / `Remote.LoadPath` profile 与实际托管地址一致，否则运行时找不到资源。
  - **`ServerData/` and local runtime**: Remote group artifacts go to `ServerData/[BuildTarget]`. This repo defaults to local builds with no remote hosting (`m_CCDEnabled = 0`), so no remote is needed. If you later enable a remote catalog, ensure the `Local.LoadPath` / `Remote.LoadPath` profile values match the real host, or assets won't be found at runtime.

- **场景加载入口**：打包后的运行入口是 `InitialScene`（不在 Addressables 组内，由 Player Settings 直接打进包），它再通过 `GameSceneSO.sceneReference`（`AssetReference`）异步加载 `PersistentScene` 与游戏场景。因此 **入口场景必须保留在 Build Settings 的 Scenes 列表里**，否则空包启动。
  - **Scene load entry**: The post-build entry is `InitialScene` (not in any Addressables group — packed directly by Player Settings). It then asynchronously loads `PersistentScene` and gameplay scenes via `GameSceneSO.sceneReference` (`AssetReference`). So **`InitialScene` must remain in Build Settings' Scenes list**, otherwise the build launches into nothing.

### Android 导出 | Android Export

- **安卓导出存在不明原因失败**：在当前配置下，Android 平台的导出（无论 IL2CPP / Mono、有无导出工程）**会出现原因不明的构建失败**，且报错信息不够明确。目前将其作为已知问题记录，**不在此处提供解决方案**；若确需安卓包，请优先尝试排查 Addressables 失效引用、JDK/SDK/NDK 版本与 Gradle 配置（见根目录 `gradleTemplate.properties`），必要时先在 Windows / PC 平台验证流程通畅。
  - **Android export fails for unknown reasons**: Under the current setup, Android export (regardless of IL2CPP/Mono, with or without exporting a project) **fails for reasons that are not clearly identified**, with unclear error messages. This is recorded as a known issue and **no fix is provided here**. If you need an Android build, first check Addressables stale references, JDK/SDK/NDK versions and the Gradle config (see root `gradleTemplate.properties`); when in doubt, validate the pipeline on the Windows / PC platform first.

### ScriptableObject（SO）使用建议 | ScriptableObject Usage Tips

- **资源名 / 路径改动后检查引用**：项目大量依赖 SO 作为数据容器与事件通道（`DialogSO`、`QuestSO`、`GameSceneSO`、各种 `*EventSO` 等）。重命名或移动 SO 资源后，Inspector 里引用它的字段可能变成 `Missing`，运行时静默失效。建议改名后用搜索（如按 GUID / `Missing`）批量核对一次。
  - **Check references after renaming/moving**: The project leans heavily on SOs as data containers and event channels (`DialogSO`, `QuestSO`, `GameSceneSO`, the various `*EventSO`s). After renaming or moving an SO asset, fields referencing it can turn `Missing` and fail silently at runtime. After a rename, do a sweep (by GUID / `Missing`) to verify references.

- **事件 SO 的订阅与注销必须成对**：自定义事件通道（`VoidEventSO`、`DataSaveEventSO`、`QuestOptionsEventSO` 等）通过 `UnityAction` 委托广播。本项目统一约定在 `OnEnable` 里 `+=` 订阅、`OnDisable` 里 `-=` 注销（参见 `DataManager`、`RetryManager`、`PlayerBow` 等）。**新增订阅者务必遵守此约定**，否则场景切换 / 对象销毁后会出现重复触发或空引用。
  - **Subscribe / unsubscribe event SOs in pairs**: Custom event channels (`VoidEventSO`, `DataSaveEventSO`, `QuestOptionsEventSO`, …) broadcast via `UnityAction` delegates. The repo convention is to subscribe (`+=`) in `OnEnable` and unsubscribe (`-=`) in `OnDisable` (see `DataManager`, `RetryManager`, `PlayerBow`, etc.). **New subscribers must follow this**, or scene transitions / object destruction will cause double-fires or null-refs.

- **`GameSceneSO.ID` 与 `GuidSO` 的稳定标识**：`GameSceneSO` 在 `OnValidate` 里用 `System.Guid` 自动生成 `ID`；`GuidSO` 也会在为空时自动填充 GUID 并 `SetDirty`。这意味着 **SO 的 GUID 一旦生成就不应手动清空或随意改动**，否则存档（`ISaveable`/`DataManager` 体系通过 ID 关联对象）会找不到对应目标。同时注意：`OnValidate` 仅在编辑器下运行，不要依赖它在打包后的运行时生成 ID。
  - **`GameSceneSO.ID` and `GuidSO` stable identity**: `GameSceneSO` auto-generates `ID` via `System.Guid` in `OnValidate`; `GuidSO` likewise fills its GUID when empty and marks itself dirty. So **once a SO's GUID exists, never clear or hand-edit it** — the save system (`ISaveable` / `DataManager`, which keys objects by ID) would lose the link. Also note `OnValidate` is editor-only; do not rely on it to generate IDs in a built player.

- **`GameSceneSO.sceneReference` 必须赋值**：`sceneReference` 是 `AssetReference`，必须指向已纳入 Addressables 的场景资产。若为空，场景加载流程会在运行时抛出 `InvalidKeyException` 之类错误。新建 `GameSceneSO` 后，先把对应场景拖进 `sceneReference`，并确认该场景已出现在 `Scenes` 组里。
  - **Always assign `GameSceneSO.sceneReference`**: `sceneReference` is an `AssetReference` that must point to a scene already included in Addressables. Leaving it empty throws an `InvalidKeyException` (or similar) at runtime when the loader tries to use it. After creating a `GameSceneSO`, drag the scene into `sceneReference` and confirm the scene is present in the `Scenes` group.

- **避免直接在 SO 实例上存“游戏运行时状态”**：SO 是共享资产。本项目把运行时状态放在专门的运行时类里（如 `QuestProgressData` 用 `Dictionary<QuestObjective,int>` 跟踪进度），而不是写回 `QuestSO`。**不要把会变化的运行时数据直接塞进 SO 字段**，否则多份引用共享同一份被篡改的数据，且容易污染编辑器中的资产值。
  - **Don't store live runtime state on the SO instance**: SOs are shared assets. This project keeps runtime state in dedicated runtime classes (e.g. `QuestProgressData` holds per-objective progress in a `Dictionary<QuestObjective,int>`) rather than writing back into `QuestSO`. **Don't dump mutable runtime data into SO fields** — every reference would share the same mutated copy, and it can dirty the asset's stored value in the editor.

## 已知限制
## Known Limitations

- 标题场景中的 `Settings` 入口目前仍未实现。
- The `Settings` entry in the title scene is not implemented yet.

- 部分菜单依赖交互范围或上下文状态，不是任何时刻都能直接打开。
- Some menus depend on interaction range or context state and cannot always be opened freely.

- 当前文档以现有工程和 `游戏指南.txt` 为准，若后续功能调整请同步更新。
- This document reflects the current project and `游戏指南.txt`; please update it when features change.

## 借物表
## Credits

- 美术资源：Tiny Swords by Pixel Frog https://pixelfrog-assets.itch.io/tiny-swords ，基于资产包许可使用，不单独再分发。
- Art assets: Tiny Swords by Pixel Frog https://pixelfrog-assets.itch.io/tiny-swords , used under asset pack license. Not redistributed separately.

## 许可证
## License

- 许可证信息见根目录 `LICENSE`。
- License details are available in the root `LICENSE` file.
