using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[DefaultExecutionOrder(-100)]

//TODO使用流程：实现了IS接口，以loot为例，在特定时刻使用自己接口的注册方法注册自己到DataManager
//DataManager订阅事件，loot脚本特定时刻拉起事件，此时DataManager负责调用loot实现的对应函数，把数据存进去
//加载和存储基本对称

//Loot脚本不需要自己拉起事件，DataManager会订阅场景加载事件，并且调用已注册的loot的对应函数
public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    public Data GetData => dataToSave;
    [Header("Send")]
    public DataSaveEventSO dataSavedEvent;

    [Header("Receive")]

    public SceneLoadEventSO sceneLoadEventSO;
    public VoidEventSO sceneLoadedEvent;

    private List<ISaveable> saveables = new();

    private Data dataToSave = new Data();
    private void OnEnable()
    {
        sceneLoadEventSO.LoadRequestEvent += OnAutoSave;
        sceneLoadedEvent.VoidEvent += OnAutoLoad;
    }
    private void OnDisable()
    {
        sceneLoadEventSO.LoadRequestEvent -= OnAutoSave;
        sceneLoadedEvent.VoidEvent -= OnAutoLoad;
    }
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterSaveableData(ISaveable saveable)
    {
        if (!saveables.Contains(saveable))
        {
            saveables.Add(saveable);
        }
    }

    public void UnRegisterSaveableData(ISaveable saveable)
    {
        saveables.Remove(saveable);
    }
    private void OnAutoSave(GameSceneSO sceneToLoadSO, Vector3 pos, bool isToFade)
    //关于位置，手动存的时候可以用玩家当前位置取代默认位置，自动存档用的是新位置或者场景默认位置
    {
        foreach (var saveable in saveables.ToList())
        {
            saveable.SaveData(dataToSave);
        }
        //场景名、人物位置、任务、背包
        dataToSave.playerStatsData = StatsManager.instance.GetStats();
        //场景类型能区分要不要存档
        if (sceneToLoadSO.sceneType == MyEnums.SceneType.Location)
            dataToSave.sceneIDAndPlayerPos = new(sceneToLoadSO.ID, sceneToLoadSO.initialPosition);
        else return;

        dataSavedEvent.RaiseDataSaveEvent(MyEnums.SaveType.SystemSave);
    }

    void OnAutoLoad()
    {
        foreach (var saveable in saveables.ToList())
        {
            saveable.LoadData(dataToSave);
        }
    }

    public void LoadFromData(Data data)//saveSystem调用
    {
        dataToSave = data;
        foreach (var saveable in saveables.ToList())
        {
            saveable.LoadData(dataToSave);
        }
        StatsManager.instance.LoadStats(data.playerStatsData);
        
    }


}
