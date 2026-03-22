using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


// 全局唯一单例，管理场景间切换时需要保留的持久化对象

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // 场景间保持的持久化对象（如玩家、UI等）
    [Header("Persistant Objects")]

    public GameObject[] persistObjects;

    // 当前场景的SaveSystem引用
    public SaveSystem currentSceneSaveSystem { get; private set; }

    // 场景切换事件
    public static event Action OnSceneTransition;

    // 场景切换数据
    public static SceneTransitionData transitionData;

    
    // 触发场景切换事件
    
    public static void TriggerSceneTransition()
    {
        transitionData.hasPendingTransition = true;
        OnSceneTransition?.Invoke();
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            MarkPersistObjects(persistObjects);
        }
        else
        {
            CleanAndDestroy(persistObjects, gameObject);
        }
    }
    public void CleanAndDestroy(GameObject[] persistObjects, GameObject owner)
    {
        foreach (GameObject obj in persistObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        if (owner != null)
        {
            Destroy(owner);
        }
    }
    public void MarkPersistObjects(GameObject[] objects)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.transform.SetParent(null);
                DontDestroyOnLoad(obj);
            }
        }
    }

}


// 场景切换数据

public struct SceneTransitionData
{
    public bool hasPendingTransition;
    public Vector2 playerPosition;
}


