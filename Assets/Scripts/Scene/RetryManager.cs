using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class RetryManager : MonoBehaviour
{
    public static RetryManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    [SerializeField] private GameSceneSO currentScene;
    [SerializeField] private SceneLoadEventSO loadEventSO;
    [Header("Retry Event")]
    [SerializeField] private VoidEventSO retryEventSO;

    private void OnEnable()
    {
        retryEventSO.VoidEvent += OnReTry;
    }
    private void OnDisable()
    {
        retryEventSO.VoidEvent -= OnReTry;
    }
    private void OnReTry()
    {
        loadEventSO.RaiseLoadRequestEvent(currentScene, Vector3.zero, true);
        StatsManager.instance.Respawn();
    }
}
