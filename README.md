# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-222222?logo=unity&logoColor=white) ![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)

**简体中文** | [English](README.en.md)

我基于 Unity 与 C# 开发了这款 2D 俯视角 ARPG 原型。我围绕场景探索与战斗设计 NPC 对话、任务、背包、商店和技能树，让这些系统共同支撑角色成长。

我将战斗、成长和交互系统串联起来，并围绕场景加载、寻路、数据配置、存档与 UI 生命周期组织客户端逻辑。

项目演示：[ARPG Demo 项目展示](https://www.bilibili.com/video/BV1sCGH6iErJ/)

## 项目内容

| 功能 | 内容 |
|---|---|
| 探索与战斗 | 我实现俯视角移动、近战与远程攻击切换、受击与击退、死亡重试和跨场景探索 |
| 敌人与 NPC | 我为敌人实现待机、追击与攻击状态切换，为 NPC 实现巡逻、游荡与对话交互 |
| 角色成长 | 我实现经验与升级、属性面板、技能点分配、技能等级与前置依赖解锁 |
| 对话与任务 | 我设计分支对话、前置条件与交互历史，并将物品收集和角色对话接入任务目标、推进与奖励 |
| 背包与商店 | 我实现物品拾取、堆叠、使用与丢弃，以及金币结算、商品分类与买卖 |
| 存档与读档 | 我支持手动存档与场景切换自动存档，并恢复角色属性、场景位置和场景掉落物状态 |

### 角色成长与玩法协作

我用 MVCS 分层集中管理角色数值规则与写操作，并提供只读接口。对话、任务、背包、商店与技能树分别管理业务状态和界面显示，使成长与交互逻辑独立于面板的创建和关闭。

我通过 ScriptableObject 配置对话、任务、物品与技能，再用事件通道连接奖励发放、界面开关等交互。我将场景组叠加加载，让常驻场景承载公共系统，并通过加载请求与完成事件协调场景切换。

### 敌人与 NPC 寻路

我实现了 A* 寻路，并将网格构建、路径搜索和路径跟随拆开，供敌人与 NPC 复用。搜索支持八方向移动与对角障碍检查，角色移动过程中可以动态重新寻路。

我用二叉小顶堆管理开放列表，并复用搜索集合以减少重复搜索时的分配。路径跟随与敌人状态、NPC 巡逻和游荡行为配合，支撑追击与自主移动。

### 界面交互与对象复用

我按需创建面板，并在关闭后缓存，再按 LRU 顺序和超时策略回收。我让系统独立维护业务状态，通过脏标记刷新界面，并用焦点栈管理窗口顺序与拖动。

我用泛型对象池复用箭矢与掉落物。EditMode 测试覆盖角色数值规则、寻路开放列表、对象池、UI 焦点栈与界面缓存。

## 使用入口

依赖配置、运行入口、键位操作与构建说明见[运行与操作指南](Docs/GettingStarted.md)。移动端构建见 [Android 构建与排障](Docs/UnityAndroidBuildGuide.md)。

## 文档导航

| 文档 | 内容 |
|---|---|
| [项目系统索引](Y_MultipleAgentWorkflow/Router.md) | 按架构、玩法、场景、数据与构建查阅系统文档 |
| [分层与读写边界](Y_MultipleAgentWorkflow/Architecture/Layering/Layering_Guide.md) | 玩家数值分层、规则与状态读写边界 |
| [初始化与 UI 管理](Y_MultipleAgentWorkflow/Architecture/Composition/Composition_Guide.md) | 系统初始化、窗口焦点与界面缓存 |
| [事件通道](Y_MultipleAgentWorkflow/Assets/EventChannels_Guide.md) | ScriptableObject 配置与系统间事件通信 |
| [存档系统](Y_MultipleAgentWorkflow/Data/SaveData_Guide.md) | 存档数据、保存与恢复流程 |

我开发并维护了多智能体协作工作流：[Y_MultipleAgentWorkflow](https://github.com/KAIDO-YONAGI/Y_MultipleAgentWorkflow)。

## 致谢与许可

我使用了 Pixel Frog 的 [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) 美术资源，并遵循资源包许可。我将项目代码按 [GPL-3.0](LICENSE) 授权。
