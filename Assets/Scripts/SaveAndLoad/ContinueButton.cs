using UnityEngine.UI;
using UnityEngine;

public class ContinueButton : MonoBehaviour
{
    DataSaveEventSO loadDataForContinue;

    [SerializeField] private Button continueButton;
    private void OnEnable()
    {
        continueButton.onClick.AddListener(() => SaveSystem.instance.LoadSave(MyEnums.SaveType.SystemSave));
    }
    private void OnDisable()
    {
        continueButton.onClick.RemoveAllListeners();
    }
}
