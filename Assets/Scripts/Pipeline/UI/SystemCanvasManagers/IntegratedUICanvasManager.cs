using System.Collections.Generic;
using Contracts.Base;
using Contracts.Enums;
using Contracts.Interfaces;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.RedDot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pipeline.UI.SystemCanvasManagers
{
    public class IntegratedUICanvasManager : YSingleton<IntegratedUICanvasManager>,ICanvasManager
    {
        [SerializeField] private List<CanvasType> canvasToToggle;//用枚举类来指定需要切换的画布组
        [SerializeField] private CanvasGroup UICanvasPanel;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Button toggleMenuButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private TMP_Text pageNumText;
        [SerializeField] private TMP_Text toggleMenuText;
        [SerializeField] private Button[] integratedButtons;
        [SerializeField] private TMP_Text[] integratedButtonTexts;

        [Tooltip("OpenMenu 按钮上的根红点表现。")]
        [SerializeField]
        private Image toggleMenuRedDot;

        [Tooltip("分页按钮各自的红点表现，顺序与 integratedButtons 一致。")]
        [SerializeField]
        private Image[] integratedButtonRedDots;

        [SerializeField] private ToggleCanvasEventSO toggleIntegratedCanvasEventSO;
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;
        public ToggleCanvasEventSO ToggleCanvasEvent => toggleIntegratedCanvasEventSO;
        public SceneLoadedEventSO SceneLoadedEvent => sceneLoadedEvent;

        private int currentPageNum = 0;
        private int buttonsEachPage;//用来模拟初始化多页的Panel
        private bool isMenuOpen = false;

        // 当前页已绑定红点的画布；翻页前按这份记录解绑，避免旧页面板残留引用。
        private readonly List<CanvasType> boundRedDotCanvases = new();

        protected override void OnSingletonInitialized()
        {
            buttonsEachPage = integratedButtons != null ? integratedButtons.Length : 0;

            //按钮监听与文本引用均为固定 UI 结构，一次性注册即可（不随 OnEnable 重复注册/堆积）
            toggleMenuButton.onClick.AddListener(OnClickMenuToggleButton);
            nextPageButton.onClick.AddListener(OnClickNextButton);
            prevPageButton.onClick.AddListener(OnClickPrevButton);

            InitiateUICanvasPanel(false);
        }

        private void Start()
        {
            // 红点管理器与本组件同在常驻场景，Awake 顺序不保证，因此在 Start 绑定一次。
            RedDotManager redDot = RedDotManager.Instance;
            if (redDot == null) return;

            redDot.BindRootImage(toggleMenuRedDot);
            redDot.SetNavigationOrder(canvasToToggle);
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
            ((ICanvasManager)this).RefreshCanvaOrder(canvas, CanvasType.Integrated, true);
        }
        private void OnToggleIntegratedCanvas(bool state)
        {
            ((ICanvasManager)this).SetCanvaState(UICanvasPanel, CanvasType.Integrated, state);

            isMenuOpen = state;

            if (toggleMenuText != null)
                toggleMenuText.text = !isMenuOpen ? "Open" : "Close";

        }

        private void InitiateUICanvasPanel(bool state)
        {
            if (pageNumText != null)
                pageNumText.text = "1";
            isMenuOpen = state;
            ((ICanvasManager)this).SetCanvaState(UICanvasPanel, CanvasType.Integrated, isMenuOpen);
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
            foreach (Button button in integratedButtons)
            {
                if (button == null) continue;
                button.gameObject.SetActive(false);
                button.onClick.RemoveAllListeners();
            }

            for (int i = startNum, count = 0; i < canvasNum && count < buttonsEachPage; i++, count++)
            {
                int pageButtonNum = i % buttonsEachPage;
                CanvasType canvasType = this.canvasToToggle[i];
                Button button = integratedButtons[pageButtonNum];
                if (button == null) continue;

                if (integratedButtonTexts != null && pageButtonNum < integratedButtonTexts.Length
                                                  && integratedButtonTexts[pageButtonNum] != null)
                {
                    integratedButtonTexts[pageButtonNum].text = canvasType.ToString();
                }

                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => OnIntegratedButtonClick(canvasType));

            }

            // 按钮在翻页间复用，红点必须按当前页画布重新绑定。
            BindPageRedDots(startNum, canvasNum);
        }

        /// <summary>
        /// 分页复用按钮时先解绑旧分支，再绑定当前页画布分支；没有分支的按钮隐藏红点。
        /// </summary>
        /// <param name="startNum">本页第一个画布在 canvasToToggle 中的下标。</param>
        /// <param name="canvasNum">画布总数，用于判断本页是否还有可显示的画布。</param>
        private void BindPageRedDots(int startNum, int canvasNum)
        {
            UnbindPageRedDots();

            RedDotManager redDot = RedDotManager.Instance;
            if (redDot == null) return;

            for (int i = startNum, buttonNum = 0;
                 i < canvasNum && buttonNum < integratedButtons.Length;
                 i++, buttonNum++)
            {
                Image redDotImage = GetButtonRedDotImage(buttonNum);
                if (redDotImage == null) continue;

                CanvasType canvasType = canvasToToggle[i];
                redDot.BindCanvasImage(canvasType, redDotImage);
                boundRedDotCanvases.Add(canvasType);
            }
        }

        // 解除上一页已绑定的画布，避免复用后的按钮继续显示旧页画布的红点。
        private void UnbindPageRedDots()
        {
            RedDotManager redDot = RedDotManager.Instance;
            if (redDot != null)
            {
                foreach (CanvasType canvas in boundRedDotCanvases)
                    redDot.UnbindCanvasImage(canvas);
            }

            boundRedDotCanvases.Clear();
        }

        // 按按钮序号取红点图片；红点数组未配满时返回 null，由调用方跳过该按钮。
        private Image GetButtonRedDotImage(int buttonNum)
        {
            if (integratedButtonRedDots == null || buttonNum >= integratedButtonRedDots.Length) return null;

            return integratedButtonRedDots[buttonNum];
        }
        private void OnIntegratedButtonClick(CanvasType canvasType)
        {
            UIManager.Instance.RequestCanvasToggle(canvasType);
        }
    }
}
