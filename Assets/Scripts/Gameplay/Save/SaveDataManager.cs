using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Gameplay.Player.Services;
using Gameplay.Player;

//TODO使用流程：实现了IS接口的类，以loot为例，在特定时刻使用自己接口的注册方法注册自己到 SaveDataManager
//SaveDataManager 订阅事件，loot 脚本特定时刻拉起事件，此时 SaveDataManager 负责调用 loot 实现的对应函数，把数据存进去
//加载和存储基本对称

//Loot 脚本不需要自己拉起事件，SaveDataManager 会订阅场景加载事件，并且调用已注册的loot的对应函数
public class SaveDataManager : YSingleton<SaveDataManager>
{

    public SaveData GetData => dataToSave;
    [Header("Send")]
    [SerializeField] private DataSaveEventSO dataSavedEvent;

    [Header("Receive")]
    [SerializeField] private SceneLoadEventSO sceneLoadEventSO;
    [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

    // 存档注册表位于 Contracts/SaveRegistry.cs，本类只做收集与分发。

    private SaveData dataToSave = new SaveData();
    // 首次从主菜单进入游戏时不应写系统档，否则场景信息还没准备好。
    private MyEnums.SceneType lastSceneType = MyEnums.SceneType.Menu;
    private void OnEnable()
    {
        sceneLoadEventSO.LoadRequestEvent += OnAutoSave;
        sceneLoadedEvent.SceneLoadedEvent += OnAutoLoad;
    }
    private void OnDisable()
    {
        sceneLoadEventSO.LoadRequestEvent -= OnAutoSave;
        sceneLoadedEvent.SceneLoadedEvent -= OnAutoLoad;
    }

    public void RemoveLootRegistration(string lootId)
    {
        if (string.IsNullOrEmpty(lootId) || dataToSave == null || dataToSave.lootsStatsDic == null)
        {
            return;
        }

        dataToSave.lootsStatsDic.Remove(lootId);
    }
    public bool PrepareManualSaveData()
    {
        GameSceneSO currentScene = SceneChanger.Instance != null ? SceneChanger.Instance.GetCurrentGameScene() : null;
        if (currentScene == null || StatsService.Instance == null)
        {
            return false;
        }

        dataToSave ??= new SaveData();
        dataToSave.lootsStatsDic ??= new Dictionary<string, LootStatus>();

        Vector3 savePosition = PlayerLocator.Instance != null
            ? PlayerLocator.Instance.GetPosition()
            : currentScene.initialPosition;
        dataToSave.sceneIDAndPlayerPos = new SceneAndPosition(currentScene.SaveKey, savePosition);

        foreach (var saveable in SaveRegistry.All.ToList())
        {
            saveable.SaveData(dataToSave);
        }

        return true;
    }
    private void OnAutoSave(List<GameSceneSO> scenesToLoadSO, Vector3 pos, bool isToFade)
    //关于位置，手动存的时候可以用玩家当前位置取代默认位置，自动存档用的是新位置或者场景默认位置
    {
        // 场景组语义：存档键跟随首个内容场景（组内第一个 Location/Menu 场景 SO）。
        GameSceneSO sceneToLoadSO = scenesToLoadSO != null && scenesToLoadSO.Count > 0 ? scenesToLoadSO[0] : null;
        if (sceneToLoadSO == null)
        {
            return;
        }

        foreach (var saveable in SaveRegistry.All.ToList())
        {
            saveable.SaveData(dataToSave);
        }

        //TODO任务、物品栏、背包。另外，重新开始的时候要删除存下的动态数据

        bool isLoadingSaveRequest = SaveSystem.Instance.IsLoadingSaveRequest;
        if (!isLoadingSaveRequest
            && lastSceneType == MyEnums.SceneType.Menu
            && sceneToLoadSO.sceneType == MyEnums.SceneType.Location)
        {
            Vector3 savePosition = pos == Vector3.zero ? sceneToLoadSO.initialPosition : pos;
            dataToSave = new SaveData();
            StatsService.Instance.SaveData(dataToSave); // 存档对象重建之后，跨场景的持久数据要重新写入
            dataToSave.sceneIDAndPlayerPos = new(sceneToLoadSO.SaveKey, savePosition);
            DynamicDataHandler.ClearDynamicData(dataToSave);
        }
        //上个场景是Menu则不存档
        if (lastSceneType != MyEnums.SceneType.Menu)
        {
            var sceneChanger = SceneChanger.Instance;
            // 切到新 Location 时沿用目标场景和入口点，退回 Menu 时则记录当前游玩场景。
            GameSceneSO saveScene = sceneToLoadSO.sceneType == MyEnums.SceneType.Location
                ? sceneToLoadSO
                : sceneChanger != null ? sceneChanger.GetCurrentGameScene() : null;
            Vector3 savePosition = sceneToLoadSO.sceneType == MyEnums.SceneType.Location
                ? sceneToLoadSO.initialPosition
                : PlayerLocator.Instance != null ? PlayerLocator.Instance.GetPosition() : Vector3.zero;

            if (saveScene != null)
            {
                dataToSave.sceneIDAndPlayerPos = new(saveScene.SaveKey, savePosition);
                dataSavedEvent.RaiseDataSaveEvent(MyEnums.SaveType.SystemSave);
            }
        }
        lastSceneType = sceneToLoadSO.sceneType;
    }

    void OnAutoLoad(GameSceneSO _)
    {
        foreach (var saveable in SaveRegistry.All.ToList())
        {
            // 只回灌按 GUID 参与动态数据的场景物体（如 Loot）：它们随场景重新实例化，
            // 必须按存档恢复拾取态与位移，回灌是必需的。
            // GetDataID() 为 null 的是固定槽位服务（SaveableService 派生，如 StatsService），
            // 状态跨场景持久，读档已由 LoadFromData 应用；在这里回灌只会把广播段抓的旧快照
            // 覆盖回运行时，静默撤销加载窗口内的状态变更（例如重试复活回血）。
            if (saveable.GetDataID() == null)
                continue;

            saveable.LoadData(dataToSave);
        }
    }

    public void LoadFromData(SaveData data)//saveSystem调用
    {
        // 兜底防御，避免其他读取入口把空数据直接塞进运行态。
        if (data == null)
        {
            Debug.LogWarning("Skip load because save data is null.");
            return;
        }
        dataToSave = data;
        foreach (var saveable in SaveRegistry.All.ToList())//拷贝一份再操作，防止耗时操作中发生时序竞态修改
        {
            saveable.LoadData(dataToSave);
        }
    }


}
