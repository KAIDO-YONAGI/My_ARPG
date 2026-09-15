# Docs 索引

本目录收纳工程的架构图、构建排障文档与分层重构的结论。

## 分层架构

| 文件 | 内容 |
|---|---|
| `My_ARPG_MVCS项目现状.md` | 已迁移的线、未迁移的域、已确认缺陷、测试现状、单例与执行序现状 |
| `My_ARPG_重构优化清单_未解决.md` | 未完成与待评估项：asmdef、xLua、效果数据驱动、多场景加载、GameJam 成果按需迁移 |
| `My_ARPG_重构优化清单_已解决.md` | 已完成项的归档，含实现与验证记录 |

分层准则本身在个人知识库 `D:\My_Docs\1TODOFiles\Learning\` 的 `MVCS重构方法论.md` 与 `MVCS笔记.md`，这两份不绑定具体工程，不随本仓库分发。

分层重构冻结于 tag `arpg-arch-final`：玩家数值线按 Model / Service / Controller / View 四层组织，其余功能域是原有的 Manager 形态，技能域是保留的验证点。`My_ARPG_重构优化清单_未解决.md` 的条目处于未排期状态。

## 构建与排障

| 文件 | 内容 |
|---|---|
| `UnityAndroidBuildGuide.md` | Android 导出排障：环境自检、代理继承、lint 失败、StreamingAssets 非 ASCII 文件名 |
| `UnityAndroidBuildVerification.md` | Android Release 构建结果存档 |
| `UnityAndroidBuildDiagnostics.skill.md` | 上述排障的可复用技能稿 |

## 其他

| 位置 | 内容 |
|---|---|
| `UML/` | 七张系统架构图：A\* 寻路、玩家、敌人与 NPC、SO 与事件、场景与存读档、对话与背包、UI，每张各有 `.puml` 源码与 `.png` 渲染 |
| `开发日志.txt` | 开发过程记录 |
| `游戏指南.txt` | 玩法说明 |
