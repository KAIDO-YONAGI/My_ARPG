# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-222222?logo=unity&logoColor=white) ![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)

[简体中文](README.md) | **English**

A 2D top-down ARPG prototype built with Unity and C#, featuring melee and ranged combat, character progression, NPC dialogue, quests, inventory, shops and a skill tree.

Demo: [ARPG Demo Showcase](https://www.bilibili.com/video/BV1sCGH6iErJ/)

## Project Features

| Feature | Content |
|---|---|
| Exploration and combat | Top-down movement, melee and ranged attacks, damage and knockback, retry after death and travel between scenes |
| Enemies and NPCs | Enemy idle, chase and attack states; NPC patrol, wandering and dialogue interaction |
| Character progression | Experience and leveling, a stats panel, skill points, skill levels and prerequisite unlocks |
| Dialogue and quests | Branching dialogue, prerequisites and interaction history; item collection and conversation objectives, progress checks and quest rewards |
| Inventory and shops | Item pickup, stacking, use and dropping; gold, product categories, buying and selling |

## Technical Highlights

### Scenes and System Coordination

Scene groups load additively, with shared systems kept in persistent scenes while level content changes as a group. Load-request and completion events let each system handle its state before and after a transition.

ScriptableObjects configure dialogue, quests, items and skills. Event channels carry interactions such as quest rewards and UI toggles, reducing direct references between systems.

### Character Stats and Presentation

Character stats follow an MVCS structure: the Model handles stat and leveling rules, the Service provides write operations and a read-only interface, and Controllers and Views handle presentation. Dialogue, quests, inventory, shops and skills manage gameplay state separately from views, retaining that state when panels close or are recreated.

### Enemy and NPC Pathfinding

Custom A* pathfinding separates grid construction, path search and path following for reuse by enemies and NPCs. It supports eight-direction movement, diagonal obstacle checks and replanning when targets move.

The open list uses a binary min-heap, and search collections are reused to reduce allocations. Path following supports enemy pursuit, NPC patrol and wandering behaviors.

### UI Interaction and Object Reuse

Panels are created on demand, cached on close and evicted by LRU order or timeout. Dirty flags drive display updates, a focus stack manages window order, and panels support dragging.

Arrows and loot use a generic object pool. EditMode tests cover character-stat rules, the pathfinding open list, object pooling, the UI focus stack and view caching.

## Getting Started

For dependencies, startup, controls and building, see the [setup and controls guide](Docs/GettingStarted.en.md). Android build issues are covered in [Android build troubleshooting](Docs/UnityAndroidBuildGuide.md), in Chinese.

## Documentation

Architecture and system design documents are listed in the [project documentation index](Y_MultipleAgentWorkflow/Router.md), in Chinese.

Documentation routing and concurrent agent work use [Y_MultipleAgentWorkflow](https://github.com/KAIDO-YONAGI/Y_MultipleAgentWorkflow), a workflow I developed and maintain.

## Credits and License

Art assets are from [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) by Pixel Frog, used under the asset pack license. Project code is licensed under [GPL-3.0](LICENSE).
