using System;
using System.Collections;
using System.Collections.Generic;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Rest.Model;
using Gemelo.Voice.Streaming;
using UnityEngine;
using UnityEngine.UI;

namespace Gemelo.Voice.Samples.Streaming
{
    public class StreamingExample : MonoBehaviour
    {
        [Serializable]
        public struct VoiceDb
        {
            public string Text;
            public VoiceType Type;
            public int VoiceId;
        }

        [Header("Analysis")] [SerializeField, Range(0.2f, 1f)]
        private float boost = 0.75f;

        [SerializeField] private int samplesPerFrame = 256;

        [Header("UI references")] [SerializeField]
        private Text textToSpeechText;

        [SerializeField] private Text voiceIdText;
        [SerializeField] private Button startButton;
        [SerializeField] private Toggle autoplayToggle;
        [SerializeField] private Slider analyzerSlider;
        [SerializeField] private Toggle useMp3Toggle;
        [SerializeField] AudioStreamingManager streamingManager;

        [SerializeField] private List<VoiceDb> texts = new List<VoiceDb>()
        {
            new VoiceDb()
            {
                Text = "Hello from Charactr Software Development Kit for Unity.",
                VoiceId = 181,
            },
        };

        private IAudioPlayer _audioPlayer;

        private void Awake()
        {
            startButton.onClick.AddListener(() => StartCoroutine(StartVoiceStreaming()));
        }

        IEnumerator StartVoiceStreaming()
        {
            //Cache average provider settings, and sample size (smaller == faster update)
            _audioPlayer = streamingManager.InitializePlayer(new AverageProvider(boost), samplesPerFrame);
            streamingManager.SetAudioDataType(useMp3Toggle.isOn ? AudioDataType.MP3 : AudioDataType.WAV);

            var buttonText = startButton.GetComponentInChildren<Text>();
            var count = autoplayToggle.isOn ? texts.Count : 1;
           
            buttonText.text = "Playing...";

            for (int i = 0; i < count; i++)
            {
                Debug.Log("Play next: " + i);
                yield return PlayNext(i);
            }

            buttonText.text = "Start";
        }

        private void Update()
        {
            if (_audioPlayer != null && _audioPlayer.IsPlaying)
            {
                analyzerSlider.value = _audioPlayer.GetSampleAverage();
            }
            else
            {
                analyzerSlider.value = 0f;
            }
        }

        private IEnumerator PlayNext(int i)
        {
            var current = texts[i];

            textToSpeechText.text = $"Text: {current.Text}";
            voiceIdText.text = $"VoiceID: {current.VoiceId} [{current.Type}]";

            yield return StreamTextToSpeech(current);
        }

        private IEnumerator StreamTextToSpeech(VoiceDb voiceDb)
        {
            streamingManager.SetVoiceId(voiceDb.VoiceId);
            streamingManager.SetVoiceType(voiceDb.Type);
            yield return streamingManager.ConvertAndStartPlaying(voiceDb.Text);
        }
    }
}
