using UnityEngine;

/// <summary>
/// 可存档服务的基础范式，与 YSingleton 同类的能力基类。继承即获得三件事：
/// 初始化时注册进静态 SaveRegistry、销毁时注销、以固定槽位身份参与存档。
/// 子类只实现 SaveData 与 LoadData；重写 OnSingletonInitialized 时必须调用 base，
/// 注册发生在 base 里。
/// GetDataID 返回 null：服务走固定槽位存档，写入 Data 上自己的字段；
/// 按 GUID 参与动态数据的场景物体，如 Loot，直接实现 ISaveable。
/// </summary>
public abstract class SaveableService<TSelf> : YSingleton<TSelf>, ISaveable
    where TSelf : MonoBehaviour
{
    protected override void OnSingletonInitialized()
    {
        SaveRegistry.Add(this);
    }

    protected override void OnDestroy()
    {
        SaveRegistry.Remove(this);
        base.OnDestroy();
    }

    public DataDefinition GetDataID() => null;

    public abstract void SaveData(Data data);

    public abstract void LoadData(Data data);
}
