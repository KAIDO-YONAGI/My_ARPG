using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class QuestBoardManager : MonoBehaviour
{
    public VoidEventSO openQuestEvent;

    [SerializeField] bool isInRange = false;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isInRange = true;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isInRange = false;
    }

    private void Update()
    {
        if (Input.GetButtonDown("OpenQuestList") && isInRange)
        {
            openQuestEvent.OnEventRaised();
        }
    }
}
