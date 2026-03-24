using System.Collections.Generic;
using UnityEngine;


// 每个场景挂载的组件，用于标记需要持久化的场景数据

public class SaveManager : MonoBehaviour
{
    [Header("SceneSaves")]
    public List<GameObject> saveObjects = new List<GameObject>();

    
    // 获取需要保存的对象数组
    
    public GameObject[] GetSaveObjects()
    {
        return saveObjects.ToArray();
    }

    
    // 添加保存对象
    
}