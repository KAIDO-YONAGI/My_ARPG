using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档注册表：ISaveable 的登记处。静态类先于一切场景实例存在，
/// 注册与注销不依赖 DataManager 是否已初始化，Awake 顺序不再影响注册——
/// 这是"活体找活体"时序问题的结构性解法。
/// DataManager 在存读档时读取 All 遍历。
/// static 列表的两类残留场景：编辑器关闭 Domain Reload 时由 ResetStatics
/// 在每次进 Play 前清空；build 内"死亡重试/回主菜单再开新局"不重启进程，
/// 由场景切换流程在切往 Menu 时调用 Clear（见指南 §3.2 待办）。
/// </summary>
public static class SaveRegistry
{
    private static readonly List<ISaveable> saveables = new();

    /// <summary>当前登记的所有存档参与者。遍历前自行拷贝（ToList），防止遍历中注册/注销修改集合。</summary>
    public static IReadOnlyList<ISaveable> All => saveables;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => saveables.Clear();

    public static void Add(ISaveable saveable)
    {
        if (!saveables.Contains(saveable))
            saveables.Add(saveable);
    }

    public static void Remove(ISaveable saveable) => saveables.Remove(saveable);

    /// <summary>局间复位：回主菜单 / 重开新局时由场景切换流程调用。编辑器进 Play 已由 ResetStatics 兜底。</summary>
    public static void Clear() => saveables.Clear();
}
