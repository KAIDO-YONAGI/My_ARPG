using UnityEngine;
using UnityEngine.AddressableAssets;
using MyEnums;
[CreateAssetMenu(fileName = "GameSceneSO", menuName = "GameSceneSO/SceneSO", order = 0)]
public class GameSceneSO : ScriptableObject {
    // 注意：这不是存档键。ID 由 OnValidate 生成且不落盘，跨会话/打包会变，不要用它索引存档。
    public string ID;
    public  AssetReference sceneReference;
    public SceneType sceneType;
    public Vector3 initialPosition;

    /// <summary>
    /// 存档用的场景稳定标识：Addressables 资产 GUID，随资产落盘，跨会话与打包一致。
    /// 存档 Data.sceneIDAndPlayerPos.sceneID 存的是它，读档时由 SceneDataForSave.gameScenes 反查回本资产。
    /// </summary>
    public string SaveKey => sceneReference != null ? sceneReference.AssetGUID : string.Empty;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(ID))
            ID = System.Guid.NewGuid().ToString();
    }
}
