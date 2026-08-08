using UnityEngine;
using UnityEngine.InputSystem;
using System.Diagnostics;
using System.IO;

public class OpenTxtWithSystem : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference openGuideAction;

    private void OnEnable()
    {
        if (openGuideAction != null) openGuideAction.action.Enable();
    }

    private void OnDisable()
    {
        if (openGuideAction != null) openGuideAction.action.Disable();
    }

    void Update()
    {
        if (openGuideAction != null && openGuideAction.action.WasPressedThisFrame())
        {
            OpenGuide();
        }
    }

    public void OpenGuide()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "游戏指南.txt");

        if (File.Exists(path))
        {
            Process.Start(path); // 用系统默认程序打开（记事本）
        }
        else
        {
            UnityEngine.Debug.LogError("找不到游戏指南文件：" + path);
        }
    }
}