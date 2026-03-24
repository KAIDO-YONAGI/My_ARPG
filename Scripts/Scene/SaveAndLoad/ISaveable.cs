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

    void GetSaveData(Data data);
    void LoadSaveData(Data data);
}
