using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonSceneToggler : MonoBehaviour
{
    public SceneLoadEventSO loadEventSO;
    public Vector3 newPosition;
    public GameSceneSO sceneToLoad;
    public bool isToFade = true;
    public void RaiseLoadRequestEvent()
    {
        Debug.Log("ButtonSceneToggler: Raising Load Request Event for scene " + sceneToLoad.sceneType);
        loadEventSO.RaiseLoadRequestEvent(sceneToLoad, newPosition, isToFade);
    }
}
