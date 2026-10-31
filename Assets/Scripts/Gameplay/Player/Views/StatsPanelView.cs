using Contracts;
using TMPro;
using UnityEngine;
using Gameplay.Player.Controllers;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.Runtime;
using Pipeline.UI.SystemCanvasManagers;

namespace Gameplay.Player.Views
{
    /// <summary>
    /// 属性面板的显示层。由 UIManager 的预制体缓存创建；打开时绑定，关闭时退订。
    /// </summary>
    public class StatsPanelView : CachedUIView
    {
        [SerializeField] private TMP_Text[] statTexts;
        [SerializeField] private CanvasGroup statsCanvas;
        [SerializeField] private Canvas canvas;

        [SerializeField] private ToggleCanvasEventSO toggleStatsEvent;
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;
        [SerializeField] private RectTransform statsContent;

        public override ToggleCanvasEventSO ToggleEvent => toggleStatsEvent;
        protected override Canvas ViewCanvas => canvas;
        protected override CanvasGroup ViewGroup => statsCanvas;

        private StatsPanelController controller;
        private Vector2 originPosition;
        private bool positionCaptured;

        protected override void OnOpen()
        {
            if (!positionCaptured && statsContent != null)
            {
                originPosition = statsContent.anchoredPosition;
                positionCaptured = true;
            }
            controller = new StatsPanelController(this);
            controller.Refresh();
            sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
        }

        protected override void OnClose()
        {
            sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
            controller?.Dispose();
            controller = null;
            // 停用后 UIDrag.LateUpdate 不再运行，因此在这里恢复拖动前的位置。
            if (positionCaptured && statsContent != null)
                statsContent.anchoredPosition = originPosition;
        }

        private void OnSceneLoaded(GameSceneSO _)
        {
            UIManager.Instance?.RequestCanvasClose(CanvasToToggle.Stats);
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
