# 音频系统 Guide

文档 ID：`ASSETS-AUDIO-GUIDE`
状态：`Active`
最后更新：`2026-11-07`

## 维护规则

音频接口、配置结构、对象池或场景接线变化时更新本文，证据记录在[开发日志](DeveloperLog.md)。修改标题时同步目录，维护计数遵循[工作流维护规则](../Workflow/Workflow_Guide.md#维护规则)。场景启动流程同步维护 [SceneFlow Guide](../SceneFlow/SceneFlow_Guide.md)。

## 文档内目录

- [1. 代码与资产](#1-代码与资产)
- [2. 播放与回收](#2-播放与回收)
- [3. 常驻场景与启动](#3-常驻场景与启动)
- [4. 调用方式](#4-调用方式)
- [5. 配置与扩展](#5-配置与扩展)

## 1. 代码与资产

| 位置 | 职责 |
|---|---|
| `Assets/Scripts/Contracts/Audio/` | `AudioType`、`BGMType` 和携带可选世界坐标的 `AudioRequest` |
| `Assets/Scripts/Pipeline/Audio/AudioPlayerManager.cs` | 全局音频单例，管理请求订阅、音效池与 BGM |
| `Assets/Scripts/Pipeline/Audio/AudioPlayer.cs` | 池化音源的播放、停止与自动归还 |
| `Assets/Scripts/Pipeline/Audio/AudioConfigSO.cs` | 按类型组织片段并按权重选取 |
| `Assets/Scripts/Pipeline/Audio/AudioTrigger.cs` | 按钮、动画事件与场景交互的 Inspector 调用入口 |
| `Assets/Scripts/Pipeline/Audio/UIAudioManager.cs` | 画布组与音效类型的序列化映射表，画布打开事件的订阅方 |
| `Assets/Scripts/Pipeline/SO/Events/FeedbackEventSO.cs` | 发布 `AudioRequest` 的共享事件通道 |
| `Assets/GameSO/Audio/AudioConfig.asset` | 音效与 BGM 的片段配置 |
| `Assets/GameSO/Events/FeedbackEventSO.asset` | 请求发送者与管理器共用的事件资产 |
| `Assets/Audio/` | 音效与背景音乐素材，作为 Unity 导入资产引用 |

契约命名空间为 `Contracts.Audio`，运行组件为 `Pipeline.Audio`，事件通道为 `Pipeline.SO.Events`。`AudioPlayerManager` 复用 `Contracts.Base.YSingleton<T>`。

## 2. 播放与回收

`PlaySfx` 从配置表选取片段，优先复用空闲播放器，容量允许时按需创建。播放器对象统一挂在管理器下，播放结束或 `StopAndReturn` 时停用并归还队列，重复停止保持单次归还。池容量与预热数量在管理器 Inspector 配置。

省略世界坐标时使用 2D 音效，传入坐标时使用 3D 音效。每次播放重设世界坐标与 `spatialBlend`，复用实例可以交替承载两种音效。3D 位置是请求发出时的坐标快照。

背景音乐使用独立循环音源。`PlayBGM` 依次淡出旧片段、替换片段、淡入新片段；新请求接替当前切换。淡变使用 `Time.deltaTime`，随游戏暂停停止推进。`StopBGM` 中止切换并停止音乐，`StopAllSfx` 停止并回收全部音效播放器。

## 3. 常驻场景与启动

正常入口为 `Assets/Scenes/InitialScene.unity`。`InitialLoad.persistentScenes` 引用 `Assets/GameSO/GameSceneSO/PersistentScene.asset`，先登记并叠加加载常驻场景。常驻场景中的 `SceneChanger.Start` 通过原有加载入口打开菜单场景。

`PersistentScene/GlobalManagers/AudioPlayerManager` 持有音频配置、音效请求事件和本项目的 `SceneLoadedEvent` 资产。管理器在单例初始化钩子校验引用并创建背景音源，`OnEnable` 订阅两个事件，`OnDisable` 成对退订。`Start` 预热音效池并按 `startupBgmType` 播放 BGM；场景组加载完成事件触发 `LoadScene` 音效。

音量、池容量、预热、淡变时长及启动音乐类型在该节点的 Inspector 配置，片段选择在 `AudioConfig.asset` 配置。

## 4. 调用方式

跨模块发布请求时，序列化持有共享的 `FeedbackEventSO.asset`，在 `Awake` 校验引用后调用：

```csharp
using Contracts.Audio;
using AudioType = Contracts.Audio.AudioType;

// 2D 音效。
feedbackEvent.RequestAudio(new AudioRequest(AudioType.Hurt));

// 在调用时的世界坐标播放 3D 音效。
feedbackEvent.RequestAudio(new AudioRequest(AudioType.Hurt, transform.position));
```

需要直接控制播放时使用管理器接口：

```csharp
using Contracts.Audio;
using Pipeline.Audio;
using AudioType = Contracts.Audio.AudioType;

AudioPlayerManager.RequiredInstance.PlaySfx(AudioType.PickUp);
AudioPlayerManager.RequiredInstance.PlayBGM(BGMType.Gaming);
AudioPlayerManager.RequiredInstance.StopAllSfx();
AudioPlayerManager.RequiredInstance.StopBGM();
```

按钮或动画事件可以调用 `AudioTrigger.Play()`：将组件挂在音效来源对象上，配置共享事件资产和音效类型，以 `useWorldPosition` 选择空间播放。2D 玩法中的通用反馈使用 2D 播放。

`UnityEngine` 也声明 `AudioType`。同时引用两个命名空间的代码使用上述类型别名，保持编译时类型明确。

## 5. 配置与扩展

`UIAudioManager.canvasAudioBindings` 按画布登记打开音效。收到 `CanvasOpenedEventSO` 后，只为已登记的画布发布音效请求；未登记的画布直接跳过，界面正常打开。

`AudioConfigSO` 的音效与音乐条目分别按 `AudioType`、`BGMType` 匹配，同类型多个片段按权重随机选择；权重全零时取第一项。类型在配置中缺席时返回空片段，本次请求跳过播放。

扩展时保持既有枚举值与序列化资产的对应关系，再为类型配置片段。素材及其 `.meta` 一起维护，重命名或移动时保留 Unity GUID，以维持配置资产的片段引用。新增请求发送者复用管理器订阅的同一个事件资产。
