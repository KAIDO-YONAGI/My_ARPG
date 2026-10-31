using System.Collections.Generic;
using Contracts;
using Pipeline.SO;

namespace Pipeline.Scene
{
    public class SceneDataForSave : YSingleton<SceneDataForSave>
    {
        public List<GameSceneSO> gameScenes;
    }
}
