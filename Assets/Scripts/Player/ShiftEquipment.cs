using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyEnums;

public class ShiftEquipment : MonoBehaviour
{
    public PlayerCombat combat;
    public PlayerBow bow;
    public PlayerMovement playerMovement;

    private float shiftCooldown = 0.3f;
    private float shiftTimer;

    private void Update()
    {
        if (shiftTimer > 0)
            shiftTimer -= Time.deltaTime;

        if (Input.GetButtonDown("ShiftEquipment") && shiftTimer <= 0)
        {
            combat.enabled = !combat.enabled;
            bow.enabled = !bow.enabled;

            playerMovement.AnimatorSM(PlayerState.Idle);
            playerMovement.animator.SetBool("isAttacking", false);
            playerMovement.animator.SetBool("isShooting", false);
            playerMovement.SetCanBeInterrupted(true);
            playerMovement.ResetTimer();

            shiftTimer = shiftCooldown;
        }
    }
}
