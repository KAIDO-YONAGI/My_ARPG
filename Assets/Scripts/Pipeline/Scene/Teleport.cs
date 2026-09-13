using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneToggler : MonoBehaviour
{
    [SerializeField] private Vector3 newPosition;
    [SerializeField] private GameSceneSO sceneToLoad;
    [SerializeField] private bool isToFade = true;
    private void OnTriggerEnter2D(Collider2D collider)
    {
        // 场景切换走 SceneChanger 的唯一入口；直接 Raise 事件只会通知订阅方，不会切场景
        SceneChanger.Instance.RequestSceneLoad(sceneToLoad, newPosition, isToFade);
    }
}
