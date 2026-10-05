# Gameplay Router

文档 ID：`BUS-GAMEPLAY`
状态：`Active`
维护计数：`0/5`
最后更新：`2026-10-05`

## 任务线索

| 线索 | 权威文档 |
|---|---|
| `对话`、`Dialog`、`DialogSO`、`RefuseDialogSO`、`对话条件` | `Dialog\Dialog_Guide.md` |
| `任务`、`Quest`、`QuestSO`、`任务板`、`任务面板`、`任务奖励` | `Quest\Quest_Guide.md` |
| `玩家属性`、`Stats`、`等级`、`经验` | `PlayerStats\Router.md` |
| `角色`、`NPC`、`敌人`、`单位` | `Units\Router.md` |
| `背包`、`商店`、`物品`、`金币` | `InventoryShop\Router.md` |
| `技能`、`技能树`、`能力点` | `Skills\Router.md` |

## 下级导航

| 子类 | Router |
|---|---|
| `PlayerStats` | `PlayerStats\Router.md` |
| `Units` | `Units\Router.md` |
| `Dialog` | `Dialog\Router.md` |
| `Quest` | `Quest\Router.md` |
| `InventoryShop` | `InventoryShop\Router.md` |
| `Skills` | `Skills\Router.md` |

## 并发资源

- `workflow:Gameplay`
- `path:Y_MultipleAgentWorkflow\Gameplay\`

## 能力边界

- Active：本 Router 只做线索分派与下级导航；各子域的实现事实一律以对应 Guide 为准。
- Active：`Dialog_Guide.md` 与 `Quest_Guide.md` 已建立，这两个子域的线索直接落到对应 Guide。
- Active：跨域隐性依赖登记在子域 Guide 的「约定与硬边界」中——对话 Refuse 分支读物品史，任务目标读物品史与对话史；改动任一侧的记账口径前需同时查对话域与任务域。
- Proposal：`PlayerStats`、`Units`、`InventoryShop`、`Skills` 四个子域的线索行落在各自 Router，权威内容以其 Guide 为准。
- 需要用户确认：新增或重命名子域 Router、改动下级导航结构，须由用户确认后再动。
