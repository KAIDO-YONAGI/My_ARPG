using Contracts;
using TMPro;
using UnityEngine;
using Gameplay.Player.Controllers;
using Gameplay.Player.Services;
using Pipeline.SO;
using Pipeline.SO.Events;
using Pipeline.UI.SystemCanvasManagers;

namespace Gameplay.Player.Views
{
    /// <summary>
    /// 属性面板的显示层。预制体是 StatsCanvas.prefab，由界面缓存按需创建；
    /// 打开时创建 <see cref="StatsCanvasController"/> 并首刷，关闭时退订并恢复拖动位置。
    /// </summary>
    /// <remarks>
    /// 数值状态保存在 <see cref="StatsService"/>，视图只负责显示；
    /// 面板被缓存销毁后重新打开，会重新读取当前数值，不会丢失属性进度。
    /// </remarks>
    public class StatsCanvasView : CachedUIView
    {
        [Header("属性文本")]
        [Tooltip("属性文本数组：0 是伤害，1 是速度。")]
        [SerializeField] private TMP_Text[] statTexts;

        [Header("画布控制")]
        [Tooltip("面板根节点的 CanvasGroup，控制显隐与交互。")]
        [SerializeField] private CanvasGroup statsCanvas;

        [Tooltip("面板根节点的 Canvas，控制排序层级。")]
        [SerializeField] private Canvas canvas;

        [Header("事件与拖动")]
        [Tooltip("属性面板的开关事件。")]
        [SerializeField] private ToggleCanvasEventSO toggleStatsEvent;

        [Tooltip("场景加载完成后自动关闭面板，避免跨场景残留。")]
        [SerializeField] private SceneLoadedEventSO sceneLoadedEvent;

        [Tooltip("可拖动的面板内容节点，关闭时恢复打开前的位置。")]
        [SerializeField] private RectTransform statsContent;

        public override ToggleCanvasEventSO ToggleEvent => toggleStatsEvent;
        protected override Canvas ViewCanvas => canvas;
        protected override CanvasGroup ViewGroup => statsCanvas;

        private StatsCanvasController controller;
        private Vector2 originPosition;
        private bool positionCaptured;

        /// <summary>数值服务尚未初始化时不打开，避免控制器拿到空模型。</summary>
        protected override bool CanOpen()
        {
            return StatsService.Instance != null;
        }

        /// <summary>打开时记录拖动起点、创建控制器并订阅场景加载事件。</summary>
        protected override void OnOpen()
        {
            if (!positionCaptured && statsContent != null)
            {
                originPosition = statsContent.anchoredPosition;
                positionCaptured = true;
            }

            controller = new StatsCanvasController(this);
            controller.Refresh();
            if (sceneLoadedEvent != null)
                sceneLoadedEvent.SceneLoadedEvent += OnSceneLoaded;
        }

        /// <summary>关闭时退订、释放控制器，并把面板恢复到打开前的位置。</summary>
        protected override void OnClose()
        {
            if (sceneLoadedEvent != null)
                sceneLoadedEvent.SceneLoadedEvent -= OnSceneLoaded;
            controller?.Dispose();
            controller = null;

            // 停用后 UIDrag.LateUpdate 不再运行，因此在这里恢复拖动前的位置。
            if (positionCaptured && statsContent != null)
                statsContent.anchoredPosition = originPosition;
        }

        /// <summary>切场景时请求关闭属性面板；真正的关闭仍走 UIManager 的统一流程。</summary>
        private void OnSceneLoaded(GameSceneSO _)
        {
            UIManager.Instance?.RequestCanvasClose(CanvasToToggle.Stats);
        }

        /// <summary>把当前属性画到面板：statTexts[0] 是伤害，statTexts[1] 是速度。</summary>
        public void SetStats(int damage, float speed)
        {
            if (statTexts == null || statTexts.Length < 1) return;
            if (statTexts[0] != null) statTexts[0].text = "Damage:" + damage;
            if (statTexts.Length >= 2 && statTexts[1] != null) statTexts[1].text = "Speed:" + speed;
        }
    }
}
