using System;
using System.Collections;
using System.Collections.Generic;
using Charactr.SDK.Streaming;
using GptDemo.Streaming;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioStreamingManager: MonoBehaviour
    {
        const string URL = "wss://api.slowpoke.charactr.dev/v1/tts/stream/simplex/ws";
        public AudioClip AudioClip { get; private set; }
        public Action<float> OnAudioBufferFull { get; set; }
        
        [SerializeField] private int voiceId = 112;

        private AudioStreamingClientBase _streamingClient;
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
            _streamingClient.Play();
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
                _streamingClient.OnBufferFull = OnAudioBufferFull;
                
                _streamingClient.Connect();
            }
           
            _streamingClient.SendConvertCommand(text);

            yield return new WaitUntil(() => _streamingClient.Initialized);
            
            AudioClip = _streamingClient.AudioClip;
        }
        
        void OnDestroy()
        {
            _streamingClient.Dispose();
            _streamingClient = null;
        }

        private void Update()
        {
            _streamingClient?.DepleteQueue();
        }
    }
}
