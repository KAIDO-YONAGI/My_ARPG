using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[DefaultExecutionOrder(-100)]
public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    List<ISaveable> saveables = new List<ISaveable>();

    Data dataToSave;

    [Header("Receive")]
    public VoidEventSO saveDataEvent;
    public VoidEventSO loadDataEvent;

    private void OnEnable()
    {
        saveDataEvent.VoidEvent += Save;
        loadDataEvent.VoidEvent += Load;
    }
    private void OnDisable()
    {
        saveDataEvent.VoidEvent -= Save;
        loadDataEvent.VoidEvent -= Load;

    }
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        dataToSave = new Data();
    }

    public void RegisterSaveableData(ISaveable saveable)
    {
        if (!saveables.Contains(saveable))
        {
            saveables.Add(saveable);
        }
    }

    public void UnRegisterSaveableData(ISaveable saveable)
    {
        saveables.Remove(saveable);
    }
    void Save()
    {
        foreach (var saveable in saveables.ToList())
        {
            saveable.SaveData(dataToSave);
        }
    }
    void Load()
    {
        foreach (var saveable in saveables.ToList())
        {
            saveable.LoadData(dataToSave);
        }
    }

}
