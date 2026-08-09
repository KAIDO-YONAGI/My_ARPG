using System;
using System.Collections.Generic;
using UnityEngine;


public class Data
{
    public Dictionary<string, LootStatus> lootsStatsDic = new();//string是GUID
    public SceneAndPosition sceneIDAndPlayerPos;
    public PlayerStatsData playerStatsData;
}
public class SaveInfo
{
    public string saveID;//时间戳
    public MyEnums.SaveType saveType;
    public SaveInfo(string saveID, MyEnums.SaveType saveType)
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
    public LootStatus() { }
    public LootStatus(Vector3 pos, bool picked)
    {
        position = new SerializableVector3(pos);
        hasBeenPicked = picked;
    }
}

[Serializable]
public class SceneAndPosition
{
    public string sceneID;
    public SerializableVector3 position;
    public SceneAndPosition() { }
    public SceneAndPosition(string sceneID, Vector3 pos)
    {
        this.sceneID = sceneID;
        position = new SerializableVector3(pos);
    }
}
