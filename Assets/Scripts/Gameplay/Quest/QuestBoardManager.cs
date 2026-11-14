using System.Collections.Generic;
using Pipeline.SO;
using Pipeline.SO.Events;
using UnityEngine;

namespace Gameplay.Quest
{
    public class QuestBoardManager : MonoBehaviour
    {

        [SerializeField] private List<QuestSO> questsOnBoard;//如果要区分任务实例，则需要深拷贝类包装

        [Header("Events To Trigger")]
        [SerializeField] private VoidEventSO openQuestEvent;
        [SerializeField] private LoadQuestEventSO loadQuestEventSO;
        [Header("Events To Receive")]
        [SerializeField] private ToggleCanvasEventSO toggleQuestEvent;
        private bool isInRange = false;

        // 当前覆盖着玩家的任务板；任务画布开关据此决定走任务板还是已接取任务视图。
        private static readonly List<QuestBoardManager> boardsInRange = new();

        /// <summary>是否有任务板正覆盖着玩家。</summary>
        public static bool AnyBoardInRange => boardsInRange.Count > 0;

        private void OnEnable()
        {
            toggleQuestEvent.toggleCanvasEvent += OnToggleQuestCanvas;
            if (isInRange)
                boardsInRange.Add(this);
        }
        private void OnDisable()
        {
            toggleQuestEvent.toggleCanvasEvent -= OnToggleQuestCanvas;
            boardsInRange.Remove(this);
            CloseQuestBoardIfShowing();
        }
        private void OnToggleQuestCanvas(bool state)
        {
            if (!state || !isInRange) return;
            // 画布已打开时不再装载，避免另一块板的列表覆盖正在显示的列表。
            if (QuestManager.Instance != null && QuestManager.Instance.IsCanvasActive) return;

            loadQuestEventSO.OnLoadQuestEventRaised(questsOnBoard);//初始化之后再打开面板
            openQuestEvent.OnEventRaised();
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                isInRange = true;
                if (!boardsInRange.Contains(this))
                    boardsInRange.Add(this);
            }
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                isInRange = false;
                boardsInRange.Remove(this);
                CloseQuestBoardIfShowing();
            }
        }

        private void CloseQuestBoardIfShowing()
        {
            if (QuestManager.Instance != null &&
                QuestManager.Instance.IsDisplayingQuestBoard(questsOnBoard))
            {
                QuestManager.Instance.CloseQuestBoard();
            }
        }
    }
}
