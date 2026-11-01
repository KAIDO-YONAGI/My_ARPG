using Contracts.Enums;
using Gameplay.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Pipeline.UI.Buttons
{
    public class ContinueButton : MonoBehaviour
    {
        // public DataSaveEventSO loadDataForContinue;

        [SerializeField] private Button continueButton;
        private void OnEnable()
        {
            continueButton.onClick.AddListener(() =>
                {
                    string savePath = SaveSystem.Instance.GetLatestLoadableSavePath(SaveType.SystemSave);
                    if (string.IsNullOrEmpty(savePath) || !SaveSystem.Instance.LoadSave(SaveType.SystemSave, savePath))
                    {
                        //TODO加载失败处理
                    }
                }
            );
        }
        private void OnDisable()
        {
            continueButton.onClick.RemoveAllListeners();
        }
    }
}
