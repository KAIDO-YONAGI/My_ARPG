public interface ISaveable
{
    DataDefinition GetDataID();
    void RegisterSaveable()
    {
        DataManager.instance.RegisterSaveableData(this);
    }
    void UnRegisterSaveable()
    {
        DataManager.instance.UnRegisterSaveableData(this);
    }

    void SaveData(Data data);
    void LoadData(Data data);
}
