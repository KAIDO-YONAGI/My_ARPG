# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-000000?logo=unity)
![C#](https://img.shields.io/badge/C%23-512BD4?logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/License-GPL--3.0-blue)

A 2D top-down ARPG prototype built with Unity and C#. Exploration, combat and NPC interaction connect character progression, dialogue, quests, inventory, shops and a skill tree.

The project focuses on integrating gameplay systems and implementing client-side infrastructure: scene organization, pathfinding and UI lifecycle management.

[中文](README.md) · [Setup and controls](Docs/GettingStarted.en.md)

## Demo

[ARPG Demo Showcase](https://www.bilibili.com/video/BV1sCGH6iErJ/) · Bilibili, showcasing an earlier version of the project.

## Gameplay and Systems

| System | Features |
|---|---|
| Exploration and combat | Top-down movement, switching between melee slashes and ranged arrows, damage and knockback, retry after death and travel between scenes |
| Enemies and NPCs | Enemy idle, chase and attack states; NPC patrol, wandering and dialogue interaction |
| Character progression | Experience and leveling, a stats panel, skill points, skill levels and prerequisite unlocks |
| Dialogue and quests | Branching dialogue, prerequisites and interaction history; item collection and character conversation objectives, quest progression and rewards |
| Inventory and shops | Item pickup, stacking, use and dropping; gold, product categories, buying and selling |

## Technical Highlights

- **Scene organization and system coordination**: Additive scene groups and persistent scenes separate shared systems from level content. Load-request and completion events let other systems participate in scene transitions and coordinate their work.
- **Configuration and event communication**: ScriptableObjects configure dialogue, quests, items and skills. Event channels connect interactions such as quest rewards and UI toggles, reducing direct references between systems and supporting independent maintenance and extension.
- **Custom A\* pathfinding**: Grid construction, path search and path following are separate components shared by enemies and NPCs. Supports eight-direction movement, diagonal obstacle checks and dynamic replanning, with a binary min-heap for the open list and reusable search collections.
- **On-demand UI and caching**: Panels are created when needed, cached on close and evicted by LRU order or timeout. Gameplay state lives independently of UI instances, and dirty flags drive display updates. A focus stack manages window order, and panels support dragging.
- **Separated gameplay logic and presentation**: Player stats use MVCS to centralize rules and write operations while providing a read-only interface. Dialogue, quests, inventory, shops and skills separate business logic from views so each can be maintained independently.
- **Object reuse and regression tests**: Arrows and loot use a generic object pool. EditMode tests verify player stat rules, the pathfinding open list, object pooling, the UI focus stack and view caching.

## Documentation and Collaboration

- [Setup, controls and building](Docs/GettingStarted.en.md)
- [Android build troubleshooting](Docs/UnityAndroidBuildGuide.md) · Chinese
- [Architecture and system documentation index](Y_MultipleAgentWorkflow/Router.md) · Chinese

The project uses the author's [multi-agent workflow](https://github.com/KAIDO-YONAGI/Y_MultipleAgentWorkflow), with documentation routing and concurrency leases for project maintenance.

## Credits and License

- Art: [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) by Pixel Frog, used under the asset pack license.
- Code: [GPL-3.0](LICENSE).
