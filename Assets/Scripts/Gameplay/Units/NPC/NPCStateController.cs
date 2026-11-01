using Contracts.Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.Units.NPC
{
    public class NPCStateController : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField] private NPCWander wanderScript;
        [FormerlySerializedAs("chatScript")]
        [SerializeField] private NPCDialogTrigger dialogTrigger;
        [SerializeField] private NPCPatrol patrolScript;
        [SerializeField] private NPCState DefaultState = NPCState.Patrol;
        private NPCState currentState;

        private void Start()
        {
            SwitchState(DefaultState);
        }
        public void SwitchState(NPCState newState)
        {
            currentState = newState;
            if (wanderScript != null) wanderScript.enabled = currentState == NPCState.Wander;
            if (dialogTrigger != null) dialogTrigger.enabled = currentState == NPCState.Chat;
            if (patrolScript != null) patrolScript.enabled = currentState == NPCState.Patrol;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                SwitchState(NPCState.Chat);
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
}
