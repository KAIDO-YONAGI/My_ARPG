using System.Collections.Generic;

namespace Gameplay.Save
{
    public static class DynamicDataHandler
    {
        public static void PrepareForNewGameLoad(SaveData data)
        {
            ClearDynamicData(data);
        }

        public static void ClearDynamicData(SaveData data)
        {
            data.lootsStatsDic = new Dictionary<string, LootStatus>();
        }
    }
}
