using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
//TODO每次进入新场景需要自动备份上个场景的信息
//需要备份的信息包括 敌人状态（位置、血量）。物品状态（位置、数量），序列化到json中，下次进场景加载
//点击备份或者退出游戏的时候额外备份当前玩家所在场景、位置、血量
//点击加载的时候加载玩家的手动存档
//玩家可选加载手动存档进度、继续游戏（加载默认存档）、重新游戏（清空状态、直接加载场景，不依赖存档）


//目前存档和加载太频繁，需要批量处理 done


//DataManager做Data对象，此处做info并且打包、写文件进行存档

public class Save
{
    public SaveInfo saveInfo;
    public Data data;
    public Save(SaveInfo saveInfo, Data data)
    {
        this.saveInfo = saveInfo;
        this.data = data;
    }
}
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem instance;

    [Header("Send")]

    public SceneLoadEventSO loadEventSO;
    [Header("Receive")]

    public DataSaveEventSO dataSavedEvent;
    private void OnEnable()
    {
        dataSavedEvent.DataSaveEvent += OnSaveEvent;
    }
    private void OnDisable()
    {
        dataSavedEvent.DataSaveEvent -= OnSaveEvent;
    }
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    public void OnSaveEvent(MyEnums.SaveType saveType)//通过事件确认存档：以收到的Data保存完成事件为准（带存档类型）
    {
        WriteSave(saveType);
    }
    private void WriteSave(MyEnums.SaveType saveType)
    {
        string saveID = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        SaveInfo saveInfo = new(saveID, saveType);
        Save save = new(saveInfo, DataManager.instance.GetData);

        string json = JsonConvert.SerializeObject(save, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + $"/{saveType}_{saveID}.json", json);

        Debug.Log("SaveRoute: " + Application.persistentDataPath + $"/{saveType}_{saveID}.json");
    }
    public bool LoadSave(MyEnums.SaveType saveType)
    {
        string[] files = Directory.GetFiles(Application.persistentDataPath, $"{saveType}_*.json");
        if (files.Length == 0)
        {
            Debug.LogWarning("No save files found.");
            return false;
        }

        //文件名格式: {saveType}_{yyyyMMdd_HHmmss_fff}.json，按文件名排序最后一个即最新
        Array.Sort(files);
        string latestFile = files[files.Length - 1];

        try
        {
            string json = File.ReadAllText(latestFile);
            Save save = JsonConvert.DeserializeObject<Save>(json);

            DataManager.instance.LoadFromData(save.data);
            //触发加载场景
            GameSceneSO gameScene = GetScene(save.data);
            if (gameScene != null)
            {
                loadEventSO.RaiseLoadRequestEvent
                (
                    gameScene,
                    save.data.sceneIDAndPlayerPos.position.ToVector3(),
                    true
                );
            }
            else
            {
                Debug.Log("Failed Load SceneID");
                return false;
            }

            Debug.Log("LoadRoute: " + latestFile);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load save file {latestFile}: {e.Message}");
        }


        return true;
    }

    private GameSceneSO GetScene(Data data)
    {
        string sceneID = data.sceneIDAndPlayerPos.sceneID;
        foreach (var scene in SceneDataForSave.Instance.gameScenes.ToList())
        {
            if (sceneID == scene.ID) return scene;
        }
        return null;
    }
}
