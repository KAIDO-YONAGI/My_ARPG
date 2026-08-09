# My_ARPG

A 2D ARPG prototype built with Unity 2022.3.62f3c1.

> **中文文档 / Chinese:** [README.md](README.md)

## Overview

- The current version centers on scene transitions, NPC dialogue, quests, shops, inventory, skill trees, and save/load.

- The game uses 2D top-down exploration and combat, with both melee and ranged modes.

## Requirements

- Open the project with Unity 2022.3.62f3c1.

- Main dependencies include Addressables, Cinemachine, Input System, TextMesh Pro, and the Unity 2D feature set.

## Quick Start

- After opening the project in Unity Hub, wait for package dependencies and Addressables data to finish importing.

- The default build entry is `Assets/Scenes/InitialScene.unity`, which asynchronously loads `PersistentScene` on startup.

- The title menu scene is located at `Assets/Scenes/GameScene/StartingMenu.unity`, and the main gameplay scenes are `Assets/Scenes/GameScene/Scene1.unity` and `Assets/Scenes/GameScene/Scene2.unity`.

- `Assets/Scenes/TestScene.unity` can be used for isolated testing, but `InitialScene` is the better entry point for validating the full flow.

## Main Features

- Cross-scene loading and transitions with fade effects and persistent-scene management.

- NPC dialogue supports branching, condition checks, and history tracking.

- The quest system includes quest boards, a quest log, objectives, rewards, and state refresh.

- The inventory and item system supports pickup, use, selling, dropping, and partial persistence.

- Shops, skill trees, stat panels, and the integrated menu are wired into one UI management flow.

- The save system supports saving and loading in gameplay scenes, and deleting saves in the menu scene.

- A* pathfinding is used by parts of the enemy and NPC movement logic.

## Controls

- `WASD`: Move the character.

- `Q`: Switch between ranged and melee modes.

- `J`: Fire an arrow in archer mode.

- `K`: Perform a sword slash in melee mode.

- `1`: Open the stats panel.

- `2`: Open the skill panel, and left-click a skill slot to spend points and unlock skills.

- `F`: Interact with a shop.

- `T`: Open or close NPC dialogue, and left-click to advance dialogue or choose options.

- `C`: Open the quest menu.

- `ESC`: Open the exit menu to return to title, save, or quit the game.

- Most panels can be closed with `X` and dragged by their top bar.

- The lower-left integrated menu can open most interfaces, but some functions require approaching an NPC, shop, or quest board first.

## Inventory and Save Notes

- Left-clicking inventory items uses them when the shop is closed and sells them when the shop is open.

- Right-clicking inventory items drops them.

- In gameplay scenes you can `Save` / `Load`, and in the menu scene `Save` changes to `Delete`.

- Dropped items are not preserved after scene transitions, reloads, or `Retry`.

- Walking to the end of a wooden bridge can trigger a scene change, and the `GameOver` menu appears automatically when the character dies.

## Project Structure

- `Assets/Scripts/UI`: UI management, dialogue, quest boards, shops, skill trees, and menu interactions.

- `Assets/Scripts/Units`: Behavior scripts for enemies, NPCs, and shopkeepers.

- `Assets/Scripts/Player`: Player movement, combat, equipment switching, time control, and stat management.

- `Assets/Scripts/Inventory`: Inventory, slots, loot, and item-use logic.

- `Assets/Scripts/SaveAndLoad`: Data structures, save interfaces, and the save/load flow.

- `Assets/Scripts/Scene` and `Assets/Scripts/A Star`: Scene transition and pathfinding logic.

- `Assets/Scripts/ScriptableObjects`: ScriptableObject definitions for quests, scenes, dialogue, and events.

## Core System Architecture

### Save System

The save system uses an `ISaveable` interface + registry pattern to manage all persistable objects.

