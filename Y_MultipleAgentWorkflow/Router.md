# My_ARPG 多 Agent 工作流路由器

文档 ID：`ROOT-ROUTER`  
状态：`Active`  
最后更新：`2026-10-05`

本目录是本工程的权威文档入口。Agent 可以先做轻量只读盘点，但在详细分析、启动子
Agent、修改文件或使用会改变状态的工具之前，必须先用
`Workflow\Scripts\WorkingAgent.ps1` 取得精确租约。

## 权威顺序

1. 用户最新的明确要求。
2. 实际代码、资源、运行结果与可复现测试。
3. 相关 Guide / Design。
4. 相关业务 Router。
5. DeveloperLog 与 Proposal。
6. 模型入口文件与 Skill。

当第 2 项与本目录文档冲突时，以当前工作区的代码与资源为准，并立即修正文档。

## 业务路由

| 业务根 | Router |
|---|---|
| `Architecture` | `Architecture\Router.md` |
| `Assets` | `Assets\Router.md` |
| `Build` | `Build\Router.md` |
| `Data` | `Data\Router.md` |
| `Gameplay` | `Gameplay\Router.md` |
| `SceneFlow` | `SceneFlow\Router.md` |
| `Workflow` | `Workflow\Router.md` |

## 必读起点

1. 先经本文件路由。
2. 读 `Workflow\Concurrency_Guide.md`。
3. 用 `Workflow\Scripts\WorkingAgent.ps1` 取得精确租约。
4. 若租约状态显示重叠任务，且客户端支持原生 Agent 通信（如 Codex 任务消息），
   编辑前先通知该任务。
5. 只读被路由到的 Guide/Design、Router 和必要证据。
6. 成功、失败或取消都要释放租约；客户端支持原生通信时，释放后再通知等待中的任务。

## 项目验证

项目专属验证统一走 `Workflow\Project_Validation_Guide.md`。该指南只登记本工程已确认的
命令与状态变更；未确认的打成空白条目，不凭其他工程推断。

## 模型入口

`AGENTS.md` 与 `CLAUDE.md`（项目根）各含一个由本工作流维护的导航块，用
`<!-- Y_MultipleAgentWorkflow:BEGIN -->` / `<!-- Y_MultipleAgentWorkflow:END -->`
标记包住，两块内容一致，可用脚本按标记幂等更新。本机实测：两个文件都会被装载为
工作区指令。

约定：**块内只放指向本文件的指针**，业务事实一律留在本目录，避免同一事实出现第二个
载体。入口文件的选择（`EntryMode`）由用户拍板，不是安装 Skill 的默认行为。

## 全局并发资源

同一工作树上的多个 Agent 共用以下资源，写入前必须在租约里声明：

| 资源 | 说明 |
|---|---|
| `git:index` | 共享暂存区；`push_to_github.bat` 会 `git add -A` 并推 master，并发期误提交风险高 |
| `runtime:UnityEditor` | 本机同时只应有一个编辑器实例；`Temp\UnityLockfile` 是陈旧锁，不代表实例存活 |
| `path:Packages/manifest.json` | 依赖全局单点，且当前工作区已有未提交改动 |
| `path:Packages/packages-lock.json` | 与 manifest 成对变更 |
| `path:ProjectSettings` | Unity 版本、构建场景列表、脚本后端等工程级配置 |
| `path:Assets/Scripts` | 产品代码；0 个 asmdef，任一改动触发全量重编译 |
| `path:Assets/GameSO` | 配置与事件通道资产；资产改名会让 Inspector 接线静默失效 |
| `path:Assets/Scenes` | 多场景组叠加加载；`PersistentScene` 与 `Scene1`/`Scene2` 是热点文件 |
| `path:Assets/Prefabs` | 与场景接线耦合 |
| `path:Builds`、`path:Logs` | 构建与测试证据输出，已被 gitignore |
| `graph:.codegraph` | SQLite WAL 索引，重建需独占 |
| `workflow:root` | 仅在变更根结构时使用 |

## 文档索引

