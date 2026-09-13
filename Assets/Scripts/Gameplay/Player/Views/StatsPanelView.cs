using TMPro;
using UnityEngine;
using Gameplay.Player.Controllers;

namespace Gameplay.Player.Views
{
    /// <summary>
    /// 属性面板的显示层：唯一的场景组件，持有控件引用并托管 <see cref="StatsPanelController"/>；
    /// 兼管画布开关/焦点/层级（ICanvasManager 是全 UI 共用的基础设施，与数值数据流无关）。
    /// 订阅模型事件与取数在 Controller，View 不接触 PlayerStatsModel。
    /// </summary>
    public class StatsPanelView : MonoBehaviour, ICanvasManager
    {
        [SerializeField] private TMP_Text[] statTexts;
        [SerializeField] private CanvasGroup statsCanvas;
        [SerializeField] private Canvas canvas;

        [SerializeField] private ToggleCanvasEventSO toggleStatsEvent;
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

        public ToggleCanvasEventSO ToggleCanvasEvent => toggleStatsEvent;
        public SceneLoadedEventSO SceneLoadedEvent => sceneLoadedEvent;

        private StatsPanelController controller;

        private void Awake()
        {
            statsCanvas.alpha = 0;
        }

        private void Start()
        {
            // Start 在所有 Awake 之后：此时 StatsService 必然就绪，Controller 可以立即订阅
            controller = new StatsPanelController(this);
            controller.Refresh();
        }

        private void OnEnable()
        {
            // 重激活时补一次刷新：失活期间错过的事件没有累积通知
            controller?.Refresh();

            toggleStatsEvent.toggleCanvasEvent += OnToggleStatsEvent;
            toggleStatsEvent.focusEvent += OnFocus;
            sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
        }

        private void OnDisable()
        {
            toggleStatsEvent.toggleCanvasEvent -= OnToggleStatsEvent;
            toggleStatsEvent.focusEvent -= OnFocus;
            sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            controller?.Dispose();
            controller = null;
        }

        private void OnSceneLoaded(GameSceneSO _)
        {
            ((ICanvasManager)this).SetCanvaInactive(statsCanvas, MyEnums.CanvasToToggle.Stats);
        }

        private void OnToggleStatsEvent(bool state)
        {
            controller?.Refresh(); // 显示前刷新数据，数据由 StatsPanelController 推入
            ((ICanvasManager)this).ToggleCanvas(statsCanvas, canvas, MyEnums.CanvasToToggle.Stats, state);
        }

        private void OnFocus()
        {
            if (!canvasIsActive()) return;
            ((ICanvasManager)this).RefreshCanvaOrder(canvas, MyEnums.CanvasToToggle.Stats, true);
        }

        private bool canvasIsActive()
        {
            return statsCanvas.alpha > 0;
        }

        /// <summary>把当前属性画到面板：statTexts[0]=Damage，[1]=Speed。</summary>
        public void SetStats(int damage, float speed)
        {
            if (statTexts == null || statTexts.Length < 1) return;
            if (statTexts[0] != null) statTexts[0].text = "Damage:" + damage;
            if (statTexts.Length >= 2 && statTexts[1] != null) statTexts[1].text = "Speed:" + speed;
        }
    }
}
