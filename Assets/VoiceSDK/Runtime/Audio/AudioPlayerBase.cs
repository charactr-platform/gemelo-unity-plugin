using System;
using UnityEngine;

namespace gemelo.VoiceSDK.Audio
{
	[RequireComponent(typeof(AudioSource))]
	public abstract class AudioPlayerBase: MonoBehaviour, IDisposable
	{
		public bool IsInitialized => _source != null;
		public bool IsPlaying => IsInitialized && _source.isPlaying;
		public int TimeSamples => IsInitialized ? _source.timeSamples : 0;
		
		private AudioListener _listener;
		private AudioSource _source;
		private WebGlAudioBufferProcessor _bufferProcessor;
		private IAverageProvider _averageProvider;
		private float[] _sample;
		private AudioClip _clip;
		
		public void Initialize(bool streaming, IAverageProvider averageProvider = null, int samplesSize = IAverageProvider.SampleSize)
		{
			
#if UNITY_WEBGL && !UNITY_EDITOR
			_bufferProcessor = new WebGlAudioBufferProcessor(samplesSize, streaming);
#endif
			_sample = new float[samplesSize];
			
			SetDefaultAverageProvider(averageProvider);
			
			if (FindObjectOfType<AudioListener>() == null)
				gameObject.AddComponent<AudioListener>();
			
			TryGetComponent(out _source);
		}

		private void SetDefaultAverageProvider(IAverageProvider averageProvider)
		{
			if (averageProvider == null)
				_averageProvider = new AverageProvider();
			else
				_averageProvider = averageProvider;
		}
		
		protected static T CreateInstance<T>(string clipId) where T : Component, IAudioPlayer
		{
			var player = new GameObject($"~TempPlayer_{clipId}").AddComponent<T>();
			player.gameObject.hideFlags = HideFlags.HideAndDontSave;
			player.Initialize(false);
			return player;
		}
		
		protected void Play(AudioClip clip)
		{
			if (!IsInitialized)
				throw new Exception("Player not initialized, Initialize() first");
			
			_source.clip = clip;
			
			_source.Play();
			
#if UNITY_WEBGL && !UNITY_EDITOR
			_bufferProcessor.StartSampling(clip);
#endif
		}
		
		public float GetSampleAverage()
		{
			if (!IsPlaying)
				return 0;
			
		#if UNITY_WEBGL && !UNITY_EDITOR
			_sample = _bufferProcessor.GetSample();
		#else
			_source.GetOutputData(_sample, 0);
		#endif
			
			return _averageProvider.GetSampleAverage(_sample);
		}
		
		public void Stop()
		{
			_source.Stop();
			
#if UNITY_WEBGL && !UNITY_EDITOR
			_bufferProcessor.StopSampling();
#endif
		}
		
		public void Dispose()
		{
			if (IsPlaying)
				Stop();
			
			//Called from static method, destroy GameObject on Dispose
			if (gameObject.hideFlags == HideFlags.HideAndDontSave)
			{
				Debug.Log($"Destroying AudioPlayer game object:{name}");
				DestroyImmediate(gameObject, false);
			}
		}
	}
}