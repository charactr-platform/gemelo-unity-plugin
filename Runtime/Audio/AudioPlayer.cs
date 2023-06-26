using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	[ExecuteInEditMode]
	public class AudioPlayer : AudioPlayerBase, IAudioPlayer
	{
		public void PlayClip(AudioClip clip)
		{
			Initialize();
			Play(clip);
		}
		
		public IEnumerator PlayClipRoutine(AudioClip clip,  float playbackTime = 0f)
		{
			var length = playbackTime > Mathf.Epsilon ? playbackTime : clip.length; 
			
			PlayClip(clip);
			yield return new WaitForSecondsRealtime(length);
		}
		
		public static async Task PlayClipStatic(AudioClip clip, float playbackTime = 0f)
		{
			var length = playbackTime > Mathf.Epsilon ? playbackTime : clip.length; 
			
			using (var player = CreateInstance<AudioPlayer>(clip.name))
			{
				player.Play(clip);
				await Task.Delay(TimeSpan.FromSeconds(length));
			}
		}
		
		public static IEnumerator PlayClipRoutineStatic(AudioClip clip, float playbackTime = 0f)
		{
			using (var player = CreateInstance<AudioPlayer>(clip.name))
			{
				yield return player.PlayClipRoutine(clip, playbackTime);
			}
		}
	}
}