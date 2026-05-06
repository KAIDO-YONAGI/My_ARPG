using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ESCButton : MonoBehaviour
{
    public Button escButton;
    private void OnEnable() {
        escButton.onClick.AddListener(OnESC);
    }
    private void OnDisable()
    {
        escButton.onClick.RemoveAllListeners();
    }
    private void OnESC()
    {
        UIManager.instance.SetCanvasToggle(MyEnums.CanvasToToggle.ESC,true);
    }
}
