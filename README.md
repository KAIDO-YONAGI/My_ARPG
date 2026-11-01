# My_ARPG

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3c1-222222?logo=unity&logoColor=white) ![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)

**简体中文** | [English](README.en.md)

基于 Unity 与 C# 开发的 2D 俯视角 ARPG 原型，包含近战与远程战斗、角色成长、NPC 对话、任务、背包、商店和技能树。

项目演示：[ARPG Demo 项目展示](https://www.bilibili.com/video/BV1sCGH6iErJ/)

## 项目内容

| 功能 | 内容 |
|---|---|
| 探索与战斗 | 俯视角移动、近战与远程攻击切换、受击与击退、死亡重试和跨场景探索 |
| 敌人与 NPC | 敌人的待机、追击与攻击状态切换，NPC 的巡逻、游荡与对话交互 |
| 角色成长 | 经验与升级、属性面板、技能点分配、技能等级与前置依赖解锁 |
| 对话与任务 | 分支对话、前置条件与交互历史，支持收集物品和与角色对话的任务目标、进度检查与奖励发放 |
| 背包与商店 | 物品拾取、堆叠、使用与丢弃，金币结算、商品分类与买卖 |

## 技术实现

### 场景与系统协作

采用场景组叠加加载与常驻场景管理，公共系统随常驻场景保留，关卡内容按组切换。加载流程对外提供请求与完成事件，各系统可在切场前后处理自身状态。

对话、任务、物品和技能通过 ScriptableObject 配置。奖励发放、界面开关等交互使用事件通道传递，减少系统间的直接引用。

### 角色数值与界面分离

角色数值采用 MVCS 分层：Model 处理数值与升级规则，Service 统一处理写操作并提供只读接口，Controller 与 View 负责显示。对话、任务、背包、商店和技能树将业务状态与界面显示分开管理，面板关闭或重新创建时，业务状态仍由对应系统持有。

### 敌人与 NPC 寻路

自写 A* 寻路，将网格构建、路径搜索和路径跟随分开，供敌人与 NPC 复用。支持八方向移动、对角障碍检查，以及目标移动后的重新寻路。

开放列表采用二叉小顶堆，搜索集合跨次复用以减少分配。路径跟随可配合敌人追击、NPC 巡逻和游荡等行为使用。

### 界面交互与对象复用

界面按需创建，关闭后进入缓存，按 LRU 顺序和超时策略回收。视图通过脏标记刷新显示，焦点栈管理窗口层级，面板支持拖动。

箭矢与掉落物使用泛型对象池复用。EditMode 测试覆盖角色数值规则、寻路开放列表、对象池、UI 焦点栈与界面缓存。

## 使用入口

依赖配置、运行入口、键位操作与构建说明见[运行与操作指南](Docs/GettingStarted.md)，Android 构建问题见 [Android 构建与排障](Docs/UnityAndroidBuildGuide.md)。

## 文档导航

架构与各系统设计见[项目文档索引](Y_MultipleAgentWorkflow/Router.md)。

文档路由与多 Agent 并发协作采用我开发并维护的 [Y_MultipleAgentWorkflow](https://github.com/KAIDO-YONAGI/Y_MultipleAgentWorkflow)。

## 致谢与许可

美术资源来自 Pixel Frog 的 [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords)，按资源包许可使用。项目代码采用 [GPL-3.0](LICENSE) 许可。
