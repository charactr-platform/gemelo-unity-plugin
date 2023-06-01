using System;
using System.Collections;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(AudioSource))]
	public class AudioPlayer : MonoBehaviour, IDisposable
	{
		private AudioListener _listener;
		private AudioSource _source;
		private bool _isPlaying;
		private WebGlAudioBufferProcessor _bufferProcessor;
		private AverageProvider _averageProvider;
		private float[] _sample;
		private AudioClip _clip;
		private void Awake()
		{
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			
			if (FindObjectOfType<AudioListener>() == null)
				gameObject.AddComponent<AudioListener>();
		}

		private void Initialize()
		{
			_sample = new float[AverageProvider.SampleSize];
			_averageProvider = new AverageProvider();
			_bufferProcessor = new WebGlAudioBufferProcessor(AverageProvider.SampleSize);
		}
		
		public static void PlayClip(AudioClip clip)
		{
			var name = $"~TempPlayer_{clip.name}";
			GetInstance(name).PlayOneShot(clip);
		}

		private static AudioPlayer GetInstance(string name)
		{
			var player = new GameObject(name).AddComponent<AudioPlayer>();
			player.Initialize();
			
			return player;
		}
		public static IEnumerator PlayClipRoutine(AudioClip clip)
		{
			var name = $"~TempPlayer_{clip.name}";
			
			using (var player = GetInstance(name))
			{
				player.TryGetComponent(out AudioSource source);
				source.PlayOneShot(clip);
				yield return new WaitForSecondsRealtime(clip.length);
			}
		}
		
		private void PlayOneShot(AudioClip clip)
		{
			TryGetComponent(out _source);
			_source.PlayOneShot(clip);
		
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
				GetSampleAverage();
			}
		}

		private float GetSampleAverage()
		{
			
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
#if UNITY_WEBGL && !UNITY_EDITOR
				_bufferProcessor.StopSampling();
#endif
				Dispose();
			}
		}
		
		public void Dispose()
		{
			_isPlaying = false;
			DestroyImmediate(gameObject, false);
		}
	}
}