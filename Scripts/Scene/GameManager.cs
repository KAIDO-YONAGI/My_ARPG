using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public SaveSystem saveSystem;

    [Header("Persist Objects")]
    public GameObject[] persistObjects;

    [Header("Save Objects")]
    public GameObject[] saveObjects;

    public static event Action OnSceneTransition;
    public static SceneTransitionData transitionData;

    public static void TriggerSceneTransition()
    {
        transitionData.hasPendingTransition = true;
        OnSceneTransition?.Invoke();
    }

    private void Awake()
    {
        saveSystem = new SaveSystem();
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            saveSystem.MarkPersistObjects(persistObjects);

            saveSystem.LoadAndRestoreSceneObjects(currentSceneName, saveObjects);
        }
        else
        {
            saveSystem.SaveSceneObjects(currentSceneName, saveObjects);
            saveSystem.CleanAndDestroy(persistObjects, gameObject);
        }
    }
}

public struct SceneTransitionData
{
    public bool hasPendingTransition;
    public Vector2 playerPosition;
}

[Serializable]
public class ObjectState
{
    public string path;
    public bool isActive;

    public ObjectState(string objectPath, bool active)
    {
        path = objectPath;
        isActive = active;
    }
}

[Serializable]
public class SceneObjectData
{
    public System.Collections.Generic.List<ObjectState> objectStates = new System.Collections.Generic.List<ObjectState>();
}

public class SceneData
{
    public string sceneName;
    public SceneObjectData objectData;

    public SceneData() { }

    public SceneData(string name, SceneObjectData data)
    {
        sceneName = name + ".yonagi";
        objectData = data;
    }

    public string getSceneName()
    {
        return sceneName;
    }
}