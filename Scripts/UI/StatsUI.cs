using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{
    public GameObject[] statsSlots;
    public CanvasGroup statsCanvas;

    private bool statsIsOpen = false;


    private void Awake()//对象实例化就会进行，先于Start()
    {
        statsCanvas.alpha = 0; ;
    }
    private void Start()
    {
        UpdateAllStats();
    }
    private void Update()
    {
        if (Input.GetButtonDown("ToggleStats"))
        {
            if (statsIsOpen)
            {
                TimeManager.instance.ResumeGame();
                statsCanvas.alpha = 0;//组件alpha值设置为0，依赖父对象的canvasGroup组件来实现，需要在unity中实现绑定
                statsCanvas.blocksRaycasts = false;
                statsIsOpen = false;
            }
            else
            {
                TimeManager.instance.PauseGame();
                statsCanvas.alpha = 1;
                statsCanvas.blocksRaycasts = true;
                statsIsOpen = true;
            }
            UpdateAllStats();

        }
    }
    public void UpdateDamage()
    {
        statsSlots[0].GetComponentInChildren<TMP_Text>().text = "Damage:" + StatsManager.instance.GetDamage();
        //注意components是复数，会导致返回一个数组，不要拼错了
    }
    public void UpdateSpeed()
    {
        statsSlots[1].GetComponentInChildren<TMP_Text>().text = "Speed:" + StatsManager.instance.GetSpeed();
    }

    public void UpdateAllStats()
    {
        UpdateDamage();
        UpdateSpeed();
    }
}
