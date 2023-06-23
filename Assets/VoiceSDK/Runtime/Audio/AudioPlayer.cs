using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	[ExecuteInEditMode]
	public class AudioPlayer : AudioPlayerBase, IAudioPlayer
	{
		public new void Initialize(int sampleSize = 0) =>
			base.Initialize(sampleSize);
		public new float GetSampleAverage() =>
			base.GetSampleAverage();
		public void PlayClip(AudioClip clip)
		{
			Initialize();
			Play(clip);
		}
		
		public IEnumerator PlayClipRoutine(AudioClip clip)
		{
			TryGetComponent(out AudioSource source);
			source.PlayOneShot(clip);
			yield return new WaitForSecondsRealtime(clip.length);
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
			var length = playbackTime > Mathf.Epsilon ? playbackTime : clip.length; 
			using (var player = CreateInstance<AudioPlayer>(clip.name))
			{
				player.Play(clip);
				yield return new WaitForSecondsRealtime(length);
			}
		}
	}
}