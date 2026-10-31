using Contracts;
using Pipeline.UI.SystemCanvasManagers;
using UnityEngine;
using UnityEngine.UI;

namespace Pipeline.UI.Buttons
{
    public class ESCButton : MonoBehaviour
    {
        [SerializeField] private Button escButton;
        [SerializeField] private CanvasToToggle canvasToESC;
        private void OnEnable() {
            escButton.onClick.AddListener(OnESC);
        }
        private void OnDisable()
        {
            escButton.onClick.RemoveAllListeners();
        }
        private void OnESC()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.RequestCanvasClose(canvasToESC);
        }


    }
}
