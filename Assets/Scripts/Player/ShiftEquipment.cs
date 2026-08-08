using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MyEnums;

public class ShiftEquipment : MonoBehaviour
{
    public PlayerCombat combat;
    public PlayerBow bow;
    public PlayerMovement playerMovement;

    [Header("Input Actions")]
    public InputActionReference shiftEquipmentAction;

    private float shiftCooldown = 0.3f;
    private float shiftTimer;

    private void OnEnable()
    {
        if (shiftEquipmentAction != null) shiftEquipmentAction.action.Enable();
    }

    private void OnDisable()
    {
        if (shiftEquipmentAction != null) shiftEquipmentAction.action.Disable();
    }

    private void Update()
    {
        if (shiftTimer > 0)
            shiftTimer -= Time.deltaTime;

        if (shiftEquipmentAction != null && shiftEquipmentAction.action.WasPressedThisFrame() && shiftTimer <= 0)
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
