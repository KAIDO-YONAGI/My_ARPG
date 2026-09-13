public interface ISaveable
{

    DataDefinition GetDataID();

    /// <summary>登记进存档注册表，任意生命周期阶段调用都安全。</summary>
    void RegisterSaveable()
    {
        SaveRegistry.Add(this);
    }
    void UnRegisterSaveable()
    {
        SaveRegistry.Remove(this);
    }

    void SaveData(Data data);
    void LoadData(Data data);
}
