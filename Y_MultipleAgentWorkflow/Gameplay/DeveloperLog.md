# Gameplay Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-01 缓存画布基类命名统一

五个玩法画布视图改继承 `CachedCanvasViewBase`，属性面板 `StatsCanvasView` 改继承 `CachedUIViewBase`。更新玩家属性、背包商店和事件通道指南中的当前类型名。UI 缓存生命周期回归通过；全量 EditMode 的三条背包槽位 `OnValidate` 空引用失败与本次修改无关。维护计数更新为 `0/5 -> 1/5`。

## 2026-11-01 管理器视图改为直接查询 State

对话、任务、背包、商店与技能视图通过稳定的 `CurrentState` 按字段或索引查询；刷新不再创建快照或复制集合，刷新标记与清理由各 `State` 方法提供。Gameplay 维护计数达到 `5/5`，复核路由与五个子域 Guide 后同步实现事实并清零为 `0/5`。Unity Editor 刷新完成，控制台返回 0 条错误；未运行 EditMode 测试，工程验证要求测试前关闭该工程的 Editor。

## 2026-11-01 管理器刷新状态改为版本化只读快照

对话、任务、商店、背包与技能管理器不再公开可变刷新状态，视图按只读快照与字段版本确认刷新；快捷栏金币与槽位独立刷新。必需的单例依赖统一通过 `RequiredInstance` 明确取得。

Unity Editor 对 `Assembly-CSharp` 与 `Assembly-CSharp-Editor` 均有成功编译记录；未运行 EditMode 测试，因工程验证要求运行前关闭该工程的 Unity 实例。Gameplay 维护计数更新为 `3/5 -> 4/5`。

## 2026-10-31 五个玩法域拆出界面层

- 对话、任务、商店、背包、技能五个域统一改为「管理器持数据 + `CachedCanvasView` 持界面」：管理器不再实现 `ICanvasManager`，也不再持有 `CanvasGroup` 与控件引用，改暴露 `State` 与 `DirtyValue` 字段。
- 界面实体移入 `Assets/Prefabs/UI/Runtime/`，由 `UICacheManager` 按需创建；常驻快捷栏例外，由 `HotbarCanvasView` 常驻。
- 细节见各子域 DeveloperLog。

## 2026-10-31 属性面板预制体与闲置缓存

属性面板从常驻场景实例迁移为按需创建的 StatsCanvas.prefab；数值模型仍由 StatsService 持有。细节见 PlayerStats/PlayerStats_Guide.md §2.10。

Unity 重编译及预制体引用检查完成；未运行测试或进入 Play 模式，复用与多界面淘汰仍待运行时验证。维护计数更新为 `2/5`。

## 2026-10-05 建立 Gameplay 域权威文档（Router 重写）

**写入的文件**
- `Router.md`（重写为项目中文模板；任务线索表分派到 6 个子域；下级导航 6 行原样保留，第二列精确为 `PlayerStats\Router.md`、`Units\Router.md`、`Dialog\Router.md`、`Quest\Router.md`、`InventoryShop\Router.md`、`Skills\Router.md`）
- `Dialog\Router.md`、`Dialog\Dialog_Guide.md`（详见 Dialog DeveloperLog）
- `Quest\Router.md`、`Quest\Quest_Guide.md`（详见 Quest DeveloperLog）

**依据的证据路径**
- 域内可写范围：本目录与其下 6 个子目录。
- 线索映射依据：产品脚本中 Gameplay 下六个子目录与 Pipeline 的 SO 目录的实际归属。
- 跨域依赖依据：`DialogManager` 与 `QuestManager` 均读 `ItemHistoryManager`，写入方在 `InventoryManager`。

**已核验**
- 6 行下级导航结构与既有 Router 文件一一对应（`PlayerStats`、`Units`、`Dialog`、`Quest`、`InventoryShop`、`Skills` 六个目录均存在 Router.md）。
- Gameplay/Router 只做分派与导航，未写任何实现细节；实现事实全部落在子域 Guide。
