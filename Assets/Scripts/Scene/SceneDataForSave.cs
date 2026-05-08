using System.Collections.Generic;
using UnityEngine;

public class SceneDataForSave : MonoBehaviour
{
    public static SceneDataForSave Instance;

    public List<GameSceneSO> gameScenes;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
