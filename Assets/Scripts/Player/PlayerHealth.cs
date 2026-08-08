using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        StatsManager.instance.Respawn();
    }
    public void ChangeHealth(int amount)
    {
        StatsManager.instance.UpdateHealth(amount);

        if (StatsManager.instance.GetCurrentHealth() <= 0)
        {
            gameObject.SetActive(false);
            //通过 UIManager 画布调度系统弹出 GameOver，不再直接持有画布引用
            if (UIManager.instance != null)
            {
                UIManager.instance.RequestCanvasToggle(MyEnums.CanvasToToggle.GameOver);
            }
        }
    }
}
