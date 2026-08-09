using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisitedHistoryManager : MonoBehaviour
{
    private static VisitedHistoryManager _instance;
    public static VisitedHistoryManager Instance => _instance;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
