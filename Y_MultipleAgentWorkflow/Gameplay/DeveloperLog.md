# Gameplay Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

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
