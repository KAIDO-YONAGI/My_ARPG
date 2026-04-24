using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//TODO 进阶：弄一个已完成任务面板
public class QuestBoardManager : MonoBehaviour
{

    [SerializeField] private List<QuestSO> questsOnBoard;//TODO如果要区分任务实例，则需要深拷贝类包装

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

            // foreach (var item in questsOnBoard)
            // {
            //     Debug.Log(item.GetInstanceID());
            // }
        }
    }
}
