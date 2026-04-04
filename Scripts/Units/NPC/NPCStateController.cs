using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NPCStateController : MonoBehaviour
{
    public MyEnums.NPCState currentState;
    public NPCPatrol patrolState;
    public NPCChat chatState;
    private void Start()
    {
        SwitchState(MyEnums.NPCState.Patrol);
    }
    public void SwitchState(MyEnums.NPCState newState)
    {
        currentState = newState;
        patrolState.enabled = (currentState == MyEnums.NPCState.Patrol);
        chatState.enabled = (currentState == MyEnums.NPCState.Chat);
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
            SwitchState(MyEnums.NPCState.Patrol);
        }
    }
}
