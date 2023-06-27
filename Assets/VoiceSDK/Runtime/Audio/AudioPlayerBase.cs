using System;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	[RequireComponent(typeof(AudioSource))]
	public abstract class AudioPlayerBase: MonoBehaviour, IDisposable
	{
		public bool IsPlaying => _source.isPlaying;
		public int TimeSamples => _source.timeSamples;
		
		private AudioListener _listener;
		private AudioSource _source;
		private WebGlAudioBufferProcessor _bufferProcessor;
		private IAverageProvider _averageProvider;
		private float[] _sample;
		private AudioClip _clip;
		
		public void Initialize(IAverageProvider averageProvider = null, int samplesSize = 0)
		{
			var size = samplesSize == 0 ? IAverageProvider.SampleSize : samplesSize;
			_sample = new float[size];
			
			TryGetComponent(out _source);
			SetDefaultAverageProvider(averageProvider);
			
#if UNITY_WEBGL && !UNITY_EDITOR
			_bufferProcessor = new WebGlAudioBufferProcessor(size);
#endif
			if (FindObjectOfType<AudioListener>() == null)
				gameObject.AddComponent<AudioListener>();
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
			player.Initialize();
			return player;
		}
		
		protected void Play(AudioClip clip, bool stream = false)
		{
			_source.clip = clip;
			_source.Play();
		
#if UNITY_WEBGL && !UNITY_EDITOR
			_bufferProcessor.StartSampling(clip, stream);
#endif
		}

		protected void PlayStream(AudioClip clip)
		{
			Play(clip, true);
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