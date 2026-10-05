# Assets Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 Assets 域权威文档

**写入的文件**

- `Assets_Guide.md`，ID `ASSETS-GUIDE`，状态 Active
- `EventChannels_Guide.md`，ID `ASSETS-EVENTCHANNELS-GUIDE`，状态 Active
- `Router.md`，重写为项目中文模板，ID `BUS-ASSETS`，下级导航无子类
- `DeveloperLog.md`，本条追加

**依据的证据**

- 代码：配置类型 `ItemSO`、`QuestSO`、`DialogSO`、`RefuseDialogSO`、`CharacterSO`、`GameSceneSO`、`SkillSO`、`PlayerStatsSO`、`GuidSO`、`LocationSO`；事件通道的 12 个类；`OpenTxtWithSystem`；`UIManager`；`SkillManager` 与 `SkillSlot`；`MyEnums`；`PlayerStatsSOTests`
- 资产：GameSO 目录下的 59 个资产、Events 目录下的 28 个通道资产、StreamingAssets 目录下的 GameGuide 文件、StartingMenu 场景里绑定 `OpenTxtWithSystem` 的按钮
- 统计方法：以每个资产的 `m_Script` GUID 对照脚本元数据的 GUID 反查类名后分组；引用计数以资产 GUID 对全工程 428 个场景、预制体与资产文件做 GUID 匹配，排除 GameSO 目录自身

**已核验项**

- GameSO 目录共 59 个资产：配置类 31 个，其中 ItemSO 6、QuestSO 3、DialogSO 8、RefuseDialogSO 3、CharacterSO 3、GameSceneSO 5、SkillSO 2、PlayerStatsSO 1、LocationSO 0；事件类 28 个。
- 事件通道共 12 个类、28 个资产：ToggleCanvasEventSO 10、VoidEventSO 6、InventorySlotsStatsSO 3，其余 9 类各 1。
- 12 个通道类全部直接继承 `ScriptableObject`，无公共基类；泛型参数仅引用 SO 与枚举，零处引用 Manager、Controller 或 View。
- 订阅表达式 49 处 / 22 个文件，发布调用 23 处 / 15 个文件；订阅统一 `OnEnable +=` 与 `OnDisable -=`。
- 10 个 toggle 资产的 `canvasToToggle` 值互不重复，唯一无资产的是枚举成员 `Default`。
- 全工程路径含目录名的非 ASCII 数量 = 0；StreamingAssets 目录仅 GameGuide 文件，1372 字节。
- 全工程 `[CreateAssetMenu]` 21 处；Editor 目录仅 `InputActionReferenceRebuilder`，它只重建输入系统的 `InputActionReference`，与 GameSO 无关。

**维护计数**

`BUS-ASSETS` 维护计数保持 `0/5`；本轮为建立，非维护轮次。
