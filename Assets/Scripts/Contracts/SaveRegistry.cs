using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档注册表，ISaveable 的登记处。静态类先于一切场景实例存在，
/// 注册与注销在任何生命周期阶段调用都安全，Awake 顺序不影响注册结果。
/// DataManager 在存读档时读取 All 遍历。
/// 编辑器关闭 Domain Reload 时 static 列表会跨 Play 存活，
/// ResetStatics 在每次进入 Play 前清空一次，保证两种配置下行为一致。
/// </summary>
public static class SaveRegistry
{
    private static readonly List<ISaveable> saveables = new();

    /// <summary>当前登记的所有存档参与者。遍历前自行拷贝一份，防止遍历中注册或注销修改集合。</summary>
    public static IReadOnlyList<ISaveable> All => saveables;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => saveables.Clear();

    public static void Add(ISaveable saveable)
    {
        if (!saveables.Contains(saveable))
            saveables.Add(saveable);
    }

    public static void Remove(ISaveable saveable) => saveables.Remove(saveable);

    /// <summary>清空注册表，用于同一进程内重开新局时的局间复位。</summary>
    public static void Clear() => saveables.Clear();
}
