using UnityEngine.UI;
using UnityEngine;

public class ContinueButton : MonoBehaviour
{
    // public DataSaveEventSO loadDataForContinue;

    [SerializeField] private Button continueButton;
    private void OnEnable()
    {
        continueButton.onClick.AddListener(() =>
        {
            if (!SaveSystem.instance.LoadSave(MyEnums.SaveType.SystemSave))
            {
                //TODO 加载失败操作
            }
        }
        );
    }
    private void OnDisable()
    {
        continueButton.onClick.RemoveAllListeners();
    }
}
