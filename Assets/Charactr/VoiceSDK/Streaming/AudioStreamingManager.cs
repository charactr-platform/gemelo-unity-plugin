using System;
using System.Collections;
using System.Collections.Generic;
using Charactr.SDK.Streaming;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioStreamingManager: MonoBehaviour
    {
        const string URL = "wss://api.slowpoke.charactr.dev/v1/tts/stream/simplex/ws";
        public AudioClip AudioClip { get; private set; }
        public Action OnAudioEnd { get; private set; }
        public Action OnAudioReady { get; private set; }
      
        [SerializeField] private int voiceId = 112;

        private IAudioStreamingClient _streamingClient;
        private Configuration _configuration;
        private Queue<Action> _actions;

        private void Awake()
        {
            _configuration = Configuration.LoadStreaming();
            
            if (_configuration == null)
                throw new Exception("Can't load Configuration data");
        }

        public IEnumerator ConvertAndStartPlaying(string text)
        {
            yield return CreateClientInstance(text, _configuration);
            Play();
        }
        
        public IEnumerator Convert(string text)
        {
            yield return CreateClientInstance(text, _configuration);
        }

        private IEnumerator CreateClientInstance(string text, Configuration configuration)
        {
            var url = URL + $"?voiceId={voiceId}";
            
            if (_streamingClient == null)
            {
                var audioSource = GetComponent<AudioSource>();
#if UNITY_WEBGL && !UNITY_EDITOR
                _streamingClient = new WebGlAudioStreamingClient(url, configuration, audioSource);
#else
                _streamingClient = new DefaultAudioStreamingClient(url, configuration, audioSource);
#endif
                _streamingClient.Connect();
            }
           
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
                CheckForAudioEnd(_streamingClient);
            }
        }

        public void Play()
        {
            _streamingClient?.Play();
        }

        private void DisposeClient()
        {
            _streamingClient?.Dispose();
            _streamingClient = null;
        }

        private void CheckForAudioEnd(IAudioStreamingClient client)
        {
            if (!client.BufferingCompleted)
                return;
            
            if (client.AudioSource.timeSamples >= client.AudioSamples)
            {
                client.AudioSource.Stop();
                OnAudioEnd?.Invoke();
                DisposeClient();
            }
        }
    }
}
