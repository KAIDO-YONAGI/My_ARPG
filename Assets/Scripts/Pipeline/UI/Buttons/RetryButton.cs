using Pipeline.SO.Events;
using UnityEngine;

namespace Pipeline.UI.Buttons
{
    public class RetryButton : MonoBehaviour
    {
        [SerializeField] private CanvasGroup ButtonCanvas;
        [SerializeField] private VoidEventSO retryEventSO;

        public void HandleRetry()//editor内由button组件绑定
        {
            ButtonCanvas.alpha = 0;
            ButtonCanvas.interactable = false;
            ButtonCanvas.blocksRaycasts = false;

            TimeManager.Instance.ForceResumeGame();
            retryEventSO.OnEventRaised();
        }
    }
}
