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