using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
//TODO每次进入新场景需要自动备份上个场景的信息
//需要备份的信息包括 敌人状态（位置、血量）。物品状态（位置、数量），序列化到json中，下次进场景加载
//点击备份或者退出游戏的时候额外备份当前玩家所在场景、位置、血量
//点击加载的时候加载玩家的手动存档
//玩家可选加载手动存档进度、继续游戏（加载默认存档）、重新游戏（清空状态、直接加载场景，不依赖存档）


//目前存档和加载太频繁，需要批量处理 done
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void InitiateSave(MyEnums.SaveType saveType, Data data)
    {
        string saveID = System.Guid.NewGuid().ToString();
        SaveInfo saveInfo = new(saveID, saveType);
        Save save = new(saveInfo, data);


        Dictionary<string, int> points = new Dictionary<string, int>
        {
            { "James", 9001 },
            { "Jo", 3474 },
            { "Jess", 11926 }
        };

        // 序列化并写入
        string json = JsonConvert.SerializeObject(points, Formatting.Indented);
        File.WriteAllText(Application.persistentDataPath + "/save.json", json);
        Debug.Log("SaveRoute: " + Application.persistentDataPath + "/save.json");
    }
}
