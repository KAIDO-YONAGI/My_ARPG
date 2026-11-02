# Architecture.AssemblyPlan Developer Log

## 维护规则

按项目约定记录带日期的任务事实、验证证据与维护计数，现行结论同步到对应 Guide。了解现状优先读取 Guide；需要任务历史时，只定位并读取最近一天或两天的相关条目，禁止整份日志载入上下文。只有任务需要追溯历史时才按日期、标题或关键词定向读取更早条目。读取范围不改变存储保留，删除具体历史条目须经用户明确确认。纯文档维护不增加业务计数。

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-11-02 同步事件机制现状

- `AssemblyPlan_Proposal.md` 的现状依据同步为 14 个事件类、16 条事件声明与 30 个事件资产，并引用 Assets 域事件通道指南；Router 使用不含数量的事件线索。提案状态保持 `Proposal`。
- 证据：`Assets/Scripts/Pipeline/SO/Events` 类型文件与事件声明，以及 `Assets/GameSO/Events` 资产清单。纯文档维护，业务计数保持不变。

## 2026-10-05 建立 程序集与热更规划 Proposal

- 任务：为 Architecture.AssemblyPlan 建立领域文档，把五项未实施设计整理为带「目标 / 理由 / 当前状态 / 前置依赖」四要素的 Proposal；五项为 asmdef 拆分、xLua 热更、效果数据驱动加状态机基类、事件总线维持不采用、事件引用可视化。
- 写入的文件：
  - `AssemblyPlan_Proposal.md`，文档 ID `ARCH-ASSEMBLY-PLAN`，状态 `Proposal`，开头声明未实施。
  - `Router.md`，按中文模板重写，Router 状态 Active，Proposal 列在能力边界。
  - `DeveloperLog.md`，本条目。
- 依据的证据：
  - 代码核对：全工程 asmdef 检索与编辑器测试目录 asmdef 检索，命中数均为 0；xLua 源码目录与 Lua 脚本目录均不存在，Lua 脚本计数为 0；`LuaManager`、`StatsManager`、`MovementController` 三个类型名的检索，命中数均为 0；Addressables 配置目录存在。
  - 事件通道 SO 类型族，事件类 11 个、通道 12 条；事件资产清单，共 28 个事件资产。
  - 分派与枚举：`SkillManager` 的技能分派、`UseItem` 的 `ApplyItemEffects` 分派、`PlayerMovement` 的状态分派、`MyEnums` 的状态枚举定义、`StatsService` 的六个写命令、`InitialLoad` 的 `Awake` 初始化内容。
- 本轮核对的静态事实：全工程 asmdef 为 0 个；无 xLua 与 Lua 资产，`LuaManager` 类型未定义；Addressables 已接入；`SkillManager` 按 `skillName` 字符串 switch、`UseItem` 按字段 if 分派、`PlayerMovement` 用 `switch(playerState)`；三套状态枚举彼此独立。
- 维护计数：`0/5 -> 1/5`
