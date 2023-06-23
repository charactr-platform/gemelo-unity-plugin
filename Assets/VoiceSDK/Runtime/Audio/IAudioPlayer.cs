using System.Collections;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public interface IAudioPlayer
	{
		bool IsPlaying { get; }
		void Initialize(int sampleSize = 0);
		void PlayClip(AudioClip clip);
		IEnumerator PlayClipRoutine(AudioClip clip, float playbackLength = 0);
		void Stop();
		float GetSampleAverage();
	}
}