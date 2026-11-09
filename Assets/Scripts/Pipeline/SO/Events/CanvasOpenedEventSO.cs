using System;
using Contracts.Enums;
using UnityEngine;

namespace Pipeline.SO.Events
{
    /// <summary>
    /// 画布打开事件通道。UIManager 确认某个画布已经打开后发布，
    /// 缓存据此刷新界面的最近使用顺序。
    /// </summary>
    [CreateAssetMenu(fileName = "CanvasOpenedEvent", menuName = "Events/Canvas Opened")]
    public sealed class CanvasOpenedEventSO : ScriptableObject
    {
        /// <summary>画布打开后触发，参数是刚打开的界面身份。</summary>
        public event Action<CanvasType> CanvasOpened;

        /// <summary>发布一次“画布已打开”通知；没有订阅者时安全返回。</summary>
        public void RaiseCanvasOpened(CanvasType canvas)
        {
            CanvasOpened?.Invoke(canvas);
        }
    }
}
