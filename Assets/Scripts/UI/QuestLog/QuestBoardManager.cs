using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//TODO 任务板里存储任务及其完成状态的列表，打开任务栏的时候广播这个列表，UI接收之后初始化对应信息，以及Details自动打开第一个,如果为空，关闭Details
//TODO 进阶：弄一个已完成任务面板
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
