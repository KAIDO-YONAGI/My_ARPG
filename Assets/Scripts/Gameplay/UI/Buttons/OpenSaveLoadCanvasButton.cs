using UnityEngine;
using UnityEngine.UI;

public class OpenSaveLoadCanvasButton : MonoBehaviour
{
    [SerializeField] private Button OpenButton;
    private void OnEnable()
    {
        OpenButton.onClick.AddListener(
            () =>
            {
                // 经 UIManager 统一入口唤起 SaveLoad，进入焦点栈体系
                UIManager.Instance.RequestCanvasToggle(MyEnums.CanvasToToggle.SaveLoad);
            }
        );
    }
    private void OnDisable()
    {
        OpenButton.onClick.RemoveAllListeners();
    }
}
