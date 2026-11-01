using System.Collections.Generic;
using Contracts.Base;
using Pipeline.SO;

namespace Pipeline.Scene
{
    public class SceneDataForSave : YSingleton<SceneDataForSave>
    {
        public List<GameSceneSO> gameScenes;
    }
}
