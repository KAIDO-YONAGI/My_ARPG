# Gameplay Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Gameplay 域权威文档（Router 重写）

**写入的文件**
- `Y_MultipleAgentWorkflow/Gameplay/Router.md`（重写为项目中文模板；任务线索表分派到 6 个子域；下级导航 6 行原样保留，第二列精确为 `PlayerStats\Router.md`、`Units\Router.md`、`Dialog\Router.md`、`Quest\Router.md`、`InventoryShop\Router.md`、`Skills\Router.md`）
- `Y_MultipleAgentWorkflow/Gameplay/Dialog/Router.md`、`Y_MultipleAgentWorkflow/Gameplay/Dialog/Dialog_Guide.md`（详见 Dialog DeveloperLog）
- `Y_MultipleAgentWorkflow/Gameplay/Quest/Router.md`、`Y_MultipleAgentWorkflow/Gameplay/Quest/Quest_Guide.md`（详见 Quest DeveloperLog）

**依据的证据路径**
- 域内真实可写路径：`Y_MultipleAgentWorkflow/Gameplay/`（含 Dialog/Quest 子目录及 Router；PlayerStats/Units/InventoryShop/Skills 目前仅有 Router）
- 线索映射依据：`Assets/Scripts/Gameplay/{Dialog,Quest,Inventory,Skills,Player,Units}/` 与 `Assets/Scripts/Pipeline/SO/` 的实际目录归属
- 跨域依赖依据：`Assets/Scripts/Gameplay/Dialog/DialogManager.cs:178-179`、`Assets/Scripts/Gameplay/Quest/QuestManager.cs:292` 均读 `ItemHistoryManager`（写入方在 `Assets/Scripts/Gameplay/Inventory/InventoryManager.cs:102/123/136/225`）

**已核验**
- 6 行下级导航结构与既有 Router 文件一一对应（`PlayerStats`、`Units`、`Dialog`、`Quest`、`InventoryShop`、`Skills` 六个目录均存在 Router.md）。
- Gameplay/Router 只做分派与导航，未写任何实现细节；实现事实全部落在子域 Guide。

**未核验**
- 未运行 Unity，也未运行工作流结构校验脚本（按授权禁止运行 `WorkingAgent.ps1`），Router 与 Guide 的格式一致性仅经人工比对模板。

**发现的缺陷**
- `PlayerStats`、`Units`、`InventoryShop`、`Skills` 四个子域只有 Router、无 Guide，线索暂只能指向 Router，属文档覆盖缺口。
- 跨域隐性依赖（对话/任务都读背包域写的 `ItemHistoryManager`）此前未在任何文档中登记，本次登记进两个子域 Guide 的硬边界。