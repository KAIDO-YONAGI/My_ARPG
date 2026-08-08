using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MyEnums;

public class ShiftEquipment : MonoBehaviour
{
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerBow bow;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference shiftEquipmentAction;

    [Header("Action Finished Events")]
    [SerializeField] private VoidEventSO slashActionFinishedEvent;
    [SerializeField] private VoidEventSO shootActionFinishedEvent;

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
            // 翻转武器激活状态（不禁用组件，确保 Animation Event 始终能被接收）
            combat.SetActive(!combat.IsActive);
            bow.SetActive(!bow.IsActive);

            // 切换装备时两种动作都视为结束，通过事件通知 PlayerMovement 统一重置状态
            if (slashActionFinishedEvent != null) slashActionFinishedEvent.OnEventRaised();
            if (shootActionFinishedEvent != null) shootActionFinishedEvent.OnEventRaised();

            shiftTimer = shiftCooldown;
        }
    }
}
