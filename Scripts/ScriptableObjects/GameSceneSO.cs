using UnityEngine;
using UnityEngine.AddressableAssets;
using MyEnums;
[CreateAssetMenu(fileName = "GameSceneSO", menuName = "GameSceneSO/SceneSO", order = 0)]
public class GameSceneSO : ScriptableObject {
    public  AssetReference sceneReference;
    public SceneType sceneType;
    public Vector3 initialPosition;
}
