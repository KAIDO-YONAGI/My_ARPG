using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//TODO 任务板里存它特定的任务，每次请求打开任务的时候向管理器广播这个信号，让它选择性刷新相应任务，能确保刷新时机正确
//TODO 进阶：弄一个已完成任务面板
public class QuestBoardManager : MonoBehaviour
{

    [SerializeField] private List<QuestSO> questsOnBoard;

    [Header("Events")]
    public VoidEventSO openQuestEvent;
    public LoadQuestEventSO loadQuestEventSO;


    private bool isInRange = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = true;
        }
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
            loadQuestEventSO.OnLoadQuestEventRaised(questsOnBoard);//初始化之后再打开面板
            openQuestEvent.OnEventRaised();
        }
    }
}
