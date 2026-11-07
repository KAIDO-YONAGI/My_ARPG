using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Audio;
using Contracts.Enums;
using Pipeline.SO.Events;
using UnityEngine;
using AudioType = Contracts.Audio.AudioType;

namespace Pipeline.Audio
{
    public class UIAudioManager : MonoBehaviour
    {
        /// <summary>画布与打开音效的绑定：某个画布打开时播放登记的音效类型。</summary>
        [Serializable]
        public class CanvasAudioBinding
        {
            [Tooltip("打开时需要播放音效的画布。")] public CanvasToToggle canvas;

            [Tooltip("该画布打开时播放的音效类型，片段由 AudioConfigSO 按类型选取。")]
            public AudioType audioType;
        }

        [SerializeField] private FeedbackEventSO feedbackEvent;
        [SerializeField] private CanvasOpenedEventSO canvasOpenedEvent;

        [Tooltip("画布组与音频的打开关系：每个画布登记一条，未登记的画布打开时不播放音效。")] [SerializeField]
        private List<CanvasAudioBinding> canvasAudioBindings = new();

        private Dictionary<CanvasToToggle, AudioType> canvasAudioBindingDict;

        private void Awake()
        {
            canvasAudioBindingDict = canvasAudioBindings.ToDictionary(
                a => a.canvas,
                a => a.audioType
            );
        }

        private void OnEnable()
        {
            canvasOpenedEvent.CanvasOpened += OnUIAudio;
        }

        void OnDisable()
        {
            canvasOpenedEvent.CanvasOpened -= OnUIAudio;
        }

        private void OnUIAudio(CanvasToToggle canvas)
        {
            if (!canvasAudioBindingDict.TryGetValue(canvas, out AudioType audioType)) return;

            feedbackEvent.RequestAudio(new AudioRequest(audioType));
        }
    }
}
