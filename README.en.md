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

Scene groups load additively, with level resources split by group and shared systems kept in persistent scenes, which keeps the work easy to divide. Load requests and completion events are broadcast during asynchronous loading, so each module handles its own state in the right order.

Dialogue, quests, items, skills and audio are configured through ScriptableObjects, while rewards, UI toggles and sound requests travel over event channels to reduce direct references between systems. The audio manager lives in the persistent scene, with pooled sound players and crossfaded background music.

### Character Stats and Presentation

Character stats are organized in Model, View, Controller and Service layers: experience curves, stat clamping and leveling rules live in the Model, the Service is the single write entry point with a read-only interface, and Controllers and Views handle presentation. Dialogue, quests, inventory, shops and skills keep their gameplay state in their own systems, views draw from read-only queries, and that state survives panels closing or being recreated.

### Enemy and NPC Pathfinding

Custom A* pathfinding separates grid construction, path search and path following for reuse by enemies and NPCs. It supports eight-direction movement and diagonal obstacle checks, samples obstacles in a straight line before adopting an optimized start point, replans by distance and cooldown when targets move, and advances through the path with a waypoint stack.

The open list uses a binary min-heap, and search collections are reused across searches to reduce allocations. Path following supports enemy pursuit, NPC patrol and wandering behaviors.

### UI Interaction and Object Reuse

The UI scheduling framework maintains panel state with linked lists plus events and callbacks, handling player input and code-driven operations through one path. Panels are created at runtime and cached on close, with LRU eviction by close order and timeouts measured from the close moment. Views refresh by versioned dirty flags in a pull design: writes advance the version, and each view compares the version it rendered before redrawing. A focus stack manages window order, and panels support dragging.

Arrows and loot use a generic object pool. EditMode tests cover stat rules, the pathfinding open list, object pooling, the UI focus stack, view caching and versioned values.

## Getting Started

For dependencies, startup, controls and building, see the [setup and controls guide](Docs/GettingStarted.en.md). Android build issues are covered in [Android build troubleshooting](Docs/UnityAndroidBuildGuide.md), in Chinese.

## Documentation

Architecture and system design documents are listed in the [project documentation index](Y_MultipleAgentWorkflow/Router.md), in Chinese.

Documentation routing and concurrent agent work use [Y_MultipleAgentWorkflow](https://github.com/KAIDO-YONAGI/Y_MultipleAgentWorkflow), a workflow I developed and maintain.

## Credits and License

Art assets are from [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) by Pixel Frog, used under the asset pack license. Project code is licensed under [GPL-3.0](LICENSE).
