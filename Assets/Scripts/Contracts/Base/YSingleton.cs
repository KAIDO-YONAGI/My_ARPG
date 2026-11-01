using System;
using UnityEngine;

namespace Contracts.Base
{
    /// <summary>
    /// 抽象单例基类：提供只读 Instance 访问、重复实例销毁、OnDestroy 复位。
    /// 用法：class Foo : YSingleton&lt;Foo&gt; { }
    /// 需要在单例注册后执行额外初始化时，重写 OnSingletonInitialized()。
    /// 各泛型闭包（YSingleton&lt;Foo&gt; / YSingleton&lt;Bar&gt;）的静态 Instance 相互独立。
    /// </summary>
    public abstract class YSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>当前注册的实例；未注册或已销毁时为 null。</summary>
        public static T Instance { get; private set; }

        /// <summary>获取必需的单例；未注册或已销毁时明确报告依赖缺失。</summary>
        public static T RequiredInstance => Instance != null
            ? Instance
            : throw new InvalidOperationException(
                $"必需的 {typeof(T).Name} 单例尚未初始化。");

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false; // 立即停用：Destroy 有延迟，避免销毁前 Update 继续在未初始化实例上运行
                Destroy(this);
                return;
            }
            Instance = this as T;
            OnSingletonInitialized();
        }

        /// <summary>子类初始化钩子：仅在合法实例的 Awake 中调用一次</summary>
        protected virtual void OnSingletonInitialized() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