- **`ISaveable`** defines `SaveData(Data)` / `LoadData(Data)`. Implementations (Loot, InventoryManager, etc.) self-register with `DataManager` on enable.
- **`DataManager`** holds a `List<ISaveable>` registry and invokes save/load on all entries during scene transitions.
- **`SaveSystem`** handles serialization (Newtonsoft.Json) and file I/O, with separate manual and automatic system saves.
- Auto-save triggers on scene transitions; manual save triggers from player actions.
- Safety: delete validates paths stay within `persistentDataPath`; loading skips corrupt files and falls back to the latest valid save.

### Dialogue System

The dialogue system builds tree-structured dialogue graphs from `DialogSO` ScriptableObjects, with conditional branching and history tracking.

- Each `DialogSO` node holds dialogue lines (`dialogLines`) and child options (`nextDialogOptions`), forming a dialogue tree.
- Conditional branching via `RefuseDialogSO`: before starting a dialogue, prerequisites are checked (character spoken to, items collected). Failure shows a refusal dialogue instead.
- `onlyTriggeredOnce` enables one-shot dialogues. `ConversationHistoryManager` tracks dialogue history.
- `ItemHistoryManager` tracks item pickup history for condition checks and quest objectives.

### Quest System

State-machine-based quest management with multi-objective types and automatic state progression.

- Quest state machine: `Idle → Accepted → IsToComplete → Completed`, with a `Decline` branch that reverts to `Accepted`.
- `QuestProgressData` inner class uses `Dictionary<QuestObjective, int>` to track per-objective progress.
- Objective types include item pickup counts (via `ItemHistoryManager`) and character conversation checks (via `ConversationHistoryManager`).
- Completing all objectives auto-promotes to `IsToComplete`; finishing a quest auto-rewards items through the event system.

### A* Pathfinding System

Three-layer decoupled architecture: grid management, pathfinding algorithm, and path consumption are independent. NPCs and enemies only need a `MovementController` component to use pathfinding.

- **`AStarNodeManager`** — Grid data layer. Auto-builds a node map from Tilemaps and Collider2Ds, with walkable/obstacle marking, world↔cell coordinate conversion, and safety margins to prevent wall-hugging.
- **`AStarPathFinder`** — Algorithm layer. Standard A* with 8-directional movement, diagonal pass-through checks (`CanWalkDiagonally`), and start-point optimization (`NoCoverObstacleNodes` for direct line-of-sight shortcuts).
- **`MovementController`** — Consumption layer. Attachable to any GameObject, provides `GetPosToGo()` for the current waypoint and `ArrivedPos()` to consume nodes. Includes automatic path rebuilding (triggers when the target moves beyond a threshold, compares old vs. new path before swapping) and a cooldown timer to prevent excessive recalculations. Path visualized via Gizmos in Scene View.

### Event-Driven Architecture

Inter-system communication is decoupled through ScriptableObject event channels.

- Various event SOs (`VoidEventSO`, `DataSaveEventSO`, `QuestOptionsEventSO`, `SceneLoadEventSO`, etc.) decouple broadcasters from subscribers.
- Cross-system operations—saving, quest rewards, scene loading, UI toggling—all flow through events to avoid direct references.

## Build Notes

> This section documents pitfalls encountered during export / build, especially around Addressables and ScriptableObjects (SOs).

### Addressables

- **Build entry & data builder**: `Build Addressables on Player Build` is enabled (`m_BuildAddressablesWithPlayerBuild = 1`), so Addressables are built automatically during export — **provided the active data builder is Packed Mode** (`m_ActivePlayerDataBuilderIndex = 3`). Switching back to "Use Asset Database" / "Simulate Groups" leaves scenes unbuilt: the exported package will be missing scenes or fail at runtime.

- **Stale / missing references break the build outright**: An Addressables group that still references a deleted or renamed asset will error out — sometimes aborting the entire build. The `Scenes` group currently retains a reference to `Assets/Scenes/Menu.unity`, while the real menu scene is `Assets/Scenes/GameScene/StartingMenu.unity`. Clean up such stale entries (wrong path or broken GUID) before exporting. **After renaming or moving assets, always re-check the Addressables groups, or reopen `Window > Asset Management > Addressables > Groups` to let it refresh.**

