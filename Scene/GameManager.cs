using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
public static GameManager instance;

[Header("Persist Objects")]
public GameObject[] persistObjects;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            MarkPersistObjects();
        }
        else//进入新场景中自动销毁当前（旧）对象
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
