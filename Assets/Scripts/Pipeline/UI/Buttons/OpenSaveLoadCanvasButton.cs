using Contracts.Enums;
using Pipeline.UI.SystemCanvasManagers;
using UnityEngine;
using UnityEngine.UI;

namespace Pipeline.UI.Buttons
{
    public class OpenSaveLoadCanvasButton : MonoBehaviour
    {
        [SerializeField] private Button OpenButton;
        private void OnEnable()
        {
            OpenButton.onClick.AddListener(
                () =>
                {
                    // SaveLoad 不配置按键，通过统一 RequestCanvasToggle 请求分支进入焦点栈体系。
                    UIManager.Instance.RequestCanvasToggle(CanvasType.SaveLoad);
                }
            );
        }
        private void OnDisable()
        {
            OpenButton.onClick.RemoveAllListeners();
        }
    }
}
