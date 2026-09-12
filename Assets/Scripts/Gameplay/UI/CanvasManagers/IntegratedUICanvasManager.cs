using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntegratedUICanvasManager : YSingleton<IntegratedUICanvasManager>,ICanvasManager
{
    [SerializeField] private List<MyEnums.CanvasToToggle> canvasToToggle;//用枚举类来指定需要切换的画布组
    [SerializeField] private CanvasGroup UICanvasPanel;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Button toggleMenuButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;
    [SerializeField] private TMP_Text pageNumText;
    [SerializeField] private TMP_Text toggleMenuText;
    [SerializeField] private Button[] integratedButtons;
    [SerializeField] private TMP_Text[] integratedButtonTexts;
    [SerializeField] private ToggleCanvasEventSO toggleIntegratedCanvasEventSO;
    [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;
    public ToggleCanvasEventSO ToggleCanvasEvent => toggleIntegratedCanvasEventSO;
    public SceneLoadedEventSO SceneLoadedEvent => sceneLoadedEvent;

    private int currentPageNum = 0;
    private int buttonsEachPage;//用来模拟初始化多页的Panel
    private bool isMenuOpen = false;

    protected override void OnSingletonInitialized()
    {
        buttonsEachPage = integratedButtons != null ? integratedButtons.Length : 0;

        //按钮监听与文本引用均为固定 UI 结构，一次性注册即可（不随 OnEnable 重复注册/堆积）
        toggleMenuButton.onClick.AddListener(OnClickMenuToggleButton);
        nextPageButton.onClick.AddListener(OnClickNextButton);
        prevPageButton.onClick.AddListener(OnClickPrevButton);

        InitiateUICanvasPanel(false);
    }
    private void OnEnable()
    {
        toggleIntegratedCanvasEventSO.toggleCanvasEvent += OnToggleIntegratedCanvas;
        toggleIntegratedCanvasEventSO.focusEvent += OnFocus;
        sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;

        //此处事件在UIManager里仅索引到editor里，没有在代码层编写
        //特别地，将开闭功能都放在当前这个脚本里
        //最终效果就是UIManger里可以依靠alpha值检测来实现互斥关闭集成面板，而这个脚本直接实现开关
    }
    private void OnDisable()
    {
        toggleIntegratedCanvasEventSO.toggleCanvasEvent -= OnToggleIntegratedCanvas;
        toggleIntegratedCanvasEventSO.focusEvent -= OnFocus;
        sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
    }

    private void OnSceneLoaded(GameSceneSO _)
    {
        InitiateUICanvasPanel(false);
    }

    private void OnFocus()
    {
        if (!isMenuOpen) return;
        ((ICanvasManager)this).RefreshCanvaOrder(canvas, MyEnums.CanvasToToggle.Integrated, true);
    }
    private void OnToggleIntegratedCanvas(bool state)
    {
        SetCanvaState(UICanvasPanel, state);

        isMenuOpen = state;

        if (toggleMenuText != null)
            toggleMenuText.text = !isMenuOpen ? "Open" : "Close";

    }

    private void InitiateUICanvasPanel(bool state)
    {
        if (pageNumText != null)
            pageNumText.text = "1";
        isMenuOpen = state;
        SetCanvaState(UICanvasPanel, isMenuOpen);
        if (isMenuOpen) ShiftPage(0);
    }
    private void OnClickMenuToggleButton()
    {
        InitiateUICanvasPanel(!isMenuOpen);
        if (toggleMenuText != null)
            toggleMenuText.text = !isMenuOpen ? "Open" : "Close";
    }

    private void OnClickNextButton()
    {
        ShiftPage(currentPageNum + 1);
    }

    private void OnClickPrevButton()
    {
        ShiftPage(currentPageNum - 1);
    }
    private bool ShiftPage(int page)
    {
        int canvasNum = canvasToToggle.Count;

        if (page < 0) page = 0;

        bool hasCanvasToDisPlay = false;
        int startNum = page * buttonsEachPage;
        if (startNum >= canvasNum)
            //大于等于的原因是因为startNum刚好对应第二页第一个的序号
            //例如4>=4的时候，实际上要显示的是第五个（从零开始），list正好没有
            hasCanvasToDisPlay = false;//没有能展示的画布组了
        else
        {
            InitiatePage(startNum, canvasNum);
            hasCanvasToDisPlay = true;
        }
        if (hasCanvasToDisPlay)
            currentPageNum = page;
        return hasCanvasToDisPlay;
    }
    private void InitiatePage(int startNum, int canvasNum)
    {
        if (pageNumText != null)
            pageNumText.text = ((startNum / buttonsEachPage) + 1).ToString();
        foreach (var button in integratedButtons)
        {
            if (button == null) continue;
            button.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
        }

        for (int i = startNum, count = 0; i < canvasNum && count < buttonsEachPage; i++, count++)
        {
            int pageButtonNum = i % buttonsEachPage;
            MyEnums.CanvasToToggle canvasToToggle = this.canvasToToggle[i];
            Button button = integratedButtons[pageButtonNum];
            if (button == null) continue;

            if (integratedButtonTexts != null && pageButtonNum < integratedButtonTexts.Length
                && integratedButtonTexts[pageButtonNum] != null)
            {
                integratedButtonTexts[pageButtonNum].text = canvasToToggle.ToString();
            }

            button.gameObject.SetActive(true);
            button.onClick.AddListener(() => OnIntegratedButtonClick(canvasToToggle));

        }
    }
    private void OnIntegratedButtonClick(MyEnums.CanvasToToggle canvasToToggle)
    {
        UIManager.Instance.RequestCanvasToggle(canvasToToggle);
    }

    private void SetCanvaState(CanvasGroup canva, bool state)
    {
        canva.alpha = state ? 1 : 0;
        canva.blocksRaycasts = state;
        canva.interactable = state;
        if (UIManager.Instance != null)
            UIManager.Instance.ReportCanvasState(MyEnums.CanvasToToggle.Integrated, state);
    }
}
