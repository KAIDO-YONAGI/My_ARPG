using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct PositionInfo
{
    private static string guid = System.Guid.NewGuid().ToString();
    public Vector3 pos;
}
//同步方登记自己的位置
//使用方传入一个值来查找 查找时算法？查找时发送事件让同步放更新一次然后再查，有时序问题？
//空间规划算法？
public class ObjectsMapManager : YSingleton<ObjectsMapManager>
{
    /// <summary>
    /// Tag,PositionInfo
    /// </summary>
    Dictionary<string, PositionInfo> objects = new();

    // Start is called before the first frame update
    void Start()
    {
        PositionInfo positionInfo = new();
        positionInfo.pos = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
    }

    void RegistToMap(Vector3 worldPos)
    {
    }
}