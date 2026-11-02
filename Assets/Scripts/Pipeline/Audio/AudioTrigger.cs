using System;
using Contracts.Audio;
using Pipeline.SO.Events;
using UnityEngine;
using AudioType = Contracts.Audio.AudioType;

namespace Pipeline.Audio
{
    /// <summary>供按钮、动画事件与场景交互调用，通过共享事件资产请求播放音效。</summary>
    public class AudioTrigger : MonoBehaviour
    {
        [SerializeField] private FeedbackEventSO audioRequestEvent;
        [SerializeField] private AudioType audioType;
        [Tooltip("启用时在本对象当前世界坐标播放 3D 音效，否则播放 2D 音效。")]
        [SerializeField] private bool useWorldPosition;

        private void Awake()
        {
            if (audioRequestEvent == null)
                throw new InvalidOperationException($"[{nameof(AudioTrigger)}] audioRequestEvent 未指定。");
        }

        /// <summary>可绑定 UnityEvent 或动画事件，每次调用读取对象的当前世界坐标。</summary>
        public void Play()
        {
            Vector3? position = useWorldPosition ? transform.position : (Vector3?)null;
            audioRequestEvent.RequestAudio(new AudioRequest(audioType, position));
        }
    }
}
