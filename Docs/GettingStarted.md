# My_ARPG 运行、操作与构建

[返回项目介绍](../README.md) · [English](GettingStarted.en.md)

## 环境与启动

- 编辑器版本：Unity 2022.3.62f3c1，版本来源见 [ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt)。
- 主要依赖：Unity 2D、Input System、Cinemachine、TextMesh Pro、Newtonsoft.Json，完整清单见 [manifest.json](../Packages/manifest.json)。
- 本地工具依赖：包清单中的 `com.coplaydev.unity-mcp` 使用仓库外的 `file:` 路径，打开工程前需让该路径对应本机可用的包目录。依赖与工程配置说明见 [项目配置指南](../Y_MultipleAgentWorkflow/Build/ProjectConfig_Guide.md)。

1. 使用 Unity Hub 打开工程，等待包解析与资源导入完成。
2. 打开 `Assets/Scenes/InitialScene.unity` 并进入 Play Mode。该入口负责加载常驻场景与标题菜单。
3. 从标题菜单开始游戏，或继续系统存档。

标题菜单位于 `Assets/Scenes/GameScene/StartingMenu.unity`，游戏场景为同目录下的 `Scene1.unity` 与 `Scene2.unity`。`Assets/Scenes/TestScene.unity` 用于独立调试；完整流程从 `InitialScene` 启动。

## 操作

| 按键 | 功能 |
|---|---|
| `WASD` / 方向键 | 移动 |
| `Q` | 切换近战与远程模式 |
| `J` | 当前模式下挥砍或射箭 |
| `1` | 属性面板 |
| `2` | 技能面板，左键技能槽消耗技能点升级 |
| `F` | 商店交互 |
| `T` | 打开或关闭 NPC 对话 |
| 鼠标左键 | 推进对话或选择选项 |
| `C` | 任务菜单 |
| `Esc` | 退出菜单，包含返回标题、存档与退出游戏 |

按键绑定见 [GameInput.inputactions](../Assets/Settings/Input/GameInput.inputactions)，游戏内说明见 [GameGuide.txt](../Assets/StreamingAssets/GameGuide.txt)。

- 左下角集成菜单提供面板入口，商店、对话与任务板交互受角色所在范围影响。
- 大多数面板支持点击 `X` 关闭、拖动顶部移动，点击窗口可调整焦点与显示层级。
- 背包物品左键使用，商店打开时左键出售，右键丢弃。
- 木桥尽头触发场景切换；死亡后打开 GameOver 菜单，可选择 Retry。
- 移动端使用屏幕摇杆移动、攻击按钮攻击。

## 存档与读档

- 游戏场景中的存档面板提供 `Save` / `Load`；标题菜单中同一入口提供 `Delete` / `Load`。
- 场景切换触发系统自动存档，标题菜单的 Continue 读取系统存档；手动档在存档面板中管理。
- 存档保存玩家属性、场景与角色位置、场景掉落物状态，文件写入 `Application.persistentDataPath`。
- 玩家丢弃的物品按场景临时物体处理，场景切换、重载或 Retry 后清理。

持久化机制见 [存档系统指南](../Y_MultipleAgentWorkflow/Data/SaveData_Guide.md)。

## 构建

构建入口为 `InitialScene`，在 Build Settings 中位于首位。运行时加载的场景也需加入构建列表；当前列表见 [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset)，场景组与常驻场景机制见 [场景流程指南](../Y_MultipleAgentWorkflow/SceneFlow/SceneFlow_Guide.md)。

Android 构建通过 Unity Hub 安装对应编辑器的 Android Build Support、SDK、NDK 与 OpenJDK，再在 Build Settings 中切换到 Android 并构建。

- [Android 构建与排障](UnityAndroidBuildGuide.md)：环境确认、SDK 与 Gradle 代理配置、Release 构建资源名问题。
- [项目配置指南](../Y_MultipleAgentWorkflow/Build/ProjectConfig_Guide.md)：依赖、构建场景与 Player Settings。
- [测试说明](../Y_MultipleAgentWorkflow/Build/TestBaseline_Guide.md) 与 [项目验证命令](../Y_MultipleAgentWorkflow/Workflow/Project_Validation_Guide.md)：EditMode 测试与验证入口。

## 开发参考

- [分层与读写边界](../Y_MultipleAgentWorkflow/Architecture/Layering/Layering_Guide.md)
- [初始化、UI 焦点与缓存](../Y_MultipleAgentWorkflow/Architecture/Composition/Composition_Guide.md)
- [ScriptableObject 与事件通道](../Y_MultipleAgentWorkflow/Assets/EventChannels_Guide.md)
- [各系统文档索引](../Y_MultipleAgentWorkflow/Router.md)
