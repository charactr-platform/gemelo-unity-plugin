using System;
using System.Collections;
using UnityEngine;

namespace Charactr.VoiceSDK
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(AudioSource))]
	public class AudioPlayer : MonoBehaviour, IDisposable
	{
		private AudioListener _listener;
		private AudioSource _source;
		private bool _isPlaying;

		private void Awake()
		{
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			
			if (FindObjectOfType<AudioListener>() == null)
				gameObject.AddComponent<AudioListener>();
		}

		public static void PlayClip(AudioClip clip)
		{
			var name = $"~TempPlayer_{clip.name}";
			GetInstance(name).PlayOneShot(clip);
		}

		private static AudioPlayer GetInstance(string name)
		{
			return new GameObject(name).AddComponent<AudioPlayer>();
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
			_isPlaying = clip.length > Mathf.Epsilon;
		}

		private void Update()
		{
			if (_isPlaying)
				CheckAudioSource();
		}

		private void CheckAudioSource()
		{
			if (!_source.isPlaying)
			{
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