- **Content Update depends on `addressables_content_state.bin`**: An incremental Content Update requires a valid `addressables_content_state.bin` per platform (`Windows/`, `Android/`, `WebGL/`). It is git-ignored, so after a machine switch or cleanup it may be missing — run a Clean Build first to regenerate it, or Content Update will fail or behave unexpectedly.

- **`ServerData/` and local runtime**: Remote group artifacts go to `ServerData/[BuildTarget]`. This repo defaults to local builds with no remote hosting (`m_CCDEnabled = 0`), so no remote is needed. If you later enable a remote catalog, ensure the `Local.LoadPath` / `Remote.LoadPath` profile values match the real host, or assets won't be found at runtime.

- **Scene load entry**: The post-build entry is `InitialScene` (not in any Addressables group — packed directly by Player Settings). It then asynchronously loads `PersistentScene` and gameplay scenes via `GameSceneSO.sceneReference` (`AssetReference`). So **`InitialScene` must remain in Build Settings' Scenes list**, otherwise the build launches into nothing.

### Android Export

- **Android Release build verified on 2026-08-09**: SDK detection stalled because `sdkmanager` was not using the local proxy, while Release lint failed because a non-ASCII filename in `StreamingAssets` produced an AAR entry that could not be decoded. See [`Docs/UnityAndroidGlobalBuildFix.md`](Docs/UnityAndroidGlobalBuildFix.md) for global proxy setup, removal, and diagnostics. The current test APK is debug-signed; configure a project keystore before publishing.

### ScriptableObject Usage Tips

- **Check references after renaming/moving**: The project leans heavily on SOs as data containers and event channels (`DialogSO`, `QuestSO`, `GameSceneSO`, the various `*EventSO`s). After renaming or moving an SO asset, fields referencing it can turn `Missing` and fail silently at runtime. After a rename, do a sweep (by GUID / `Missing`) to verify references.

- **Subscribe / unsubscribe event SOs in pairs**: Custom event channels (`VoidEventSO`, `DataSaveEventSO`, `QuestOptionsEventSO`, …) broadcast via `UnityAction` delegates. The repo convention is to subscribe (`+=`) in `OnEnable` and unsubscribe (`-=`) in `OnDisable` (see `DataManager`, `RetryManager`, `PlayerBow`, etc.). **New subscribers must follow this**, or scene transitions / object destruction will cause double-fires or null-refs.

- **`GameSceneSO.ID` and `GuidSO` stable identity**: `GameSceneSO` auto-generates `ID` via `System.Guid` in `OnValidate`; `GuidSO` likewise fills its GUID when empty and marks itself dirty. So **once a SO's GUID exists, never clear or hand-edit it** — the save system (`ISaveable` / `DataManager`, which keys objects by ID) would lose the link. Also note `OnValidate` is editor-only; do not rely on it to generate IDs in a built player.

- **Always assign `GameSceneSO.sceneReference`**: `sceneReference` is an `AssetReference` that must point to a scene already included in Addressables. Leaving it empty throws an `InvalidKeyException` (or similar) at runtime when the loader tries to use it. After creating a `GameSceneSO`, drag the scene into `sceneReference` and confirm the scene is present in the `Scenes` group.

- **Don't store live runtime state on the SO instance**: SOs are shared assets. This project keeps runtime state in dedicated runtime classes (e.g. `QuestProgressData` holds per-objective progress in a `Dictionary<QuestObjective,int>`) rather than writing back into `QuestSO`. **Don't dump mutable runtime data into SO fields** — every reference would share the same mutated copy, and it can dirty the asset's stored value in the editor.

## Known Limitations

- The `Settings` entry in the title scene is not implemented yet.

- Some menus depend on interaction range or context state and cannot always be opened freely.

- This document reflects the current project and `GameGuide.txt`; please update it when features change.

## Credits

- Art assets: Tiny Swords by Pixel Frog https://pixelfrog-assets.itch.io/tiny-swords , used under asset pack license. Not redistributed separately.

## License

- License details are available in the root `LICENSE` file.