| 文档 ID | 路径 | 状态 |
|---|---|---|
| `ROOT-ROUTER` | `Router.md` | Active |
| `ENTRY-AGENTS` | `AGENTS.md` | Active |
| `ENTRY-CLAUDE` | `CLAUDE.md` | Active |
| `WF-CONFIG-METHOD` | `Workflow_Configuration_Guide.md` | Active |
| `WF-ROUTER` | `Workflow\Router.md` | Active |
| `WF-PROJECT-VALIDATION` | `Workflow\Project_Validation_Guide.md` | Active |
| `WF-OPEN-DECISIONS` | `Workflow\OpenDecisions_Proposal.md` | Proposal |
| `BUS-ARCHITECTURE` | `Architecture\Router.md` | Active |
| `BUS-ARCHITECTURE-LAYERING` | `Architecture\Layering\Router.md` | Active |
| `ARCH-LAYERING-GUIDE` | `Architecture\Layering\Layering_Guide.md` | Active |
| `BUS-ARCHITECTURE-COMPOSITION` | `Architecture\Composition\Router.md` | Active |
| `ARCH-COMPOSITION-GUIDE` | `Architecture\Composition\Composition_Guide.md` | Active |
| `BUS-ARCHITECTURE-ASSEMBLYPLAN` | `Architecture\AssemblyPlan\Router.md` | Active |
| `ARCH-ASSEMBLY-PLAN` | `Architecture\AssemblyPlan\AssemblyPlan_Proposal.md` | Proposal |
| `BUS-GAMEPLAY` | `Gameplay\Router.md` | Active |
| `BUS-GAMEPLAY-PLAYERSTATS` | `Gameplay\PlayerStats\Router.md` | Active |
| `GP-PLAYERSTATS-GUIDE` | `Gameplay\PlayerStats\PlayerStats_Guide.md` | Active |
| `BUS-GAMEPLAY-UNITS` | `Gameplay\Units\Router.md` | Active |
| `GP-UNITS-GUIDE` | `Gameplay\Units\Units_Guide.md` | Active |
| `BUS-GAMEPLAY-DIALOG` | `Gameplay\Dialog\Router.md` | Active |
| `GP-DIALOG-GUIDE` | `Gameplay\Dialog\Dialog_Guide.md` | Active |
| `BUS-GAMEPLAY-QUEST` | `Gameplay\Quest\Router.md` | Active |
| `GP-QUEST-GUIDE` | `Gameplay\Quest\Quest_Guide.md` | Active |
| `BUS-GAMEPLAY-INVENTORYSHOP` | `Gameplay\InventoryShop\Router.md` | Active |
| `GP-INVENTORYSHOP-GUIDE` | `Gameplay\InventoryShop\InventoryShop_Guide.md` | Active |
| `BUS-GAMEPLAY-SKILLS` | `Gameplay\Skills\Router.md` | Active |
| `GP-SKILLS-GUIDE` | `Gameplay\Skills\Skills_Guide.md` | Active |
| `BUS-SCENEFLOW` | `SceneFlow\Router.md` | Active |
| `SCENE-FLOW-GUIDE` | `SceneFlow\SceneFlow_Guide.md` | Active |
| `BUS-ASSETS` | `Assets\Router.md` | Active |
| `ASSETS-GUIDE` | `Assets\Assets_Guide.md` | Active |
| `ASSETS-EVENTCHANNELS-GUIDE` | `Assets\EventChannels_Guide.md` | Active |
| `BUS-DATA` | `Data\Router.md` | Active |
| `DATA-SAVEDATA-GUIDE` | `Data\SaveData_Guide.md` | Active |
| `BUS-BUILD` | `Build\Router.md` | Active |
| `BUILD-ANDROID-GUIDE` | `Build\AndroidBuild_Guide.md` | Active |
| `BUILD-PROJECTCONFIG-GUIDE` | `Build\ProjectConfig_Guide.md` | Active |
| `BUILD-TESTBASELINE-GUIDE` | `Build\TestBaseline_Guide.md` | Active |
| `REF-MVCS-STATUS` | `Docs\My_ARPG_MVCS项目现状.md` | Reference |
| `REF-ANDROID-GUIDE` | `Docs\UnityAndroidBuildGuide.md` | Reference |
| `REF-ANDROID-VERIFY` | `Docs\UnityAndroidBuildVerification.md` | Reference |
| `REF-ANDROID-SKILL` | `Docs\UnityAndroidBuildDiagnostics.skill.md` | Reference |
| `REF-DEVLOG` | `Docs\开发日志.txt` | Archived |
| `REF-GAMEGUIDE` | `Assets\StreamingAssets\GameGuide.txt` | Active |
| `REF-GAMEGUIDE-DUPLICATE` | `Docs\游戏指南.txt` | Duplicate（待收敛） |

## 维护

成功且实质影响某个业务根的任务，会让该业务根的计数 +1。到 `5/5` 时，对照任务证据与
工程实际状态复核，更新权威文档或记录 `reviewed-no-change`，然后清零。

不要自动截断 `DeveloperLog.md` 历史；只有用户明确确认某条目冗余或过时后才能删除。
