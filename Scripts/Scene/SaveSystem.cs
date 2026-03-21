using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem
{
    public void MarkPersistObjects(GameObject[] persistObjects)
    {
        foreach (GameObject obj in persistObjects)
        {
            if (obj != null)
            {
                obj.transform.SetParent(null);
                MonoBehaviour.DontDestroyOnLoad(obj);
            }
        }
    }

    public void CleanAndDestroy(GameObject[] persistObjects, GameObject owner)
    {
        foreach (GameObject obj in persistObjects)
        {
            if (obj != null)
            {
                MonoBehaviour.Destroy(obj);
            }
        }
        if (owner != null)
        {
            MonoBehaviour.Destroy(owner);
        }
    }

    public void SaveSceneObjects(string sceneName, GameObject[] saveObjects)
    {
        if (saveObjects == null || saveObjects.Length == 0) return;

        SceneObjectData objectData = new SceneObjectData();

        foreach (GameObject obj in saveObjects)
        {
            if (obj != null)
            {
                CollectChildObjects(obj, objectData.objectStates);
            }
        }

        SceneData sceneData = new SceneData(sceneName, objectData);
        SaveByJson(sceneName + ".yonagi", sceneData);
    }

    private void CollectChildObjects(GameObject parent, List<ObjectState> states)
    {
        states.Add(new ObjectState(GetObjectPath(parent), parent.activeSelf));

        foreach (Transform child in parent.transform)
        {
            CollectChildObjects(child.gameObject, states);
        }
    }

    private string GetObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null && parent != obj.transform.root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    public void LoadAndRestoreSceneObjects(string sceneName, GameObject[] saveObjects)
    {
        if (saveObjects == null || saveObjects.Length == 0) return;

        SceneData sceneData = LoadByJson<SceneData>(sceneName + ".yonagi");

        if (sceneData == null || sceneData.objectData == null || sceneData.objectData.objectStates == null)
        {
            Debug.Log("未找到存档或存档为空，使用默认场景状态");
            return;
        }

        Debug.Log($"找到存档，正在恢复 {sceneData.objectData.objectStates.Count} 个对象的状态");

        foreach (ObjectState state in sceneData.objectData.objectStates)
        {
            GameObject obj = FindObjectByPath(state.path, saveObjects);
            if (obj != null)
            {
                obj.SetActive(state.isActive);
            }
            else
            {
                Debug.LogWarning($"未找到对象: {state.path}");
            }
        }
    }

    private GameObject FindObjectByPath(string path, GameObject[] saveObjects)
    {
        foreach (GameObject root in saveObjects)
        {
            if (root != null)
            {
                GameObject result = FindChildByPath(root.transform, path);
                if (result != null) return result;
            }
        }

        return GameObject.Find(path);
    }

    private GameObject FindChildByPath(Transform parent, string path)
    {
        string[] parts = path.Split('/');

        if (parts.Length == 0) return null;

        Transform current = parent;
        for (int i = 0; i < parts.Length; i++)
        {
            if (current == null) return null;

            if (current.name == parts[i] && i == parts.Length - 1)
            {
                return current.gameObject;
            }

            Transform child = null;
            foreach (Transform t in current)
            {
                if (t.name == parts[i])
                {
                    child = t;
                    break;
                }
            }

            if (child != null)
            {
                current = child;
            }
            else
            {
                return null;
            }
        }

        return current?.gameObject;
    }

    public void SaveByJson(string fileName, SceneData data)
    {
        var json = JsonUtility.ToJson(data);
        var path = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            File.WriteAllText(path, json);
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to save data to {path}: {e.Message}");
        }
        #if UNITY_EDITOR
        Debug.Log($"Saved data to {path}");
        #endif
    }

    public T LoadByJson<T>(string fileName)
    {
        var path = Path.Combine(Application.persistentDataPath, fileName);
        var data = default(T);
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                data = JsonUtility.FromJson<T>(json);
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to load data from {path}: {e.Message}");
                data = default;
            }
            #if UNITY_EDITOR
            Debug.Log($"Loaded data from {path}");
            #endif
        }
        else
        {
            Debug.LogWarning($"Save file not found at {path}");
            data = default;
        }
        return data;
    }

    public void DeleteSaveFile(string fileName)
    {
        var path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException e)
            {
                Debug.LogError($"Failed to delete save file at {path}: {e.Message}");
            }
            #if UNITY_EDITOR
            Debug.Log($"Deleted save file at {path}");
            #endif
        }
        else
        {
            Debug.LogWarning($"Save file not found at {path} for deletion");
        }
    }
}