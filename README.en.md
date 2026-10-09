# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-000000?logo=unity)
![License](https://img.shields.io/badge/License-GPL--3.0-blue)
![Language](https://img.shields.io/badge/C%23-74%25-512BD4?logo=csharp)

A 2D top-down ARPG prototype built with Unity 2022.3.62f3c1 — a complete, event-driven implementation of core RPG systems: dialogue, quests, shops, inventory, a skill tree, and save/load.

> **中文文档 / Chinese:** [README.md](README.md)

## Highlights

- **Complete RPG system loop**: scene transitions, NPC dialogue (branching + conditions + history), a state-machine quest system, shops, inventory, a skill tree, and save/load.
- **Event-driven architecture**: cross-system decoupling via ScriptableObject event channels.
- **Three-layer decoupled A\* pathfinding**: grid management / pathfinding algorithm / PathFollower — attach a component and go.
- **2D combat**: top-down exploration with both melee and ranged modes.
- **Engineering practices**: multi-scene additive loading, Newtonsoft.Json saves, and Android build troubleshooting docs.

## Demo Video

- [【Unity】28th ARPG Demo Showcase](https://www.bilibili.com/video/BV1sCGH6iErJ/) (Bilibili, published 2026-05-25)

> The video showcases an **earlier version** of the project. It has evolved significantly since then — controls, system implementations, and scene content have all changed — so the video may be inaccurate or outdated and is for reference only. Treat this README and the repository code as the source of truth.

## Table of Contents

- [Requirements & Quick Start](#requirements--quick-start)
- [Controls](#controls)
- [Project Structure](#project-structure)
- [Core System Architecture](#core-system-architecture)
- [Layered Architecture](#layered-architecture)
- [Multi-Agent Workflow](#multi-agent-workflow)
- [Build Guide](#build-guide)
- [ScriptableObject Usage Tips](#scriptableobject-usage-tips)
- [Known Limitations](#known-limitations)
- [Credits](#credits)
- [License](#license)

## Requirements & Quick Start

**Requirements**

- Open the project with Unity 2022.3.62f3c1.
- Main dependencies include Cinemachine, Input System, TextMesh Pro, and the Unity 2D feature set.

**Getting started**

1. Open the project in Unity Hub and wait for package dependencies and assets to finish importing.
2. Start from `Assets/Scenes/InitialScene.unity`; as the build entry it additively loads the persistent scene group.
3. The title menu scene is at `Assets/Scenes/GameScene/StartingMenu.unity`; the main gameplay scenes are `Assets/Scenes/GameScene/Scene1.unity` and `Scene2.unity`.
4. `Assets/Scenes/TestScene.unity` can be used for isolated testing, but `InitialScene` is the better entry point for validating the full flow.

## Controls

**Keys**

| Key | Action |
|---|---|
| `WASD` | Move the character |
| `Q` | Switch between ranged and melee modes |
| `J` | Fire an arrow in ranged mode; perform a sword slash in melee mode (`Slash` and `Shoot` share one key; press `Q` to pick the mode) |
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

Scripts are split into three top-level folders: cross-feature contracts, gameplay organized by feature domain, and pipeline infrastructure that is independent of gameplay.

- `Assets/Scripts/Contracts`: cross-layer contracts and infrastructure — `ISaveable`, `IDamageable`, `ICanvasManager`, the static save registry `SaveRegistry`, and the singleton base `YSingleton`.
- `Assets/Scripts/Gameplay`: one folder per feature domain, each further split into `Models/`, `Services/`, `Controllers/`, and `Views/`.
  - `Player/`: player stats, movement, combat, equipment.
  - `Quest/`, `Dialog/`, `Inventory/`, `Shop/`, `Skills/`: quests, dialogue, inventory, shops, and the skill tree.
  - `Units/`: enemy, NPC, and shopkeeper behavior.
  - `Save/`: `SaveDataManager` and the save/load flow.
  - `Grid/`: grid data.
- `Assets/Scripts/Pipeline`: infrastructure independent of gameplay, containing A\* pathfinding, scene transition and loading, config assets and event channels, and UI infrastructure.

Layering conventions are described in [Layered Architecture](#layered-architecture).

## Core System Architecture

### Save System

The save system uses an `ISaveable` interface + registry pattern to manage all persistable objects.

- **`ISaveable`** defines `SaveData(Data)` / `LoadData(Data)`. Implementations (Loot, InventoryManager, etc.) register with `SaveRegistry` on enable.
- **`SaveRegistry`** in `Contracts/SaveRegistry.cs` is a static registry that exists before any scene instance, so registering and unregistering are safe at any lifecycle stage and do not depend on `Awake` order. `SaveDataManager` only collects and dispatches, iterating `SaveRegistry.All` when saving or loading.
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

Three-layer decoupled architecture: grid management, pathfinding algorithm, and path consumption are independent. NPCs and enemies only need a `PathFollower` component to use pathfinding.

- **`AStarNodeManager`** — Grid data layer. Auto-builds a node map from Tilemaps and Collider2Ds, with walkable/obstacle marking, world↔cell coordinate conversion, and safety margins to prevent wall-hugging.
- **`AStarPathFinder`** — Algorithm layer. Standard A* with 8-directional movement, diagonal pass-through checks (`CanWalkDiagonally`), and start-point optimization (`NoCoverObstacleNodes` for direct line-of-sight shortcuts).
- **`PathFollower`** — Consumption layer. Attachable to any GameObject; `GetPosToGo(optPos, startPos, endPos)` returns the next waypoint and `ArrivedPos()` consumes a reached node. Includes automatic path rebuilding (triggers when the target moves beyond a threshold, compares old vs. new path before swapping) and a cooldown timer to prevent excessive recalculations. Path visualized via Gizmos in Scene View.

### Event-Driven Architecture

Inter-system communication is decoupled through ScriptableObject event channels.

- Various event SOs (`VoidEventSO`, `DataSaveEventSO`, `QuestOptionsEventSO`, `SceneLoadEventSO`, etc.) decouple broadcasters from subscribers.
- Cross-system operations—saving, quest rewards, scene loading, UI toggling—all flow through events to avoid direct references.

## Layered Architecture

The project uses a lightweight MVCS split: a Model holds one data aggregate's state and rules, a Service is the domain's write entry point, a Controller translates, and a View only writes widgets. The player stats line follows this split; the other feature domains keep their original Manager form.

- **Write path**: input sources such as button clicks, SO event channels, and collisions enter an input-side Controller, are translated into intents, and pass through the Service write entry point into the Model, where the aggregate's rules run.
- **Read path**: the Model raises C# events when state changes, a display-side Controller translates the data into display parameters, and the View writes widgets through `SetXxx`.
- **Three boundaries**: rules that read only the aggregate's own fields belong in the Model; cross-aggregate rules, lifecycle, and saving belong in the Service; persistent state must be written into the Model through the Service, because state that bypasses the Model reaches neither the read path nor the save file.

This project's migration status, confirmed defects, unimplemented designs, and per-system conventions live in [`Y_MultipleAgentWorkflow/`](Y_MultipleAgentWorkflow/Router.md): the root Router is the entry point, layering boundaries are in `Architecture/Layering/Layering_Guide.md`, composition and initialization order in `Architecture/Composition/Composition_Guide.md`, and every gameplay, scene, asset, save, and build domain has its own Router and Guide. The historical baseline docs (the refactor checklists formerly indexed under `Docs/`) are no longer the current authority.

> **Project status: the layered refactor is frozen.** The player stats line is organized in four layers; quests, dialogue, inventory and shops, save and scene orchestration, and movement, combat, and pathfinding keep their original Manager form. The skill domain is the retained verification point: skill points belong to the stats aggregate while spending happens in the skill aggregate, so it is the only place in this project where the rule that a single write atomically changes two data aggregates can be verified.

## Multi-Agent Workflow

The multi-agent collaboration workflow used in this project is the **author's own** workflow, kept in a standalone repository: [KAIDO-YONAGI/Y_MultipleAgentWorkflow](https://github.com/KAIDO-YONAGI/Y_MultipleAgentWorkflow) — reusable multi-agent workflow configuration, concurrency leases, and multi-client skill distribution. The [`Y_MultipleAgentWorkflow/`](Y_MultipleAgentWorkflow/Router.md) directory here is this project's instance of it and is versioned with the project; the configuration method is documented in [`Workflow_Configuration_Guide.md`](Y_MultipleAgentWorkflow/Workflow_Configuration_Guide.md).

## Build Guide

> Only the key takeaways are kept here; full Android troubleshooting lives in the Docs folder.

### Scene Loading

Scene loading runs through `SceneManager` in additive mode, with `SceneChanger.RequestSceneLoad` as the single entry point.

- **Entry scene**: the build entry is `InitialScene`, packed directly by Build Settings. Its `InitialLoad` registers `persistentScenes` with `PersistentSceneRegistry` and loads them additively one by one; these scenes survive later transitions.
- **Scene groups**: `GameSceneSO` identifies a scene by `sceneName`, kept in sync with `sceneAsset` in the editor. `SceneChanger` loads each entry in list order with `LoadSceneAsync`, unloads the previous group in reverse order while skipping persistent scenes, and accepts one request per loading window.
- **Broadcast, then execute**: `RequestSceneLoad` raises `SceneLoadEventSO` first so subscribers can finish up before the transition starts, then runs the loading flow.
- **Scenes must be in Build Settings**: both `SceneChanger` and `InitialLoad` pre-check with `Application.CanStreamedLevelBeLoaded` and log an error for missing scenes. Currently enabled: `InitialScene`, `GameScene/PersistentScene`, `GameScene/StartingMenu`, `GameScene/Scene1`, `GameScene/Scene2`, `TestScene`.

### Android Export

- **Android Release build (verified 2026-08-09)**: SDK detection stalled because `sdkmanager` did not inherit the proxy; Release lint failed because a non-ASCII filename in `StreamingAssets` produced an undecodable AAR entry. See [`Docs/UnityAndroidBuildGuide.md`](Docs/UnityAndroidBuildGuide.md) for proxy setup, removal, environment self-checks, and diagnostics (**machine-independent**: first read the real paths and proxy port on your machine via its "Step 1: confirm the environment"). Build evidence is archived in [`Docs/UnityAndroidBuildVerification.md`](Docs/UnityAndroidBuildVerification.md).
- The current test APK is debug-signed; configure a project keystore before publishing.

## ScriptableObject Usage Tips

- **Check references after renaming/moving**: The project leans heavily on SOs as data containers and event channels (`DialogSO`, `QuestSO`, `GameSceneSO`, the various `*EventSO`s). After renaming or moving an SO, fields referencing it can turn `Missing` and fail silently at runtime — do a sweep (by GUID / `Missing`) to verify.
- **Subscribe / unsubscribe event SOs in pairs**: The repo convention is to subscribe (`+=`) in `OnEnable` and unsubscribe (`-=`) in `OnDisable` (see `SaveDataManager`, `SceneChanger`, `PlayerBow`, etc.). New subscribers must follow this, or scene transitions / object destruction will cause double-fires or null-refs.
- **Never hand-edit save identifiers once generated**: the `ISaveable` system keys objects by `SaveDefinition` ID, and changing one breaks the link. A scene's save key is `GameSceneSO.SaveKey`, which takes the scene file's GUID in the editor. `OnValidate` is editor-only — don't rely on it to generate IDs in a built player.
- **Keep `GameSceneSO.sceneName` valid**: it drives which scene loads at runtime, kept in sync with `sceneAsset` in the editor. When empty, `SceneChanger` and `InitialLoad` log an error and skip that scene.
- **Don't store live runtime state on SO instances**: SOs are shared assets — keep runtime state in dedicated runtime classes (e.g. `QuestProgressData`), or every reference shares the same mutated copy and the asset's stored values get dirtied in the editor.

## Known Limitations

- The `Settings` entry in the title scene is not implemented yet.
- Some menus depend on interaction range or context state and cannot always be opened freely.
- This document reflects the current project and `GameGuide.txt`; please update it when features change.

## Credits

- Art assets: Tiny Swords by Pixel Frog https://pixelfrog-assets.itch.io/tiny-swords , used under asset pack license. Not redistributed separately.

## License

- License details are available in the root `LICENSE` file.
