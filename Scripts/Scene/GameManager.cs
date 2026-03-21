using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public struct SceneTransitionData//保证场景切换效果的
{
    public bool hasPendingTransition;
    public Vector2 playerPosition;
}
public class SceneData
{
    private string sceneName;
    private GameObject[] sceneObjects;

    public SceneData(GameObject[] objects)
    {
        sceneName = SceneManager.GetActiveScene().name+".yonagi";
        sceneObjects = objects;
    }
    public string getSceneName()
    {
        return sceneName;
    }
}
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public SaveSystem saveSystem;

    [Header("Persist Objects")]
    public GameObject[] persistObjects;

    [Header("Save Objects")]
    public GameObject[] saveObjects;

    public SceneChanger sceneChanger;



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
        saveSystem = new SaveSystem();

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            saveSystem.MarkPersistObjects(persistObjects);


        }
        else//如果实例已经存在，说明已经跳到下一个场景，调用相关函数保存当前数据
        {
            SceneData sceneData = new(saveObjects);
            saveSystem.SaveByJson(DateTime.Now.ToString() + sceneData.getSceneName(), sceneData);
            saveSystem.CleanAndDestroy(persistObjects, gameObject);
        }
    }



}
