using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	/// <summary>
	/// Use AudioPlayerInstance behaviour to play one shot, one by one audio clips
	/// </summary>
	[ExecuteInEditMode]
	public class AudioPlayerInstance : AudioPlayerBase, IAudioPlayer
	{
		private static AudioPlayer _instance;
		public new void Play(AudioClip clip) => base.Play(clip);
		public IEnumerator PlayClipRoutine(AudioClip clip, float playbackLength = 0)
		{
			var length = playbackLength > Mathf.Epsilon ? playbackLength : clip.length; 
			
			Play(clip);
			
			yield return new WaitForSecondsRealtime(length);
		}
		public static async Task PlayClipStatic(AudioClip clip)
		{
			var player = GetInstance(clip.name);
			if (player.IsPlaying) player.Stop();
			await player.PlayClip(clip);
		}
		
		private static AudioPlayer GetInstance(string id)
		{
			if (_instance == null)
			{
				_instance = CreateInstance<AudioPlayer>(id);
				Debug.Log($"Created AudioPlayer [{id}]");
			}
			else
			{
				_instance.gameObject.name = GetName(id);
			}
			return _instance;
		}
	}
}