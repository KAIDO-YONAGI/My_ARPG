# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-222222?logo=unity&logoColor=white) ![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)

[简体中文](README.md) | **English**

I built this 2D top-down ARPG prototype with Unity and C#. I connected exploration and combat with NPC dialogue, quests, inventory, shops and a skill tree to support character progression.

I connected combat, progression and interaction systems through scene loading, pathfinding, data configuration, saving and UI lifecycle management.

Demo: [ARPG Demo Showcase](https://www.bilibili.com/video/BV1sCGH6iErJ/)

## Project Features

| Feature | Content |
|---|---|
| Exploration and combat | I implemented top-down movement, melee and ranged attacks, damage and knockback, retry after death and travel between scenes |
| Enemies and NPCs | I built idle, chase and attack states for enemies, plus patrol, wandering and dialogue interaction for NPCs |
| Character progression | I implemented experience and leveling, a stats panel, skill points, skill levels and prerequisite unlocks |
| Dialogue and quests | I designed branching dialogue, prerequisites and interaction history, then connected collection and conversation objectives to quest progress and rewards |
| Inventory and shops | I implemented item pickup, stacking, use and dropping, as well as gold, product categories, buying and selling |
| Saving and loading | I support manual saves and automatic saves on scene transitions, restoring character stats, scene position and dropped-item state |

### Character Progression and System Coordination

I use MVCS to centralize character-stat rules and write operations while providing a read-only interface. Dialogue, quests, inventory, shops and skills keep gameplay state separate from views, so progression and interaction logic operates independently of panel creation and closure.

I configure dialogue, quests, items and skills with ScriptableObjects, and connect rewards and UI toggles through event channels. I organize levels into additive scene groups around persistent shared systems, using load-request and completion events to coordinate transitions.

### Enemy and NPC Pathfinding

I implemented custom A* pathfinding with separate grid construction, path search and path following for enemies and NPCs. It supports eight-direction movement, diagonal obstacle checks and dynamic replanning as characters move.

I manage the open list with a binary min-heap and reuse search collections to reduce allocations during repeated searches. Path following works with enemy states and NPC patrol and wandering behaviors to support pursuit and autonomous movement.

### UI Interaction and Object Reuse

I create panels on demand, cache them on close and evict them by LRU order or timeout. Systems maintain gameplay state independently, while dirty flags drive display updates. I use a focus stack to manage window order and support panel dragging.

I reuse arrows and loot through a generic object pool. EditMode tests cover character-stat rules, the pathfinding open list, object pooling, the UI focus stack and view caching.

## Getting Started

For dependencies, startup, controls and building, see the [setup and controls guide](Docs/GettingStarted.en.md). Mobile builds are covered in [Android build troubleshooting](Docs/UnityAndroidBuildGuide.md), in Chinese.

## Documentation

| Document | Content |
|---|---|
| [System documentation index](Y_MultipleAgentWorkflow/Router.md) | Architecture, gameplay, scenes, data and building |
| [Layers and state access](Y_MultipleAgentWorkflow/Architecture/Layering/Layering_Guide.md) | Player stat layers, rules and state access boundaries |
| [Initialization and UI management](Y_MultipleAgentWorkflow/Architecture/Composition/Composition_Guide.md) | System initialization, window focus and view caching |
| [Event channels](Y_MultipleAgentWorkflow/Assets/EventChannels_Guide.md) | ScriptableObject configuration and events between systems |
| [Save system](Y_MultipleAgentWorkflow/Data/SaveData_Guide.md) | Save data, persistence and restoration |

The reference documents above are in Chinese.

I developed and maintain the multi-agent workflow: [Y_MultipleAgentWorkflow](https://github.com/KAIDO-YONAGI/Y_MultipleAgentWorkflow).

## Credits and License

I use [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) by Pixel Frog under the asset pack license. I license the project code under [GPL-3.0](LICENSE).
