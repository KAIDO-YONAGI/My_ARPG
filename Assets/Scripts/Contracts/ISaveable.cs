public interface ISaveable
{

    DataDefinition GetDataID();

    /// <summary>登记进存档注册表。SaveRegistry 是静态类，任意生命周期阶段调用都安全，无时序依赖。</summary>
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
