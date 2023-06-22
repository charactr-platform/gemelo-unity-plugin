using System;
using System.Collections;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public interface IAudioPlayer
	{
		void Initialize(int sampleSize = 0);
		void PlayClip(AudioClip clip);
		IEnumerator PlayClipRoutine(AudioClip clip);
		
		void Stop();
		float GetSampleAverage();
	}

	[RequireComponent(typeof(AudioSource))]
	public abstract class AudioPlayerBase: MonoBehaviour, IDisposable 
	{
		private AudioListener _listener;
		private AudioSource _source;
		private bool _isPlaying;
		private WebGlAudioBufferProcessor _bufferProcessor;
		private AverageProvider _averageProvider;
		private float[] _sample;
		private AudioClip _clip;
		
		protected void Initialize(int samplesSize = 0)
		{
			var size = samplesSize == 0 ? AverageProvider.SampleSize : samplesSize;
			_sample = new float[size];
			_averageProvider = new AverageProvider();
#if UNITY_WEBGL && !UNITY_EDITOR
			_bufferProcessor = new WebGlAudioBufferProcessor(size);
#endif
			if (FindObjectOfType<AudioListener>() == null)
				gameObject.AddComponent<AudioListener>();
		}

		protected static T CreateInstance<T>(string cliId) where T : Component, IAudioPlayer
		{
			var player = new GameObject($"~TempPlayer_{cliId}").AddComponent<T>();
			player.hideFlags = HideFlags.HideAndDontSave;
			player.Initialize();
			return player;
		}
		
		protected void Play(AudioClip clip)
		{
			TryGetComponent(out _source);
			_source.clip = clip;
			_source.Play();
		
#if UNITY_WEBGL && !UNITY_EDITOR
			_bufferProcessor.StartSampling(clip, false);
#endif
			_isPlaying = clip.length > Mathf.Epsilon;
		}

		private void Update()
		{
			if (_isPlaying)
			{
				CheckAudioSource();
			}
		}

		protected float GetSampleAverage()
		{
			if (!_isPlaying)
				return 0;
			
		#if UNITY_WEBGL && !UNITY_EDITOR
			_sample = _bufferProcessor.GetSample();
		#else
			_source.GetOutputData(_sample, 0);
		#endif
			
			return _averageProvider.GetSampleAverage(_sample);
		}

		private void CheckAudioSource()
		{
			if (!_source.isPlaying)
			{
				Stop();
			}
		}

		public void Stop()
		{
			_source.Stop();
			
#if UNITY_WEBGL && !UNITY_EDITOR
				_bufferProcessor.StopSampling();
#endif
			Dispose();
		}
		public void Dispose()
		{
			_isPlaying = false;
			
			//Called from static method, destroy gameobject
			if (name.StartsWith("~"))
			{
				DestroyImmediate(gameObject, false);
			}
		}
	}
}