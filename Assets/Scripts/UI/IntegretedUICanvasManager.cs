using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class IntegretedUICanvasManager : MonoBehaviour
{
    public static IntegretedUICanvasManager instance;
    [SerializeField] private List<MyEnums.CanvasToToggle> canvasToToggle;
    [SerializeField] private CanvasGroup UICanvasPanel;
    [SerializeField] private List<Button> integretedButtons;
    [SerializeField] private Button toggleMenuButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;

    private TMP_Text toggleMenuText;
    private List<TMP_Text> integretedButtonTexts = new();

    private List<ToggleCanvasEventSO> UICanvasNeedIntegrete;
    private int currentPageNum = 0;
    private int buttonsEachPage;//用来模拟初始化多页的Panel
    private bool isMenuOpen = false;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
        InitiateUICanvasPanel(false);
        buttonsEachPage = integretedButtons.Count;
        UICanvasNeedIntegrete = UIManager.instance.GetToggleCanvasEventsList();

        InitiateButtons();
    }
    private void InitiateButtons()
    {
        toggleMenuButton.onClick.AddListener(OnClickMenuToggleButton);
        nextPageButton.onClick.AddListener(OnClickNextButton);
        prevPageButton.onClick.AddListener(OnClickPrevutton);

        toggleMenuText = toggleMenuButton.GetComponentInChildren<TMP_Text>();
        toggleMenuText.text = "Open";
        for (int i = 0; i < buttonsEachPage; i++)
        {
            integretedButtonTexts.Add(integretedButtons[i].GetComponentInChildren<TMP_Text>());
        }
    }
    private void InitiateUICanvasPanel(bool state)
    {
        isMenuOpen = state;
        SetCanvaState(UICanvasPanel, state);
        if (isMenuOpen) ShiftPage(0);
    }
    public void OnClickMenuToggleButton()
    {
        InitiateUICanvasPanel(!isMenuOpen);
        toggleMenuText.text = isMenuOpen ? "Open" : "Close";
    }
    public void OnClickNextButton()
    {
        ShiftPage(currentPageNum + 1);
    }

    public void OnClickPrevutton()
    {
        ShiftPage(currentPageNum - 1);
    }
    private bool ShiftPage(int page)
    {
        if (page < 0) page = 0;
        bool hasCanvasToDisPlay = false;
        int startNum = page * buttonsEachPage;
        int canvasNum = UICanvasNeedIntegrete.Count;
        if (startNum >= canvasNum)
            //大于等于的原因是因为startNum刚好对应第二页第一个的序号
            //例如4>=4的时候，实际上要显示的是第五个（从零开始），list正好没有
            hasCanvasToDisPlay = false;//没有能展示的画布组了
        else
        {
            InitiatePage(startNum, canvasNum);
            hasCanvasToDisPlay = true;
        }
        currentPageNum = page;
        return hasCanvasToDisPlay;
    }
    private void InitiatePage(int startNum, int canvasNum)
    {
        int buttonNum = (canvasNum + buttonsEachPage) / buttonsEachPage * buttonsEachPage;
        for (int i = startNum; i < buttonNum; i++)
        {
            int pageButtonNum = i == 0 ? 0 : i % buttonsEachPage;
            if (i < canvasNum)
            {
                integretedButtons[pageButtonNum].gameObject.SetActive(true);
                integretedButtonTexts[pageButtonNum].text = canvasToToggle[i].ToString();
            }
            else
            {
                integretedButtons[pageButtonNum].gameObject.SetActive(false);
            }
        }
    }
    private void SetCanvaState(CanvasGroup canva, bool state)
    {
        canva.alpha = state ? 1 : 0;
        canva.blocksRaycasts = state;
        canva.interactable = state;
    }
}
