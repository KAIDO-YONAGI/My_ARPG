using System;
using Contracts;
using Pipeline.SO.Events;
using Pipeline.UI.SystemCanvasManagers;
using UnityEngine;

namespace Pipeline.UI.Runtime
{
    /// <summary>由 UIViewCache 托管的普通界面；业务订阅只存在于打开期间。</summary>
    public abstract class CachedUIView : MonoBehaviour
    {
        public abstract ToggleCanvasEventSO ToggleEvent { get; }
        protected abstract Canvas ViewCanvas { get; }
        protected abstract CanvasGroup ViewGroup { get; }
        public virtual bool CloseOnEscape => true;
        public virtual bool BlocksGlobalInput => false;
        public bool IsOpen { get; private set; }

        internal event Action Closed;

        internal void Open()
        {
            if (IsOpen) return;

            IsOpen = true;
            try
            {
                gameObject.SetActive(true);
                OnOpen();
                SetVisible(true);
            }
            catch
            {
                Close();
                throw;
            }
        }

        internal void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            try
            {
                OnClose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                SetVisible(false);
                SetOrder(UIManager.DefaultOrder);
                gameObject.SetActive(false);
                Closed?.Invoke();
            }
        }

        internal void SetOrder(int order)
        {
            if (ViewCanvas != null) ViewCanvas.sortingOrder = order;
        }

        private void SetVisible(bool state)
        {
            ViewGroup.alpha = state ? 1 : 0;
            ViewGroup.interactable = state;
            ViewGroup.blocksRaycasts = state;
        }

        protected virtual void OnDisable()
        {
            // 父节点停用或组件被关闭时，也必须解绑并同步焦点状态。
            Close();
        }

        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
    }
}
