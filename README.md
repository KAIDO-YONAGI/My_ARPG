# My_ARPG
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

## 已知限制
## Known Limitations

- 标题场景中的 `Settings` 入口目前仍未实现。
- The `Settings` entry in the title scene is not implemented yet.

- 部分菜单依赖交互范围或上下文状态，不是任何时刻都能直接打开。
- Some menus depend on interaction range or context state and cannot always be opened freely.

- 当前文档以现有工程和 `游戏指南.txt` 为准，若后续功能调整请同步更新。
- This document reflects the current project and `游戏指南.txt`; please update it when features change.

## 许可证
## License

- 许可证信息见根目录 `LICENSE`。
- License details are available in the root `LICENSE` file.
