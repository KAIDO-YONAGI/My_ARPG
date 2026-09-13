using System;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.Player.Models;


public class SaveData
{
    public Dictionary<string, LootStatus> lootsStatsDic = new();//string是GUID
    public SceneAndPosition sceneIDAndPlayerPos;
    public PlayerStatsData playerStatsData;
}
public class SaveMetaData
{
    public string saveID;//时间戳
    public MyEnums.SaveType saveType;
    public SaveMetaData(string saveID, MyEnums.SaveType saveType)
    {
        this.saveID = saveID;
        this.saveType = saveType;
    }
}


[Serializable]
public class SerializableVector3
{
    public float x, y, z;
    public SerializableVector3() { }
    public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public class LootStatus
{
    public SerializableVector3 position;
    public bool hasBeenPicked;
    // 位置是否偏离「生成态」（场景摆放位置 / 掉落物出生位置）。
    // 只有 true 时 position 才会在读档时回写；旧档没有这个字段，
    // 反序列化后为 false → 位置以场景生成为准，只吃 hasBeenPicked。
    public bool moved;
    public LootStatus() { }
    public LootStatus(Vector3 pos, bool picked, bool moved)
    {
        position = new SerializableVector3(pos);
        hasBeenPicked = picked;
        this.moved = moved;
    }
}

[Serializable]
public class SceneAndPosition
{
    // 存的是 GameSceneSO.SaveKey（Addressables 资产 GUID），不是 GameSceneSO.ID
    public string sceneID;
    public SerializableVector3 position;
    public SceneAndPosition() { }
    public SceneAndPosition(string sceneID, Vector3 pos)
    {
        this.sceneID = sceneID;
        position = new SerializableVector3(pos);
    }
}
