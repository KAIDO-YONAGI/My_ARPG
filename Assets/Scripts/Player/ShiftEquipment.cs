using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShiftEquipment : MonoBehaviour
{
    public PlayerCombat combat;
    public PlayerBow bow;

    private void Update()
    {
        if (Input.GetButtonDown("ShiftEquipment")){
            combat.enabled=!combat.enabled;
            bow.enabled=!bow.enabled;
        }
    }
}
