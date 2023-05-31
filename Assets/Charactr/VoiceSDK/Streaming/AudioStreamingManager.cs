using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioStreamingManager: MonoBehaviour
    {
        public const string URL = "wss://api.slowpoke.charactr.dev/v1/tts/stream/simplex/ws";
        public AudioClip AudioClip { get; private set; }
        public bool AudioEnd { get; private set; }
        public event Action OnAudioEnd;
        public event Action OnAudioReady;
      
        [SerializeField] private int voiceId = 151;

        private IAudioStreamingClient _streamingClient;
        private Configuration _configuration;
        private Queue<Action> _actions;

        private void Awake()
        {
            _configuration = Configuration.LoadStreaming();
            
            if (_configuration == null)
                throw new Exception("Can't load Configuration data");
        }

        public void SetVoiceId(int voice)
        {
            voiceId = voice;
        }
        
        public IEnumerator ConvertAndStartPlaying(string text)
        {
            yield return CreateClientInstance(text, _configuration);
            yield return Play();
        }
        
        public IEnumerator Convert(string text)
        {
            yield return CreateClientInstance(text, _configuration);
        }

        private IEnumerator CreateClientInstance(string text, Configuration configuration)
        {
            var url = URL + $"?voiceId={voiceId}";
            
            var audioSource = GetComponent<AudioSource>();
            audioSource.Stop();
            
#if UNITY_WEBGL && !UNITY_EDITOR
            _streamingClient = new WebGlAudioStreamingClient(url, configuration, audioSource);
#else
            _streamingClient = new DefaultAudioStreamingClient(url, configuration, audioSource);
#endif
            _streamingClient.Connect();

            _streamingClient.SendConvertCommand(text);

            yield return new WaitUntil(() => _streamingClient.Initialized);
            
            AudioClip = _streamingClient.AudioClip;
            OnAudioReady?.Invoke();
        }

        void OnDestroy()
        {
            DisposeClient();
        }

        private void Update()
        {
            if (_streamingClient != null)
            {
                _streamingClient.DepleteBufferQueue();
                AudioEnd = CheckForAudioEnd(_streamingClient);
            }
        }

        public IEnumerator Play()
        {
            AudioEnd = false;
            _streamingClient?.Play();
            yield return new WaitUntil(() => AudioEnd);
        }

        private void DisposeClient()
        {
            _streamingClient?.Dispose();
            _streamingClient = null;
        }

        private bool CheckForAudioEnd(IAudioStreamingClient client)
        {
            if (!client.BufferingCompleted)
                return false;

            var playbackSamples = client.AudioSource.timeSamples;
            var clipSamples = client.AudioSamples;

            if (playbackSamples < clipSamples)
                return false;
            
            client.AudioSource.Stop();
            DisposeClient();
            OnAudioEnd?.Invoke();
            Debug.Log($"Playback finished [{playbackSamples}/{clipSamples}]");
            return true;
        }
    }
}
