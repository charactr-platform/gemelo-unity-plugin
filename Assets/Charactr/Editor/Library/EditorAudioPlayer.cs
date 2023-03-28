using System;
using UnityEngine;

namespace Charactr.Editor.Library
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(AudioListener), typeof(AudioSource))]
	public class EditorAudioPlayer : MonoBehaviour
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
			var player = new GameObject("~player").AddComponent<EditorAudioPlayer>();
			player.PlayOneShot(clip);
		}
		
		public void PlayOneShot(AudioClip clip)
		{
			TryGetComponent(out _listener);
			TryGetComponent(out _source);
			name = $"~TempPlayer_{clip.name}";
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
				_isPlaying = false;
				DestroyImmediate(gameObject, false);
			}
		}
	}
}