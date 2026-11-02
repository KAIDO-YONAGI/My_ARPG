# Assets Developer Log

## 维护规则

按项目约定记录带日期的任务事实、验证证据与维护计数，现行结论同步到对应 Guide。了解现状优先读取 Guide；需要任务历史时，只定位并读取最近一天或两天的相关条目，禁止整份日志载入上下文。只有任务需要追溯历史时才按日期、标题或关键词定向读取更早条目。读取范围不改变存储保留，删除具体历史条目须经用户明确确认。纯文档维护不增加业务计数。

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-02 事件通道计数核对

- 核对 `Assets/Scripts/Pipeline/SO/Events` 为 14 个通道类、16 条 `public event`，`Assets/GameSO/Events` 为 30 个事件资产；修正 `EventChannels_Guide.md` §2.5 的类数。
- 证据：通道目录的类型文件清单、`rg -n 'public event' Assets/Scripts/Pipeline/SO/Events` 与事件资产目录清单。纯文档维护，业务计数保持不变。

## 2026-11-02 画布打开音效映射表

- `UIAudioManager` 增加 `[Serializable] CanvasAudioBinding`，字段为 `CanvasToToggle canvas` 与 `AudioType audioType`，并增加序列化列表 `canvasAudioBindings`；画布组与音频的打开关系在 Inspector 配置，音效片段仍由 `AudioConfigSO` 按类型选取。
- 证据：`Assets/Scripts/Pipeline/Audio/UIAudioManager.cs`。

## 2026-11-02 音频系统迁入与常驻接线

- 从 My_3DProject 提取音效播放、加权配置、播放器池、BGM 与请求通道，迁入 19 个音频片段，保留素材及配置的 `.meta`。音频契约归 `Contracts.Audio`，运行组件归 `Pipeline.Audio`，事件归 `Pipeline.SO.Events`。
- 移除管理器的 Mirror 转发依赖，提供通用 `AudioTrigger.Play` 供按钮和动画事件调用。常驻节点接入本项目 `SceneLoadedEvent` 与 `FeedbackEventSO`，音量在其 Inspector 配置。
- `Audio_Guide.md` 登记接口与使用方式；复核 GameSO 清单为 63 个资产，其中 33 个配置与 30 个事件，事件通道为 14 类。Assets 计数达到 5/5 后复核配置、事件与音频 Guide，归零为 0/5。
- 证据：`Assets/Scripts/Pipeline/Audio/`、`Assets/Scripts/Contracts/Audio/`、`Assets/GameSO/Audio/`、`Assets/Audio/`、常驻场景中的音频节点和启动场景的加载列表。完成文件与序列化引用复核；Unity MCP 返回实例数 0，未执行编译确认、Play 试听或测试。

## 2026-10-31 事件通道新增开面板事件

- `Pipeline/SO/Events` 从 12 个通道类增至 13 个，新增 `CanvasOpenedEventSO`（`Action<CanvasToToggle> CanvasOpened`）与对应资产，通道资产总数 28 → 29。
- 登记方式更正：缓存界面的开关事件仍登记在 `UIManager.toggleCanvasEvents` 供按键查找，预制体则登记在 `UICacheManager.views` 供缓存实例化与订阅。

## 2026-10-31 属性面板预制体与闲置缓存

提取 Assets/Prefabs/UI/Runtime/StatsCanvas.prefab 并保留内部控件与事件引用。PersistentScene 删除旧 StatsCanvas，将 Prefab 注册到 UIManager，实例父节点为 GamePlayCanvases/RuntimeViews；Stats 不再重复登记于旧事件列表。

Unity 重编译及预制体引用检查完成；未运行测试或进入 Play 模式，复用与多界面淘汰仍待运行时验证。维护计数更新为 `2/5`。

## 2026-10-05 建立 Assets 域权威文档

**写入的文件**

- `Assets_Guide.md`，ID `ASSETS-GUIDE`，状态 Active
- `EventChannels_Guide.md`，ID `ASSETS-EVENTCHANNELS-GUIDE`，状态 Active
- `Router.md`，重写为项目中文模板，ID `BUS-ASSETS`，下级导航无子类
- `DeveloperLog.md`，本条追加

**依据的证据**

- 代码：配置类型 `ItemSO`、`QuestSO`、`DialogSO`、`RefuseDialogSO`、`CharacterSO`、`GameSceneSO`、`SkillSO`、`PlayerStatsSO`、`GuidSO`、`LocationSO`；事件通道的 12 个类；`OpenTxtWithSystem`；`UIManager`；`SkillManager` 与 `SkillSlot`；`MyEnums`；`PlayerStatsSOTests`
- 资产：GameSO 目录下的 59 个资产、Events 目录下的 28 个通道资产、StreamingAssets 目录下的 GameGuide 文件、StartingMenu 场景里绑定 `OpenTxtWithSystem` 的按钮
- 统计方法：以每个资产的 `m_Script` GUID 对照脚本元数据的 GUID 反查类名后分组；引用计数以资产 GUID 对全工程 428 个场景、预制体与资产文件做 GUID 匹配，排除 GameSO 目录自身

**已核验项**

- GameSO 目录共 59 个资产：配置类 31 个，其中 ItemSO 6、QuestSO 3、DialogSO 8、RefuseDialogSO 3、CharacterSO 3、GameSceneSO 5、SkillSO 2、PlayerStatsSO 1、LocationSO 0；事件类 28 个。
- 事件通道共 12 个类、28 个资产：ToggleCanvasEventSO 10、VoidEventSO 6、InventorySlotsStatsSO 3，其余 9 类各 1。
- 12 个通道类全部直接继承 `ScriptableObject`，无公共基类；泛型参数仅引用 SO 与枚举，零处引用 Manager、Controller 或 View。
- 订阅表达式 49 处 / 22 个文件，发布调用 23 处 / 15 个文件；订阅统一 `OnEnable +=` 与 `OnDisable -=`。
- 10 个 toggle 资产的 `canvasToToggle` 值互不重复，唯一无资产的是枚举成员 `Default`。
- 全工程路径含目录名的非 ASCII 数量 = 0；StreamingAssets 目录仅 GameGuide 文件，1372 字节。
- 全工程 `[CreateAssetMenu]` 21 处；Editor 目录仅 `InputActionReferenceRebuilder`，它只重建输入系统的 `InputActionReference`，与 GameSO 无关。

**维护计数**

`BUS-ASSETS` 维护计数保持 `0/5`；本轮为建立，非维护轮次。
