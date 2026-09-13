using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class NPCStateController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private NPCWander wanderScript;
    [FormerlySerializedAs("chatScript")]
    [SerializeField] private NPCDialogTrigger dialogTrigger;
    [SerializeField] private NPCPatrol patrolScript;
    [SerializeField] private MyEnums.NPCState DefaultState = MyEnums.NPCState.Patrol;
    private MyEnums.NPCState currentState;

    private void Start()
    {
        SwitchState(DefaultState);
    }
    public void SwitchState(MyEnums.NPCState newState)
    {
        currentState = newState;
        if (wanderScript != null) wanderScript.enabled = currentState == MyEnums.NPCState.Wander;
        if (dialogTrigger != null) dialogTrigger.enabled = currentState == MyEnums.NPCState.Chat;
        if (patrolScript != null) patrolScript.enabled = currentState == MyEnums.NPCState.Patrol;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SwitchState(MyEnums.NPCState.Chat);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SwitchState(DefaultState);
        }
    }

}
