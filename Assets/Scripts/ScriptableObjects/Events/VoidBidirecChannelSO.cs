using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidBidirecChannelSO : ScriptableObject
{
    public delegate void VoidBidirecEvent();
    public event VoidBidirecEvent OnEventRaised;

    public void RaiseEvent()
    {
        if (OnEventRaised != null)
        {
            OnEventRaised.Invoke();
        }
    }
}
