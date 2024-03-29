using System;
using System.Collections;
using System.Collections.Generic;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Rest.Model;
using UnityEngine;

namespace Gemelo.Voice.Streaming
{
    /// <summary>
    /// Base utility class for Audio Streaming
    /// </summary>
    [RequireComponent(typeof(AudioPlayer))]
    public class AudioStreamingManager: MonoBehaviour
    {
        public IAudioPlayer AudioPlayer { get; private set; }
        public AudioClip AudioClip { get; private set; }
        public bool AudioEnd { get; private set; }
        public AudioParameters AudioParameters { 
            get => audioParameters ??= new AudioParameters();
            private set => audioParameters = value;
        }
        public event Action OnAudioEnd, OnAudioReady;

        [SerializeField] private int voiceId = 151;

        [Header("Audio details")] [SerializeField]
        private AudioParameters audioParameters;
        
        private IAudioStreamingClient _streamingClient;
        private IAverageProvider _averageProvider;
        private int _samplesSize;
       
        private Configuration _configuration;
        private Queue<Action> _actions;

        private void Awake()
        {
            _configuration = Configuration.Load();
            
            if (_configuration == null)
                throw new Exception("Can't load Configuration data");

            AudioPlayer = GetComponent<AudioPlayer>();

            if (AudioPlayer == null)
                throw new Exception("Can't find required AudioPlayer component");
        }
        
        public IAudioPlayer InitializePlayer(IAverageProvider provider, int samplesSize)
        {
            _averageProvider = provider;
            _samplesSize = samplesSize;
            AudioPlayer.Initialize(true, _averageProvider, _samplesSize);
            return AudioPlayer;
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
            var url = Configuration.STREAMING_API + $"?voiceId={voiceId}";
            
#if UNITY_WEBGL && !UNITY_EDITOR
            audioParameters.SetSamplingRate(WebGlAudioBufferProcessor.GetSupportedSampleRate());
            _streamingClient = new WebGlAudioStreamingClient(url, configuration, audioParameters);
#else
            _streamingClient = new DefaultAudioStreamingClient(url, configuration, audioParameters);
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
            AudioPlayer.Play(AudioClip);
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

            var playbackSamples = AudioPlayer.TimeSamples;
            var clipSamples = client.TimeSamples;

            if (playbackSamples < clipSamples)
                return false;
            
            AudioPlayer.Stop();
            DisposeClient();
            OnAudioEnd?.Invoke();
            Debug.Log($"Playback finished [{playbackSamples}/{clipSamples}]");
            return true;
        }
        
        public void SetVoiceId(int voice) => voiceId = voice;
        public void SetAudioParameters(AudioParameters parameters) => AudioParameters = parameters;
        public void SetSamplingRate(int rate) => AudioParameters.SetSamplingRate(rate);
        public void SetMaxLenght(int lenght) => AudioParameters.SetMaxLenght(lenght);
        public void SetAudioDataType(AudioDataType audioType) => AudioParameters.SetAudioDataType(audioType);
        public void SetVoiceType(VoiceType voiceType) => AudioParameters.SetVoiceType(voiceType);
    }
}
