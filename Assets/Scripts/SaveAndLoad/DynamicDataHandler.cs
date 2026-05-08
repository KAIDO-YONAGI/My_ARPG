using System.Collections.Generic;

public static class DynamicDataHandler
{
    // NewGame 的动态数据入口，后面新增动态字段时统一从这里扩展。
    public static void PrepareForNewGameLoad(Data data)
    {
        ClearDynamicData(data);
    }

    // 目前动态数据先处理 loot，后续再有别的动态字段继续在这里补清理逻辑。
    public static void ClearDynamicData(Data data)
    {
        if (data == null)
        {
            return;
        }

        if (data.lootsStatsDic == null)
        {
            data.lootsStatsDic = new Dictionary<string, LootStatus>();
            return;
        }

        data.lootsStatsDic.Clear();
    }
}
