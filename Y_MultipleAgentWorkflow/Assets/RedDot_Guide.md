# 待办红点 Guide

文档 ID：`ASSETS-REDDOT-GUIDE`
状态：`Active`
最后更新：`2026-11-14`

## 维护规则

红点树结构、表现绑定、待办导航或叶子判定变化时更新本文，证据记录在[开发日志](DeveloperLog.md)。修改标题时同步目录，维护计数遵循[工作流维护规则](../Workflow/Workflow_Guide.md#维护规则)。画布开关与焦点栈同步维护 [组合契约](../Architecture/Composition/Composition_Guide.md)，界面缓存与实例生命周期见[分层说明](../Architecture/Layering/Layering_Guide.md)。

## 文档内目录

- [1. 代码与资产](#1-代码与资产)
- [2. 树结构与计数](#2-树结构与计数)
- [3. 表现绑定](#3-表现绑定)
- [4. 待办导航](#4-待办导航)
- [5. 业务叶子](#5-业务叶子)
- [6. 约定与硬边界](#6-约定与硬边界)

## 1. 代码与资产

| 位置 | 职责 |
|---|---|
| `Assets/Scripts/Pipeline/UI/RedDot/RedDotManager.cs` | 红点树结构与待办导航的单例，持有根节点、画布分支与导航游标 |
| `Assets/Scripts/Pipeline/UI/RedDot/RedDotNodeBase.cs` | 节点基类：计数、父分支与表现图片的绑定 |
| `Assets/Scripts/Pipeline/UI/RedDot/RedDotBranchNode.cs` | 分支节点：汇总直接子节点计数，根节点与画布分支共用 |
| `Assets/Scripts/Pipeline/UI/RedDot/RedDotLeafBase.cs` | 叶子基类：写入计数并发出变化事件 |
| `Assets/Scripts/Gameplay/Player/StatsRedDotLeaf.cs` | 属性画布叶子，计数在 0 与 1 之间切换 |
| `Assets/Scripts/Gameplay/Player/StatsRedDotTracker.cs` | 属性提升的跟踪器，把伤害与速度变化折算成未查看状态 |
| `Assets/Scripts/Gameplay/Quest/QuestRedDotLeaf.cs` | 任务画布叶子，按已接取任务的可交付状态写入计数 |
| `Assets/Scripts/Gameplay/Skills/SkillRedDotLeaf.cs` | 技能画布叶子，按技能槽位的升级资格写入计数 |
| `Assets/Prefabs/UI/RedDot.prefab` | 红点表现预制体，由界面按需实例化 |
| `Assets/Sprites/UI/Icons/RedDot.png` | 红点图标 |
| `Assets/Settings/Input/ActionRefs/UI_RedDotNavigate.asset` | 待办导航按键的输入资产 |

树与导航位于 `Pipeline.UI.RedDot`；三个业务叶子分属 `Gameplay.Player`、`Gameplay.Quest` 与 `Gameplay.Skills`。`RedDotManager` 与 `StatsRedDotTracker` 复用 `Contracts.Base.YSingleton<T>`。

## 2. 树结构与计数

树分三层：根节点、画布分支、业务叶子。根节点与画布分支同为 `RedDotBranchNode`，分支计数等于直接子节点计数之和；叶子按所属域的规则写入 0 或 1。

- `AddLeaf(CanvasType, RedDotLeafBase, int initialCount)` 校验叶子尚未挂接、画布不为 `Default`，写入初始计数、订阅计数变化、挂接到画布分支并刷新父链。
- `RemoveLeaf(RedDotLeafBase)` 退订、摘除父子关系、解除表现绑定，并刷新原父链。
- 叶子计数变化后由管理器沿父链逐级重算，直到根节点。

树只保存结构与计数。界面缓存淘汰只让表现图片失效，计数与树结构继续保留。

## 3. 表现绑定

- `BindRootImage(Image)` 绑定聚合面板入口的根红点，界面重建后重新绑定即可显示当前状态。
- `RegisterCanvas(CanvasType, Func<bool>)` 登记画布分支与可用性回调，重复登记以最后一次回调为准。
- `BindCanvasImage(CanvasType, Image)` 把聚合面板入口的图片绑到画布分支；分支尚未建立时先记录并在建立后补绑，没有对应分支时隐藏图片。
- `UnbindCanvasImage(CanvasType)` 在分页按钮复用前解除旧画布绑定，并把图片置为隐藏。

表现通过 `Image.enabled` 开关，宿主对象的启停仍由原界面生命周期控制。`IntegratedUICanvasManager` 在 `Start` 绑定根红点与导航顺序，分页切换时绑定或解绑按钮红点。

## 4. 待办导航

待办导航按键由 `navigateAction` 配置，默认为键盘反引号。按下后按导航顺序循环查找：以上次成功定位的画布为起点，逐个检查分支计数是否大于零、可用性回调是否放行，第一个满足的画布经 `UIManager.RequestCanvasToggle` 请求打开或聚焦，一次只消费一个目标。

导航顺序由 `IntegratedUICanvasManager.canvasToToggle` 经 `SetNavigationOrder` 写入，重复画布只参与一次，`Default` 不参与。

打开请求在本帧由 `UIManager` 处理，下一帧在 `LateUpdate` 用 `UICacheManager.IsCanvasOpen` 确认界面是否真正打开。确认成功才把游标移到该画布，失败保持原游标。

## 5. 业务叶子

| 画布 | 叶子 | 计数判定 |
|---|---|---|
| 属性 | `StatsRedDotLeaf` | 伤害或速度存在玩家尚未查看的提升 |
| 任务 | `QuestRedDotLeaf` | 已接取任务的 `IsQuestCompletable` 为真 |
| 技能 | `SkillRedDotLeaf` | 技能已解锁、未满级且当前有可用技能点 |

`StatsRedDotTracker` 常驻场景，订阅属性面板实际展示的伤害与速度两项。基线是上一次看到的数值，数值变大才留下未查看标记，数值下降只下移基线；连续提升只保留一个标记。属性面板绘制完成后调用 `ConfirmDisplayed` 清除标记。读档时先同步基线再清除未查看状态，读档不算新增提醒。

任务叶子随已接取列表建立与移除，未接取的任务即使目标已满足也不提示。技能叶子按技能画布预制体的槽位路径建立，与技能状态使用同一套键，技能等级、解锁状态与技能点变化后由管理器统一重算。

## 6. 约定与硬边界

- 通用节点不认识具体玩法，业务条件由叶子所属域判断。
- 计数只经受保护入口写入，对外只读。
- 一个叶子同时只属于一个父分支。
- 画布分支按 `CanvasType` 建立，`Default` 不参与树与导航。
- 红点表现依赖界面实例，计数与树结构独立于界面是否创建。
- 导航只打开或聚焦画布，不代替业务入口准备数据。
