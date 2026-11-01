using System;
using Contracts.Enums;
using Contracts.Interfaces;
using Pipeline.SO.Events;
using UnityEngine;

namespace Contracts.Base
{
    /// <summary>
    /// 可被 <see cref="UIViewInstanceCache"/> 缓存的普通界面基类。
    /// 负责“打开时激活并绑定、关闭时退订并隐藏、销毁前收尾”的固定生命周期，
    /// 业务界面只实现 <see cref="OnOpen"/>、<see cref="OnClose"/>、<see cref="OnCacheDestroy"/>。
    /// </summary>
    /// <remarks>
    /// 界面实例可能被反复打开和关闭，也可能在闲置超时后被销毁。
    /// 因此打开期间产生的订阅、监听和临时对象必须在关闭时释放，
    /// 不能在字段初始化或 <c>Awake</c> 里长期持有业务事件。
    /// </remarks>
    public abstract class CachedUIViewBase : MonoBehaviour, ICanvasManager
    {
        /// <summary>该界面绑定的开关事件；缓存按它的事件通道接收显隐请求。</summary>
        public abstract ToggleCanvasEventSO ToggleEvent { get; }

        ToggleCanvasEventSO ICanvasManager.ToggleCanvasEvent => ToggleEvent;
        SceneLoadedEventSO ICanvasManager.SceneLoadedEvent => null;

        /// <summary>界面身份。默认取开关事件上的枚举，预制体也可以单独覆写。</summary>
        public virtual CanvasToToggle CanvasId =>
            ToggleEvent != null ? ToggleEvent.canvasToToggle : CanvasToToggle.Default;

        /// <summary>是否由开关事件直接打开；需要先准备业务数据的界面交给管理器打开，关闭请求始终响应。</summary>
        public virtual bool OpenOnToggleEvent => true;

        /// <summary>显示排序使用的 Canvas。</summary>
        protected abstract Canvas ViewCanvas { get; }

        /// <summary>控制透明度、交互和射线阻挡的 CanvasGroup。</summary>
        protected abstract CanvasGroup ViewGroup { get; }

        /// <summary>按 ESC 时是否允许关闭本界面。</summary>
        public virtual bool CloseOnEscape => true;

        /// <summary>打开期间是否阻断底层界面的全局输入。</summary>
        public virtual bool BlocksGlobalInput => false;

        /// <summary>当前是否处于打开状态；关闭后实例仍可能留在缓存中。</summary>
        public bool IsOpen { get; private set; }

        /// <summary>关闭完成后的通知，仅供缓存内部维护闲置队列，业务代码不要订阅。</summary>
        internal event Action Closed;

        /// <summary>
        /// 打开界面：先询问 <see cref="CanOpen"/>，再激活对象并执行 <see cref="OnOpen"/>，
        /// 最后统一设置可见性。打开过程抛异常时回滚为关闭状态，避免留下半初始化界面。
        /// </summary>
        /// <returns>已经打开或本次打开成功时返回 true。</returns>
        internal bool Open()
        {
            if (IsOpen) return true;
            if (!CanOpen()) return false;

            IsOpen = true;
            try
            {
                gameObject.SetActive(true);
                OnOpen();
                ToggleCanvasLifecycle(true);
                return true;
            }
            catch
            {
                Close();
                throw;
            }
        }

        /// <summary>缓存实例化前检查业务门槛，避免拒绝打开的界面触发 Awake。</summary>
        internal bool CanOpenFromCache() => IsOpen || CanOpen();

        /// <summary>
        /// 关闭界面：先给业务一次退订机会，再统一隐藏、还原排序并停用对象。
        /// 即使 <see cref="OnClose"/> 抛异常，也必须完成隐藏和清理，防止界面卡在打开状态。
        /// </summary>
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
#if UNITY_EDITOR
                Debug.LogException(exception, this);
#endif
            }
            finally
            {
                ToggleCanvasLifecycle(false);
                gameObject.SetActive(false);
                Closed?.Invoke();
            }
        }

        /// <summary>焦点栈变化时刷新画布排序。</summary>
        internal void RefreshOrder()
        {
            ((ICanvasManager)this).RefreshCanvaOrder(ViewCanvas, CanvasId, true);
        }

        /// <summary>缓存销毁实例前的最后清理入口；异常只记录，不阻断销毁流程。</summary>
        internal void BeforeCacheDestroy()
        {
            try
            {
                OnCacheDestroy();
            }
            catch (Exception exception)
            {
#if UNITY_EDITOR
                Debug.LogException(exception, this);
#endif
            }
        }

        /// <summary>复用画布管理契约统一更新显隐、输入状态、焦点和排序。</summary>
        private void ToggleCanvasLifecycle(bool state)
        {
            ((ICanvasManager)this).ToggleCanvas(ViewGroup, ViewCanvas, CanvasId, state);
        }

        /// <summary>
        /// 父节点停用、组件被关闭或场景卸载时，Unity 不会调用 Close，
        /// 这里补一次收尾，保证业务订阅和焦点状态不会残留。
        /// </summary>
        protected virtual void OnDisable()
        {
            Close();
        }

        /// <summary>界面打开后执行：绑定数据、刷新显示、订阅事件。</summary>
        protected virtual void OnOpen() { }

        /// <summary>界面关闭前执行：退订事件、释放临时对象、恢复被改动的显示状态。</summary>
        protected virtual void OnClose() { }

        /// <summary>实例即将被缓存销毁时执行：保存需要跨实例保留的业务状态。</summary>
        protected virtual void OnCacheDestroy() { }

        /// <summary>是否允许打开；缓存会在实例化前查询，重写只检查条件，不执行副作用。</summary>
        protected virtual bool CanOpen() => true;
    }
}
