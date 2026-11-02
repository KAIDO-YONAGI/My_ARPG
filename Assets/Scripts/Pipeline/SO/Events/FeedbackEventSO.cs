using System;
using Contracts.Audio;
using UnityEngine;

namespace Pipeline.SO.Events
{
    /// <summary>向音频管理器发布统一音效播放请求。</summary>
    [CreateAssetMenu(fileName = "FeedbackEventSO", menuName = "Events/FeedbackEventSO", order = 0)]
    public class FeedbackEventSO : ScriptableObject
    {
        public event Action<AudioRequest> AudioRequested;

        /// <summary>通知订阅的音频管理器处理一次播放请求。</summary>
        public void RequestAudio(AudioRequest request)
        {
            AudioRequested?.Invoke(request);
        }
    }
}
