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
                // SaveLoad 不配置按键，通过统一 RequestCanvasToggle 请求分支进入焦点栈体系。
                UIManager.Instance.RequestCanvasToggle(MyEnums.CanvasToToggle.SaveLoad);
            }
        );
    }
    private void OnDisable()
    {
        OpenButton.onClick.RemoveAllListeners();
    }
}
