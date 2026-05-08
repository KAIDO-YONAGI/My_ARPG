using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
public class Data//string是GUID
{
    //元组的第一个参数代表位置，第二个代表是否被拾取
    public Dictionary<string, (Vector3, bool)> lootsStatsDic = new();
    public Tuple<string, Vector3> SceneNameAndPlayerPos;


}

