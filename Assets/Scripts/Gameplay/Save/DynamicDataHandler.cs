using System.Collections.Generic;

public static class DynamicDataHandler
{
    public static void PrepareForNewGameLoad(Data data)
    {
        ClearDynamicData(data);
    }

    public static void ClearDynamicData(Data data)
    {
        data.lootsStatsDic = new Dictionary<string, LootStatus>();
    }
}
