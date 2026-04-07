using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;
    public  bool IsGamePaused;
    private int numberOfPauses = 0;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void PauseGame()//需要调用脚本配合使用bool变量保证只能暂停一次，不能重复调用导致时间缩放异常
    {
        Time.timeScale = 0;
        numberOfPauses++; 
        IsGamePaused = true;

    }

    public void ResumeGame()
    {
        
        numberOfPauses--;
        if (numberOfPauses < 0) numberOfPauses = 0;
        if (numberOfPauses == 0)
        {
            Time.timeScale = 1;
            IsGamePaused = false;
        }
    }
}
