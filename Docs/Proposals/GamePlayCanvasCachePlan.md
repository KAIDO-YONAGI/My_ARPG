# GamePlayCanvas 缓存接入

> 实施记录，非权威文档。业务规则仍以 `Y_MultipleAgentWorkflow/Gameplay/` 下对应 Guide 为准。

## 场景与配置

`GamePlayCanvases/RuntimeViews` 挂载 `UICacheManager`，其序列化列表为每个界面保存 Prefab 与场景 Anchor：

| 画布组 | Prefab | Anchor |
|---|---|---|
| Stats | `StatsCanvas.prefab` | `RuntimeViews/StatsCanvas` |
| Skills | `SkillsCanvas.prefab` | `GamePlayCanvases/SkillsCanvas` |
| Shop | `ShopCanvas.prefab` | `GamePlayCanvases/ShopCanvas` |
| Dialog | `DialogCanvas.prefab` | `GamePlayCanvases/DialogCanvas` |
| Quest | `QuestCanvas.prefab` | `GamePlayCanvases/QuestCanvas` |
| Backpack | `BackpackCanvas.prefab` | `GamePlayCanvases/BackpackCanvas` |

Prefab 资源位于 `Assets/Prefabs/UI/Runtime/`。Anchor 是场景占位对象；界面实例以 Anchor 为父节点创建。

## 生命周期

`UIManager` 负责输入、焦点和状态协调。画布状态确认打开后，它发布携带 `CanvasToToggle` 的 `CanvasOpenedEventSO`；`UICacheManager` 订阅该事件，记录最近使用顺序。

关闭的实例保留在缓存中，`CachedUIView.IsOpen` 标记显示状态。闲置超时默认 120 秒、闲置容量默认 3 个，均可在 Inspector 调整。超时按关闭时间检查；容量超限时按最近打开顺序淘汰最久未使用的关闭实例。打开中的实例不进入淘汰队列。

## 业务状态

背包、技能、商店和对话管理器位于持久场景的 Anchor 或 `GlobalManagers`，界面打开时绑定动态视图引用。背包管理器从 Prefab 槽位读取初始容量，并在实例销毁前保存物品状态；技能管理器在销毁前保存等级与解锁状态，重建时恢复。

## 验证记录

Unity 2022.3.62f3c1 编辑器已检查 Prefab、Anchor、事件资产和序列化映射。运行时开关与超时淘汰仍需在 Play Mode 中验证。
