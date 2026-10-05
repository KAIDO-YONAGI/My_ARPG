# 根工作流开发日志

本文件记录跨业务的结构变更与根索引维护。业务证据写进对应业务根的 DeveloperLog。

## 2026-10-05：工作流初始化

- 按 `multiple-agent-workflow-config` 方法初始化根结构：根 Router、工作流指南、
  WorkingAgent 租约注册表（`WorkingAgent\`）与 7 个业务根。
- 业务根：`Workflow`、`Architecture`、`Gameplay`、`SceneFlow`、`Assets`、`Data`、`Build`；
  二级：`Architecture.{Layering,Composition,AssemblyPlan}`、
  `Gameplay.{PlayerStats,Units,Dialog,Quest,InventoryShop,Skills}`。
- 分类依据来自只读盘点（5 路并行调查 + 3 路补齐），不是按源码目录形状切分。
- 模型入口初始化时未修改（`EntryMode=None`）；全仓本就不存在 `AGENTS.md` / `CLAUDE.md` /
  项目级 Skill，入口职责暂由根 `Router.md` 的“必读起点”承担。
  **该状态已于同日变更：用户拍板建立两个模型入口文件，见本文件末条。**
- 项目验证模式选 `Guide`，落地在 `Workflow\Project_Validation_Guide.md`。

## 2026-10-05：权威文档库建立

- 10 路写手并行产出 16 份域权威文档（7 个一级域），路径互不相交，全程中文：
  `Architecture` 3 份（Layering / Composition / AssemblyPlan-Proposal）、
  `Gameplay` 6 份、`SceneFlow` 1 份、`Assets` 2 份、`Data` 1 份、`Build` 3 份。
- 每份业务 Router 按项目中文模板重写，保留父级所需的下级导航行。
- 根 Router 建立可解析的文档索引（41 条），结构校验逐条核对索引路径。

## 2026-10-05：并发冲突记录

- 取得父租约 `ec977814-b875-4904-a0bb-e62172c1c5b9`（`DSH-Main`，
  AccessMode `write`，覆盖 7 个业务根与 `README.md` / `README.en.md` / `Docs`）。
- 作业期间工作区出现用户本人的未提交改动（`SceneChanger` 移除重试订阅与
  `Respawn()`、`PlayerDamageController` 接管重试复活、`SaveDataManager` 回灌跳过
  `GetDataID()` 为空的固定槽位服务），以及 `Docs/README.md`、
  `Docs/My_ARPG_重构优化清单_已解决.md`、`Docs/My_ARPG_重构优化清单_未解决.md`
  三个跟踪文件被删除。
- 处置：用户确认上述改动均为其本人所做，不回退；三个删除保持删除；
  库内写入照常，共享路径的修复按工作区现状重新落回。

- **更正（同日，撤回一条错误结论）**：本条目初版写的是「作业期间观测到**不入租约
  注册表**的并发写入」并据此断言「租约注册表对不入表的外部写入没有约束力」。该断言
  已撤回，依据如下：
  1. 注册表**不留已释放租约的历史**：每个 agent 只落一个 `<AgentName>_<hash>.txt`，
     `Release` 即删（`WorkingAgent.ps1:490`、`:207`），`Status` 只列活动租约。
     因此两次 `Status` 快照在原理上无法判定「是否有别的写入者曾入表」。
  2. 时间线不支持该推断：`SaveDataManager.cs` 18:15:00、`SceneChanger.cs` 18:15:19、
     `PersistentScene.unity` 18:16:25 均**早于** `WorkingAgent/` 目录的创建时间
     18:19:36；`PlayerDamageController.cs` 为 18:21:13。即三次改动发生在协议载体尚不
     存在时，当时既无业务根、也无 `Concurrency_Guide` 可参与——该事件落在协议的
     操作条件之外，不能用作评价协议有效性的证据。
  3. 同期我**未发现任何第二种 agent 会话的证据**。初版援引的
     `.zcode/plans/plan-sess_a2068891-….md` 是 `2026-08-17` 的旧计划，
     `.idea/workspace.xml` 并不存在。把推断当观测写出，本身是本轮要防的错误。
- 本轮被证实的协议违规只有一处，且在我这一侧：全程**未发送任何 `Heartbeat`**。
  释放时服务端回读本租约为 `heartbeatAgeSeconds: 1528.7`、`suspectedStale: true`；
  按协议准则，这 25 分钟内我才是「疑似陈旧」的一方。
- 仍然成立的规则（与上述更正不冲突）：权威顺序第 2 条「以当前工作区为准」+「发现冲突
  即报告用户、不得静默覆盖」是并发写入的现实兜底。人在自己的 IDE 里直接编辑，按构造
  不会去取租约；这是协议的边界，不是协议失效——判断某次冲突能否归因于协议，必须先确认
  该次冲突是否落在协议的操作条件之内。

## 2026-10-05：模型入口接入（用户拍板）

- 决策：按 `references/configuration-method.md` §7 的两个方案，用户选择**精简入口**形态，
  并选择**两个文件都建**——`AGENTS.md` 与 `CLAUDE.md`（项目根）。
  该节原文要求“先检查现有规则和链接，再让用户选择”，且“未选择前不修改”；本仓库原本
  没有任何入口文件，故两个方案都落在“新建”上，差异只在形态：块内**只放指向根 Router
  的指针**，不内联业务路由表摘要，避免同一事实出现第二个载体。
- 流程失误（记录在案）：初始化时把用户对入口模式的回答「工作流有定义」直接解读为
  `EntryMode=None` 并结案，**跳过了让用户在两个方案中选择这一步**。该选择此后由用户
  明确拍板补上。
- 块格式：用 `<!-- Y_MultipleAgentWorkflow:BEGIN -->` / `…:END -->` 包住，块外内容
  各自独立；两个文件的块内容一致，可按标记幂等更新。skill 未提供导航块模板或标记约定，
  该格式为本工程自定义。
- 落地效果（实测，非推断）：两个文件建立后立即被本机 harness 装载为工作区指令，
  即入口确实生效。此前“DSH 读哪个入口文件未经核验”的悬置项就此关闭。
- 已纳入根 Router 文档索引：`ENTRY-AGENTS`、`ENTRY-CLAUDE` 两行，受结构校验逐条解析；
  入口文件若被删除或改名，校验会失败。
- 未处理：`references/distribution.md:4` 记载适配器来自 Codex / Claude / **ZCode** 三家，
  而 ZCode 读取哪个入口文件本工程无依据可查，故未为它建立入口；需要时另行确认。

## 2026-10-05：写法规范落地与全库重写（用户拍板）

- 用户给出的写法要求：除特别说明外全部按当前状态陈述；不写不存在的与过时的内容；不加变更
  记录、修订补丁与版本说明；不用括号夹注解释；少用否定与转折句式；整体精简；**并且不写
  资产路径**。同轮确认的两项口径：资产只写名称与职责、代码只写类型名与方法名；
  DeveloperLog 保留时间线，不参与本次清理。
- 规范落在 `Workflow\Workflow_Guide.md` 的「文档写法」一节，作为各写手共同遵循的唯一文本，
  同文件另有「写法自查」指向检查器。
- 新增机械检查器 `Workflow\Scripts\Check-DocStyle.ps1`：逐份文档检查资产路径、被禁扩展名、
  行号、来历叙述、修订小节与括号密度，发现 error 时退出码为 1。库内引用与包 ID 先遮蔽再判定。
- 全库重写：10 路写手并行改写 35 份文档——16 份域 Guide、8 份 Router、2 份 Proposal、
  Workflow 域 4 份；DeveloperLog 全部未改。规范同时写入 `Workflow_Guide.md`。
- 检查器自身修掉两次误报：把包 ID `com.unity.*` 当成 `.unity` 扩展名引用；把库内相对路径
  `Assets\Router.md` 当成资产路径。另有大小写不敏感导致把 APK 内部条目前缀 `assets/`
  误判为工程资产目录。
- **发现并修复一处真实缺陷**：`.gitignore` 的 `[Bb]uild/` 规则命中了文档库的 `Build` 业务域，
  该域 4 份文档此前从未纳入版本控制。已追加 `!Y_MultipleAgentWorkflow/Build/` 与
  `!Y_MultipleAgentWorkflow/Build/**` 收回，`git ls-files` 由 0 条转为可跟踪。
- 恢复被过度封禁而丢失的信息：`Build\ProjectConfig_Guide.md` 的 15 条依赖恢复为完整包 ID；
  `Workflow\Project_Validation_Guide.md` 恢复全部命令操作数，并把文档简称与位置对成一张表。
  口径收敛为：只封资产路径与代码路径及行号，工程配置、工具脚本与文档的路径保留，
  验证命令必须能照着执行。
- 校验结果：结构校验 115 pass / 0 warning / 0 error（含 WorkingAgent 回归）；写法检查
  37 份文档 0 error / 15 warning，余下多为章节号与并列枚举；标题数逐文件比对无减少，
  改动只落在措辞与失效内容。
- 未处理：Skill 安装进来的 `Workflow_Configuration_Guide.md` 属于跨项目方法文档，不是本工程
  文档，未参与本轮重写；其正文含其他工程的验证案例，重跑初始化会覆盖该文件。
