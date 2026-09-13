using UnityEngine;

/// <summary>
/// 可存档服务的基础范式（与 YSingleton 同类的能力基类）：继承即得
/// "注册进 SaveRegistry + 销毁注销 + 固定槽位存档身份"，无时序依赖、无需重试注册。
/// 子类只实现 SaveData/LoadData；重写 OnSingletonInitialized 时必须调用 base（注册在那里发生）。
/// GetDataID 返回 null 表示固定槽位存档（如 data.playerStatsData），不参与按 GUID 的动态物体数据；
/// 场景物体类存档参与者（如 Loot）直接实现 ISaveable，不走本基类。
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
