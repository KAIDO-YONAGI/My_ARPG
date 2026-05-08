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

    List<ISaveable> saveables = new List<ISaveable>();

    Data dataToSave;

    [Header("Receive")]
    public VoidEventSO saveDataEvent;
    public VoidEventSO loadDataEvent;
    public SceneLoadEventSO sceneLoadEventSO;
    public VoidEventSO sceneLoadedEvent;

    private void OnEnable()
    {
        saveDataEvent.VoidEvent += Save;
        loadDataEvent.VoidEvent += Load;
        sceneLoadEventSO.LoadRequestEvent += OnSave;
        sceneLoadedEvent.VoidEvent += Load;

    }



    private void OnDisable()
    {
        saveDataEvent.VoidEvent -= Save;
        loadDataEvent.VoidEvent -= Load;
        sceneLoadEventSO.LoadRequestEvent -= OnSave;
        sceneLoadedEvent.VoidEvent -= Load;
    }
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        dataToSave = new Data();
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
    void Save()
    {
        foreach (var saveable in saveables.ToList())
        {
            saveable.SaveData(dataToSave);
        }
    }
    void Load()
    {
        foreach (var saveable in saveables.ToList())
        {
            saveable.LoadData(dataToSave);
        }
    }

    private void OnSave(GameSceneSO arg0, Vector3 arg1, bool arg2)
    {
        Save();

    }
}
