# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-000000?logo=unity)
![License](https://img.shields.io/badge/License-GPL--3.0-blue)
![Language](https://img.shields.io/badge/C%23-74%25-512BD4?logo=csharp)

A 2D top-down ARPG prototype built with Unity 2022.3.62f3c1 — a complete, event-driven implementation of core RPG systems: dialogue, quests, shops, inventory, a skill tree, and save/load.

> **中文文档 / Chinese:** [README.md](README.md)

## Highlights

- **Complete RPG system loop**: scene transitions, NPC dialogue (branching + conditions + history), a state-machine quest system, shops, inventory, a skill tree, and save/load.
- **Event-driven architecture**: cross-system decoupling via ScriptableObject event channels.
- **Three-layer decoupled A\* pathfinding**: grid management / pathfinding algorithm / MovementController — attach a component and go.
- **2D combat**: top-down exploration with both melee and ranged modes.
- **Engineering practices**: Addressables scene loading, Newtonsoft.Json saves, and Android build troubleshooting docs.

## Demo Video

- [【Unity】28th ARPG Demo Showcase](https://www.bilibili.com/video/BV1sCGH6iErJ/) (Bilibili, published 2026-05-25)

> The video showcases an **earlier version** of the project. It has evolved significantly since then — controls, system implementations, and scene content have all changed — so the video may be inaccurate or outdated and is for reference only. Treat this README and the repository code as the source of truth.

## Table of Contents

- [Requirements & Quick Start](#requirements--quick-start)
- [Controls](#controls)
- [Project Structure](#project-structure)
- [Core System Architecture](#core-system-architecture)
- [Build Guide](#build-guide)
- [ScriptableObject Usage Tips](#scriptableobject-usage-tips)
- [Known Limitations](#known-limitations)
- [Credits](#credits)
- [License](#license)

## Requirements & Quick Start

**Requirements**

- Open the project with Unity 2022.3.62f3c1.
- Main dependencies include Addressables, Cinemachine, Input System, TextMesh Pro, and the Unity 2D feature set.

**Getting started**

1. Open the project in Unity Hub and wait for package dependencies and Addressables data to finish importing.
2. Start from `Assets/Scenes/InitialScene.unity` (the default build entry; it asynchronously loads `PersistentScene` on startup).
3. The title menu scene is at `Assets/Scenes/GameScene/StartingMenu.unity`; the main gameplay scenes are `Assets/Scenes/GameScene/Scene1.unity` and `Scene2.unity`.
4. `Assets/Scenes/TestScene.unity` can be used for isolated testing, but `InitialScene` is the better entry point for validating the full flow.

## Controls

**Keys**

| Key | Action |
|---|---|
| `WASD` | Move the character |
| `Q` | Switch between ranged and melee modes |
| `J` | Fire an arrow in archer mode |
| `K` | Perform a sword slash in melee mode |
| `1` | Open the stats panel |
| `2` | Open the skill panel (left-click a skill slot to spend points and unlock skills) |
| `F` | Interact with a shop |
| `T` | Open or close NPC dialogue (left-click to advance dialogue or choose options) |
| `C` | Open the quest menu |
| `ESC` | Open the exit menu (return to title, save, or quit) |

**UI basics**: Most panels can be closed with `X` and dragged by their top bar. The lower-left integrated menu can open most interfaces, but some functions require approaching an NPC, shop, or quest board first.

**Inventory & saving**

- Left-clicking inventory items uses them when the shop is closed and sells them when the shop is open; right-click drops them.
- In gameplay scenes you can `Save` / `Load`; in the menu scene `Save` changes to `Delete`.
- Dropped items are not preserved after scene transitions, reloads, or `Retry`.
- Walking to the end of a wooden bridge triggers a scene change, and the `GameOver` menu appears automatically when the character dies.

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
- **`SaveSystem`** handles serialization (Newtonsoft.Json) and file I/O, with separate manual and automatic saves: auto-save triggers on scene transitions, manual saves from player actions.
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

## Build Guide

> Only the key takeaways are kept here; full Android troubleshooting lives in the Docs folder.

### Addressables Essentials

- **The data builder must be Packed Mode**: `Build Addressables on Player Build` is enabled, but only works when the active data builder is Packed Mode (`m_ActivePlayerDataBuilderIndex = 3`). Switching back to "Use Asset Database" / "Simulate Groups" leaves scenes unbuilt — the exported package will be missing scenes or fail at runtime.
- **Stale references break the build outright**: a group referencing a deleted/renamed asset will error out — sometimes aborting the whole build. After renaming/moving assets, re-check the Addressables groups, or reopen `Window > Asset Management > Addressables > Groups` to refresh.
- **Content Update depends on `addressables_content_state.bin`**: stored per platform (`Windows/`, `Android/`, …) and git-ignored; if lost after a machine switch or cleanup, run a Clean Build first to regenerate it.
- **The entry scene must stay in Build Settings**: the post-build entry is `InitialScene` (not in any Addressables group — packed directly by Player Settings); it loads everything else via `GameSceneSO.sceneReference`. Without it the build launches into nothing.
- Remote group artifacts go to `ServerData/[BuildTarget]`; this repo defaults to local builds (`m_CCDEnabled = 0`) and needs no remote. If you later enable a remote catalog, ensure the LoadPath matches the real host.

### Android Export

- **Android Release build (verified 2026-08-09)**: SDK detection stalled because `sdkmanager` did not inherit the proxy; Release lint failed because a non-ASCII filename in `StreamingAssets` produced an undecodable AAR entry. See [`Docs/UnityAndroidBuildGuide.md`](Docs/UnityAndroidBuildGuide.md) for proxy setup, removal, environment self-checks, and diagnostics (**machine-independent**: first read the real paths and proxy port on your machine via its "Step 1: confirm the environment"). Build evidence is archived in [`Docs/UnityAndroidBuildVerification.md`](Docs/UnityAndroidBuildVerification.md).
- The current test APK is debug-signed; configure a project keystore before publishing.

## ScriptableObject Usage Tips

- **Check references after renaming/moving**: The project leans heavily on SOs as data containers and event channels (`DialogSO`, `QuestSO`, `GameSceneSO`, the various `*EventSO`s). After renaming or moving an SO, fields referencing it can turn `Missing` and fail silently at runtime — do a sweep (by GUID / `Missing`) to verify.
- **Subscribe / unsubscribe event SOs in pairs**: The repo convention is to subscribe (`+=`) in `OnEnable` and unsubscribe (`-=`) in `OnDisable` (see `DataManager`, `SceneChanger`, `PlayerBow`, etc.). New subscribers must follow this, or scene transitions / object destruction will cause double-fires or null-refs.
- **Never hand-edit `GameSceneSO.ID` / `GuidSO` GUIDs once generated**: the save system (`ISaveable`/`DataManager`) keys objects by ID; clearing or changing a GUID breaks the link. Note `OnValidate` is editor-only — don't rely on it to generate IDs in a built player.
- **Always assign `GameSceneSO.sceneReference`**: it's an `AssetReference` that must point to a scene already included in Addressables; leaving it empty throws an `InvalidKeyException` (or similar) at runtime.
- **Don't store live runtime state on SO instances**: SOs are shared assets — keep runtime state in dedicated runtime classes (e.g. `QuestProgressData`), or every reference shares the same mutated copy and the asset's stored values get dirtied in the editor.

## Known Limitations

- The `Settings` entry in the title scene is not implemented yet.
- Some menus depend on interaction range or context state and cannot always be opened freely.
- This document reflects the current project and `GameGuide.txt`; please update it when features change.

## Credits

- Art assets: Tiny Swords by Pixel Frog https://pixelfrog-assets.itch.io/tiny-swords , used under asset pack license. Not redistributed separately.

## License

- License details are available in the root `LICENSE` file.
