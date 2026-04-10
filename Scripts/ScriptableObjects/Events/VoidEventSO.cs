using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "VoidEventSO", menuName = "GameSceneSO/VoidEventSO", order = 0)]

public class VoidEventSO : ScriptableObject
{
    public UnityAction VoidEvent;

    public void OnEventRaised()
    {
        VoidEvent?.Invoke();
    }
}
