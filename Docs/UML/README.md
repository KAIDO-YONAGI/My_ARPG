# 类图索引

本目录存放 My_ARPG 的 PlantUML 类图与渲染结果。

## 目录结构

| 路径 | 内容 |
| --- | --- |
| `v2_2026-10/Docs/uml_N_topic.puml` | 与当前 `Assets/Scripts` 代码及 `Y_MultipleAgentWorkflow` 文档一致的一套图，14 个源文件 |
| `v2_2026-10/Topic.png` | 上表每个源文件对应的渲染结果，文件名取自 `@startuml` 后的图名 |
| `v1_2026-05/Docs/01..07_*.puml` | 2026-05 的一套图，源文件与渲染结果原样保留，不随代码更新 |
| `v1_2026-05/*.png` | v1 的渲染结果 |
| `plantuml.jar` | PlantUML 1.2025.3，与 `C:\Users\12248\bin\plantuml.jar` 字节一致 |

`v1_2026-05/` 是冻结档案，只读不改；新图一律落在 `v2_2026-10/`。

## v2 图一览

| 源文件 | 图名 | 覆盖范围 |
| --- | --- | --- |
| `uml_1_astar_pathfinding.puml` | `AStar_Pathfinding` | 网格层、算法层、跟随层与调用方 |
| `uml_2_player_system.puml` | `Player_System` | 玩家移动状态机、近战、弓箭、武器切换、投射物与对象池 |
| `uml_3_playerstats_mvcs.puml` | `PlayerStats_MVCS` | 玩家数值的 Model、View、Controller、Service 四层与经验线 |
| `uml_4_enemy_npc_system.puml` | `Enemy_NPC_System` | 敌人、NPC、店主三支单位的行为组件 |
| `uml_5_config_assets.puml` | `Config_Assets` | 角色、物品、技能、场景、对话、任务六类配置资产与序列化结构 |
| `uml_6_event_channels.puml` | `Event_Channels` | 12 个事件通道资产、通道引用的枚举与类型 |
| `uml_7_scene_flow.puml` | `Scene_Flow` | 常驻场景注册、切换执行段、触发点与加载完成后的订阅方 |
| `uml_8_save_load_data.puml` | `SaveLoad_Data` | 存档身份、落盘结构、读写编排、存档参与者与保存面板 |
| `uml_9_dialog_system.puml` | `Dialog_System` | 会话推进、拒绝策略、历史记录与 NPC 触发链 |
| `uml_10_inventory_shop.puml` | `Inventory_Shop` | 背包槽位、掉落物池、商店槽位与买卖结算 |
| `uml_11_quest_system.puml` | `Quest_System` | 任务状态机、进度字典、奖励发放与任务板 |
| `uml_12_skills_system.puml` | `Skills_System` | 技能槽位、技能点经济、技能树与技能效果派发 |
| `uml_13_ui_system.puml` | `UI_System` | 画布焦点栈、输入绑定、10 个画布管理器与窗口控件 |
| `uml_14_architecture_contracts.puml` | `Architecture_Contracts` | 三层划分、跨域契约与初始化次序 |

## v1 到 v2 的落点

| v1 文件 | v2 图 |
| --- | --- |
| `01_AStar_Pathfinding` | `AStar_Pathfinding` |
| `02_Player_System` | `Player_System`、`PlayerStats_MVCS` |
| `03_Enemy_NPC_System` | `Enemy_NPC_System` |
| `04_ScriptableObjects_and_Events` | `Config_Assets`、`Event_Channels` |
| `05_Scene_SaveLoad_System` | `Scene_Flow`、`SaveLoad_Data` |
| `06_Dialog_Inventory_System` | `Dialog_System`、`Inventory_Shop` |
| `07_UI_System` | `UI_System` |

`Quest_System`、`Skills_System`、`Architecture_Contracts` 三张图在 v2 补齐。

## 渲染

优先用 skill 的脚本，它按目录差异判定真实产出：

```powershell
python "C:\Users\12248\.agents\skills\plantuml-class-diagram\scripts\render_puml.py" "D:\Unity\Projects\My_ARPG\Docs\UML\v2_2026-10\Docs" -o "D:\Unity\Projects\My_ARPG\Docs\UML\v2_2026-10"
```

也可以直接调 jar，在 `v2_2026-10\Docs` 目录下执行：

```powershell
java "-Dfile.encoding=UTF-8" "-DPLANTUML_LIMIT_SIZE=16384" "-Djava.awt.fonts=C:\Windows\Fonts" -jar plantuml.jar -tpng -charset UTF-8 "*.puml" -o "D:\Unity\Projects\My_ARPG\Docs\UML\v2_2026-10"
```

`PLANTUML_LIMIT_SIZE` 要放开到 16384；默认的 4096 会把大图裁掉且不报错，判断方法是检查 PNG 任一边是否恰好等于 4096。`-o` 用绝对路径。pwsh 里 `-D...` 参数要逐个加引号，否则会被拆坏导致 `ClassNotFoundException`。

`!theme plain` 把类名、成员行与构造型的字体固定成 Verdana，Verdana 没有中文字形，这三处的中文会渲染成豆腐块。每个源文件都在主题之后补了 `skinparam Class`、`skinparam ClassAttribute`、`skinparam ClassStereotype` 三段 `FontName "Microsoft YaHei"`，套用主题时这三段不能省。

## 图内约定

| 记号 | 含义 |
| --- | --- |
| `+` | 公开成员与 Inspector 序列化字段 |
| `-` | 私有成员 |
| `#` | protected 成员 |
| `{static}` | 静态成员 |
| `..|>` | 实现接口 |
| `--|>` | 继承基类 |
| `*--` | 组合 |
| `o--` | 聚合 |
| `..>` | 依赖 |

类型角色用构造型标注：`<<单例>>`、`<<静态类>>`、`<<事件通道>>`、`<<配置资产>>`、`<<可序列化>>`、`<<纯 C#>>`、`<<对象池>>`、`<<存档传输格式>>`、`<<Unity 引擎>>`。

排版约定：包不带填充色；包与包之间用一条带标签的虚线主干串起阅读顺序，包内才画类与类的关系；成员签名只保留必要的形参名，同类形参合并成 `a, b : Type` 一行；长解释全部收进图例（`legend`）块，图内只留结构与短标签，图例说明规则而不是逐条复述代码。每个源文件只有一条图例，它同时承载 `+/-` 记号说明与规则说明。

走线约定：一个类向另一个包里的多个类各拉一条虚线时，合并成一条包间线 `"包A" ..> "包B" : 短标签`，标签不超过 6 个汉字，被合并掉的接线明细改成一句或一段话写进图例；继承、实现、组合、聚合以及同一个包内部的关系保持逐条绘制，不参与合并。跨包散线的条数因此控制在一张图十条以内，图里每根线都能指认出它连的是哪两个包。

关系线上的中文是调用语义，图内文字只描述当前状态，字段名与代码标识符保持原文。
