using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHistoryManager : MonoBehaviour
{
    public static ItemHistoryManager instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else Destroy(gameObject);
    }
}
