using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
public static GameManager instance;

[Header("Persist Objects")]
public GameObject[] persistObjects;

// 场景切换事件
public static event Action OnSceneTransition;

// 是否有待处理的场景切换
public static bool hasPendingTransition = false;

// 触发场景切换事件
public static void TriggerSceneTransition()
{
    hasPendingTransition = true;
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
                obj.transform.SetParent(null);
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
