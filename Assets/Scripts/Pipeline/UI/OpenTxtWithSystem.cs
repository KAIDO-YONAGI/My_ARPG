using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Pipeline.UI
{
    public class OpenTxtWithSystem : MonoBehaviour
    {
        public void OpenGuide()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "GameGuide.txt");

            if (File.Exists(path))
            {
                Process.Start(path); // 用系统默认程序打开（记事本）
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError("找不到游戏指南文件：" + path);
#endif
            }
        }
    }
}