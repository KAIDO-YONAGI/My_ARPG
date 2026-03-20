using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct SceneTransitionData
{
    public bool hasPendingTransition;
    public Vector2 playerPosition;
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Persist Objects")]
    public GameObject[] persistObjects;

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
            MarkPersistObjects();
        }
        else
        {
            CleanAndDestroy();
        }
    }

    private void MarkPersistObjects()
    {
        foreach (GameObject obj in persistObjects)
        {
            if (obj != null)
            {
                obj.transform.SetParent(null);//转为父对象再设置为不销毁
                DontDestroyOnLoad(obj);
            }
        }
    }
    private void CleanAndDestroy()
    {
        foreach (GameObject obj in persistObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        Destroy(gameObject);
    }

}
