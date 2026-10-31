# My_ARPG Setup, Controls and Building

[Back to project overview](../README.en.md) · [中文](GettingStarted.md)

## Requirements and Launch

- Editor: Unity 2022.3.62f3c1. See [ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt).
- Main dependencies: Unity 2D, Input System, Cinemachine, TextMesh Pro and Newtonsoft.Json. See [manifest.json](../Packages/manifest.json) for the full list.
- Local tooling dependency: `com.coplaydev.unity-mcp` uses a `file:` path outside the repository. Ensure that path resolves to a package directory on your machine before opening the project. See the [project configuration guide](../Y_MultipleAgentWorkflow/Build/ProjectConfig_Guide.md), in Chinese.

1. Open the project in Unity Hub and wait for package resolution and asset import.
2. Open `Assets/Scenes/InitialScene.unity` and enter Play Mode. This entry loads the persistent scenes and title menu.
3. Start a game from the title menu, or continue from a system save.

The title menu is at `Assets/Scenes/GameScene/StartingMenu.unity`; gameplay scenes are `Scene1.unity` and `Scene2.unity` in the same directory. `Assets/Scenes/TestScene.unity` supports isolated debugging. Use `InitialScene` for the full flow.

## Controls

| Input | Action |
|---|---|
| `WASD` / arrow keys | Move |
| `Q` | Switch between melee and ranged modes |
| `J` | Slash or shoot in the current mode |
| `1` | Stats panel |
| `2` | Skill panel; left-click a slot to spend skill points on upgrades |
| `F` | Shop interaction |
| `T` | Open or close NPC dialogue |
| Left mouse button | Advance dialogue or select an option |
| `C` | Quest menu |
| `Esc` | Exit menu, including return to title, saving and quitting |

Bindings are in [GameInput.inputactions](../Assets/Settings/Input/GameInput.inputactions). The in-game guide is [GameGuide.txt](../Assets/StreamingAssets/GameGuide.txt), in Chinese.

- The lower-left menu provides panel shortcuts. Shop, dialogue and quest-board interactions depend on the player's proximity.
- Most panels support closing with `X`, dragging by the top bar and clicking to bring them into focus.
- Left-click an inventory item to use it, or to sell it while a shop is open. Right-click to drop it.
- The end of a wooden bridge triggers a scene transition. Death opens the GameOver menu with a Retry option.
- On mobile, use the on-screen joystick to move and the attack button to attack.

## Save and Load

- In gameplay scenes, the save panel provides `Save` / `Load`. In the title menu, it provides `Delete` / `Load`.
- Scene transitions trigger automatic system saves. Continue loads a system save, while the save panel manages manual saves.
- Saves store player stats, scene and player position, and scene loot state in `Application.persistentDataPath`.
- Player-dropped items are temporary scene objects and are cleared on scene transitions, reloads or Retry.

See the [save system guide](../Y_MultipleAgentWorkflow/Data/SaveData_Guide.md), in Chinese, for persistence details.

## Building

`InitialScene` is the first scene in Build Settings. Scenes loaded at runtime must also be included in the build list. See [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset) and the [scene flow guide](../Y_MultipleAgentWorkflow/SceneFlow/SceneFlow_Guide.md), in Chinese.

For Android, install Android Build Support, SDK, NDK and OpenJDK for the matching editor through Unity Hub. Then switch the target to Android in Build Settings and build.

The following detailed guides are in Chinese:

- [Android build troubleshooting](UnityAndroidBuildGuide.md): environment checks, SDK and Gradle proxy setup, and resource naming issues in Release builds.
- [Project configuration](../Y_MultipleAgentWorkflow/Build/ProjectConfig_Guide.md): dependencies, build scenes and Player Settings.
- [Test guide](../Y_MultipleAgentWorkflow/Build/TestBaseline_Guide.md) and [validation commands](../Y_MultipleAgentWorkflow/Workflow/Project_Validation_Guide.md): EditMode tests and verification entry points.

## Development References

The following system documents are in Chinese:

- [Layering and read/write boundaries](../Y_MultipleAgentWorkflow/Architecture/Layering/Layering_Guide.md)
- [Initialization, UI focus and caching](../Y_MultipleAgentWorkflow/Architecture/Composition/Composition_Guide.md)
- [ScriptableObjects and event channels](../Y_MultipleAgentWorkflow/Assets/EventChannels_Guide.md)
- [System documentation index](../Y_MultipleAgentWorkflow/Router.md)
