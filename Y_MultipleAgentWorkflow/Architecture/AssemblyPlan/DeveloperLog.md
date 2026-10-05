# Architecture.AssemblyPlan Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 程序集与热更规划 Proposal

- 任务：为 Architecture.AssemblyPlan 建立领域文档，把五项未实施设计（asmdef 拆分、xLua 热更、效果数据驱动 + 状态机基类、事件总线维持不采用、事件引用可视化）整理为带「目标 / 理由 / 当前状态 / 前置依赖」四要素的 Proposal，并核对其上游线索文档的现状描述。
- 写入的文件：
  - `Y_MultipleAgentWorkflow\Architecture\AssemblyPlan\AssemblyPlan_Proposal.md`（`ARCH-ASSEMBLY-PLAN`，状态 `Proposal`，开头声明未实施）
  - `Y_MultipleAgentWorkflow\Architecture\AssemblyPlan\Router.md`（重写为中文模板格式，Router 状态 Active，Proposal 列在能力边界）
  - `Y_MultipleAgentWorkflow\Architecture\AssemblyPlan\DeveloperLog.md`（本条目）
- 依据的证据路径：
  - `Docs/My_ARPG_重构优化清单_未解决.md`（主线索，含 xLua 附录）
  - 代码核对：`Assets/**` 全量 `*.asmdef` 检索（0 命中）；`Assets/Tests/**` asmdef 检索（0 命中）；`Assets/xLua/`、`Assets/LuaScripts/` 存在性判定与 `*.lua` 计数（均无/为 0）；`class LuaManager`、`class StatsManager`、`class MovementController` 检索（均 0 命中）；`Assets/AddressableAssetsData/` 存在性（存在）
  - `Assets/Scripts/Pipeline/SO/Events/*.cs`（事件类 11 个、通道 12 条）；含 `Event` 路径的 `*.asset` 清单（事件资产 28 个）
  - `Assets/Scripts/Gameplay/Skills/SkillManager.cs:19-21`、`Gameplay/Inventory/UseItem.cs:8-19`、`Gameplay/Player/PlayerMovement.cs:171`、`Contracts/MyEnums.cs:3-18`、`:39-45`、`Gameplay/Player/Services/StatsService.cs:40-50`、`Pipeline/InitialLoad.cs:11-15`
- 已核验（静态核对已做）：全工程 0 个 asmdef；无 xLua/Lua 资产与 `LuaManager`；Addressables 已接入；`SkillManager` 按 `skillName` 字符串 switch、`UseItem` 按字段 if 分派、`PlayerMovement` 用 `switch(playerState)`；三套状态枚举彼此独立。
- 未核验：用户当前排期意愿；事件类/通道/资产计数是否穷尽；Addressables 分组与 Remote/CDN 配置；拆 asmdef 后的实际可编译性（未运行 Unity，未做编译验证）。
- 发现的缺陷：上游线索文档事件类路径失效（写 `Gameplay/SO/Events`，实为 `Pipeline/SO/Events`）；事件资产计数过期（记约 24，实测 28）；`MovementController.cs:187` 指向不存在的类；xLua 附录骨架引用不存在的 `StatsManager`/`StatsBridge`；无 asmdef 使跨域依赖只能靠口头约定。
- 维护计数：`0/5 -> 1/5`