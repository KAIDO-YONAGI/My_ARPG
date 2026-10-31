# Architecture.Composition Developer Log

Record completed task evidence, affected files/resources, validation, conclusions, and
maintenance count changes here.

## 2026-10-05 建立 组合与初始化时序 权威文档

- 任务：为 Architecture.Composition 建立权威 Guide，覆盖单例清单与预算、初始化时序三套手写解法、唯一执行序属性、`SaveRegistry` 登记处契约、UI 画布焦点栈契约。
- 写入的文件：
  - `Composition_Guide.md`，文档 ID `ARCH-COMPOSITION-GUIDE`，状态 Active。
  - `Router.md`，按中文模板重写。
  - `DeveloperLog.md`，本条目。
- 依据的证据：
  - 契约类型：`YSingleton`、`SaveableService`、`SaveRegistry`、`ISaveable`、`ICanvasManager`、`MyEnums`。
  - 管线类型：`TimeManager`、`InitialLoad`、`SceneChanger`、`SceneDataForSave`、`CameraPixelSnap`、`PersistentSceneRegistry`、`UIManager`、`CanvasFocusStack`、`IntegratedUICanvasManager`。
  - 玩法类型：`StatsService`、`SkillTreeManager`、`SaveSystem`、`SaveDataManager`、`SaveLoadCanvasManager`、`Loot`。
  - 测试类型：`CanvasFocusStackTests`。
- 本轮核对的静态事实：
  - 全工程单例实数：18 个类型直接继承 `YSingleton<T>`，`StatsService` 经抽象基类 `SaveableService<TSelf>` 间接继承，具体单例共 19 个，抽象派生只有 `SaveableService` 一个。
  - `DefaultExecutionOrder` 全工程仅 1 处，位于 `CameraPixelSnap`；工程设置中没有脚本执行顺序配置。
  - `RuntimeInitializeOnLoadMethod` 仅 2 处，位于 `SaveRegistry` 与 `PersistentSceneRegistry`，均为 `SubsystemRegistration`。
  - `OnSingletonInitialized` 重写点 6 处；18 个单例派生类均未重写 `Awake`。
  - `ICanvasManager` 实现者 10 个；全工程 asmdef 数量为 0。
- 维护计数：`0/5 -> 1/5`


## 2026-10-31 属性面板预制体与闲置缓存

记录 UIManager 的双注册入口、CachedUIView 打开/关闭生命周期、闲置 LRU 与切场清理；Stats 为首个预制体样例，无新增单例。

Unity 重编译及预制体引用检查完成；未运行测试或进入 Play 模式，复用与多界面淘汰仍待运行时验证。维护计数更新为 `1/5`。
