using UnityEngine;

public class ESCMenuManager : MonoBehaviour
{
    //TODO 此处订阅一个事件，传递bool变量，用于切换画布，manager处按照按钮，使用枚举类和状态机调用对应事件,非枚举属性都设为disable
    public CanvasGroup ESCGroup;
    public ToggleCanvasEventSO toggleESCEvent;
    private void OnEnable()
    {
        toggleESCEvent.toggleCanvasEvent += OnESC;
    }
    private void OnDisable()
    {
        toggleESCEvent.toggleCanvasEvent -= OnESC;

    }
    private void OnESC(bool state)
    {

        if (state)
        {
            TimeManager.instance.PauseGame();
            ESCGroup.alpha = 1;
            ESCGroup.interactable = true;
            ESCGroup.blocksRaycasts = true;
        }
        else
        {
            TimeManager.instance.ResumeGame();
            ESCGroup.alpha = 0;
            ESCGroup.interactable = false;
            ESCGroup.blocksRaycasts = false;
        }
    }
}

