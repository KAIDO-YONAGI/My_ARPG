using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyEnums;
public class DataDefinition : MonoBehaviour
{
    public PersistentType persistentType;
    public string ID;
    private void OnValidate()
    {
        if (persistentType == PersistentType.ReadWrite)
        {
            if (string.IsNullOrEmpty(ID))
            {
                ID = System.Guid.NewGuid().ToString();
            }
        }
        else
        {
            ID = string.Empty;
        }
    }
}
