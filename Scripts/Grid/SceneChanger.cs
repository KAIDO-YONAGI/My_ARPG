using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChanger : MonoBehaviour
{
    public GameSceneSO firstScene;

    private void Awake() {
        Addressables.LoadSceneAsync(firstScene.sceneReference, LoadSceneMode.Additive);
    }
}
