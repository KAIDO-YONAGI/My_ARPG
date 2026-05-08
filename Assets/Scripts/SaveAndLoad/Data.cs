using System;
using System.Collections.Generic;
using UnityEngine;

public class Save
{
    SaveInfo saveInfo;

    Data data;
    public Save(SaveInfo saveInfo, Data data)
    {
        this.saveInfo = saveInfo;
        this.data = data;

    }
}
public class Data//string是GUID
{
    //元组的第一个参数代表位置，第二个代表是否被拾取
    public Dictionary<string, (Vector3, bool)> lootsStatsDic = new();
    public Tuple<string, Vector3> SceneNameAndPlayerPos;
    public PlayerStatsData playerStatsData;
}
public class SaveInfo
{
    string saveID;//用于标识
    MyEnums.SaveType saveType;
    public SaveInfo(string saveID,MyEnums.SaveType saveType)
    {
        this.saveID=saveID;
        this.saveType=saveType;
    }
}

