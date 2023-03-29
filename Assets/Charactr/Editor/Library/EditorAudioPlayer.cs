using System;
using System.Collections;
using UnityEngine;

namespace Charactr.Editor.Library
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(AudioListener), typeof(AudioSource))]
	public class EditorAudioPlayer : MonoBehaviour, IDisposable
	{
		private AudioListener _listener;
		private AudioSource _source;
		private bool _isPlaying;

		private void Awake()
		{
			gameObject.hideFlags = HideFlags.HideAndDontSave;
		}

		public static void PlayClip(AudioClip clip)
		{
			var name = $"~TempPlayer_{clip.name}";
			GetInstance(name).PlayOneShot(clip);
		}

		private static EditorAudioPlayer GetInstance(string name)
		{
			return new GameObject(name).AddComponent<EditorAudioPlayer>();
		}
		public static IEnumerator PlayClipRoutine(AudioClip clip)
		{
			var name = $"~TempPlayer_{clip.name}";
			
			using (var player = GetInstance(name))
			{
				player.TryGetComponent(out AudioSource source);
				source.PlayOneShot(clip);

				while (source.isPlaying)
				{
					yield return null;
				}
			}
		}
		
		private void PlayOneShot(AudioClip clip)
		{
			TryGetComponent(out _source);
		
			_source.PlayOneShot(clip);
			_isPlaying = clip.length > 0.1f;
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