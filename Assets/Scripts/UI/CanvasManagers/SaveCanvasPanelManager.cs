using UnityEngine;

public class SaveCanvasPanelManager : MonoBehaviour, ICanvasManager
{
    [Header("Events To Receive")]
    public OpenSaveLoadPanelEventSO openSaveLoadPanelEvent;
    public ToggleCanvasEventSO toggleSaveLoadCanvasEvent;
    public ToggleCanvasEventSO ToggleCanvasEvent => toggleSaveLoadCanvasEvent;

    [Header("UI")]
    [SerializeField] private CanvasGroup saveCanvasGroup;
    [SerializeField] private Canvas canvas;

    private bool isPanelOpen = false;

    private void OnEnable()
    {
        openSaveLoadPanelEvent.OpenSaveLoadPanelEvent += OnPanelOpen;
        toggleSaveLoadCanvasEvent.toggleCanvasEvent += OnToggleCanvas;
    }

    private void OnDisable()
    {
        openSaveLoadPanelEvent.OpenSaveLoadPanelEvent -= OnPanelOpen;
        toggleSaveLoadCanvasEvent.toggleCanvasEvent -= OnToggleCanvas;
    }

    private void OnPanelOpen(MyEnums.SaveLoadPanelType panelType)
    {
        if (!isPanelOpen)
        {
            OpenPanel();
            if (panelType == MyEnums.SaveLoadPanelType.Save)
            {
                // TODO: 初始化保存面板
            }
            else if (panelType == MyEnums.SaveLoadPanelType.Load)
            {
                // TODO: 初始化读取面板
            }
            return;
        }
        ((ICanvasManager)this).RefreshCanvaOrder(
            canvas,
            MyEnums.CanvasToToggle.SaveLoad,
            isPanelOpen);
    }

    private void OnToggleCanvas(bool state)
    {
        if (state)
        {
            if (!isPanelOpen)
            {
                OpenPanel();
                return;
            }
            ((ICanvasManager)this).RefreshCanvaOrder(
                canvas,
                MyEnums.CanvasToToggle.SaveLoad,
                isPanelOpen);
        }
        else
        {
            ClosePanel();
        }
    }

    public void OpenPanel()
    {
        isPanelOpen = true;
        ((ICanvasManager)this).ToggleCanvas(saveCanvasGroup, canvas, MyEnums.CanvasToToggle.SaveLoad, true);
        UIManager.instance.HandleFocus(MyEnums.CanvasToToggle.SaveLoad);//非键盘按键唤起的画布，直接手动focus
    }

    public void ClosePanel()
    {
        isPanelOpen = false;
        ((ICanvasManager)this).ToggleCanvas(saveCanvasGroup, canvas, MyEnums.CanvasToToggle.SaveLoad, false);
    }
}
