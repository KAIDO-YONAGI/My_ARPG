# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-000000?logo=unity)
![C#](https://img.shields.io/badge/C%23-512BD4?logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/License-GPL--3.0-blue)

基于 Unity 与 C# 开发的 2D 俯视角 ARPG 原型，围绕探索战斗、角色成长和 NPC 交互，串联对话、任务、背包、商店与技能树。

项目重点是玩法系统之间的协作，以及场景组织、寻路、UI 生命周期管理等游戏客户端工程实践。

[English](README.en.md) · [运行与操作指南](Docs/GettingStarted.md)

## 项目演示

[【Unity】28届 ARPG Demo项目展示](https://www.bilibili.com/video/BV1sCGH6iErJ/) · Bilibili，项目早期演示。

## 玩法与系统

| 系统 | 项目内容 |
|---|---|
| 探索与战斗 | 俯视角移动、近战挥砍与远程弓箭切换、受击与击退、死亡重试、跨场景探索 |
| 敌人与 NPC | 敌人待机、追击与攻击状态切换；NPC 巡逻、游荡与对话交互 |
| 角色成长 | 经验与升级、属性面板、技能点分配、技能等级与前置依赖解锁 |
| 对话与任务 | 分支对话、前置条件与交互历史；物品收集和角色对话目标、任务状态推进与奖励 |
| 背包与商店 | 物品拾取、堆叠、使用与丢弃，金币结算、商品分类与买卖 |

## 技术实现

- **场景组织与系统协作**：采用场景组叠加加载与常驻场景管理，将公共系统与关卡内容分开组织；对外暴露加载请求与完成事件，便于各系统接入切场流程并协作。
- **配置与事件通信**：使用 ScriptableObject 配置对话、任务、物品与技能，通过事件通道连接任务奖励、界面开关等交互，减少系统间的直接引用，便于独立维护与扩展。
- **自写 A\* 寻路**：将网格构建、路径搜索与路径跟随拆分，供敌人和 NPC 复用。支持八方向移动、对角障碍检查与动态重寻路，采用二叉小顶堆管理开放列表，复用搜索集合。
- **UI 按需创建与缓存**：面板按需实例化，关闭后缓存，按 LRU 顺序与超时策略回收；业务状态独立于界面生命周期，通过脏标记刷新显示。焦点栈统一处理窗口排序，面板支持拖动。
- **业务与显示分离**：玩家数值采用 MVCS 分层，集中处理数值规则与写操作，对外提供只读接口；对话、任务、背包、商店和技能将业务管理与界面显示分开，便于分别维护。
- **对象复用与回归测试**：箭矢和掉落物使用泛型对象池；通过 EditMode 测试验证玩家数值规则、寻路开放列表、对象池、UI 焦点栈与界面缓存。

## 文档与协作

- [运行、操作与构建](Docs/GettingStarted.md)
- [Android 构建与排障](Docs/UnityAndroidBuildGuide.md)
- [架构与各系统文档索引](Y_MultipleAgentWorkflow/Router.md)

工程使用作者自研的 [多 Agent 协作工作流](https://github.com/KAIDO-YONAGI/Y_MultipleAgentWorkflow)，通过文档路由与并发租约组织项目维护。

## 资源与许可

- 美术资源：[Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords)，作者 Pixel Frog，按资源包许可使用。
- 代码许可：[GPL-3.0](LICENSE)。
