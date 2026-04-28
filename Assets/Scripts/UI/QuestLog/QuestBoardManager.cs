using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//TODO 进阶：弄一个已完成任务面板
public class QuestBoardManager : MonoBehaviour
{

    [SerializeField] private List<QuestSO> questsOnBoard;//TODO如果要区分任务实例，则需要深拷贝类包装

    [Header("Events To Trigger")]
    public VoidEventSO openQuestEvent;
    public LoadQuestEventSO loadQuestEventSO;
    [Header("Events To Receive")]
    public ToggleCanvasEventSO toggleQuestEvent;
    private bool isInRange = false;


    private void OnEnable()
    {
        toggleQuestEvent.toggleCanvasEvent += OnToggleQuestCanvas;
    }
    private void OnDisable()
    {
        toggleQuestEvent.toggleCanvasEvent -= OnToggleQuestCanvas;
    }
    private void OnToggleQuestCanvas(bool state)
    {
        if (isInRange&&state)
        {
            loadQuestEventSO.OnLoadQuestEventRaised(questsOnBoard);//初始化之后再打开面板
            openQuestEvent.OnEventRaised();
        }
    }
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
}
