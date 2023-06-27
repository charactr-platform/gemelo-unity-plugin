using System.Collections;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public interface IAudioPlayer
	{
		bool IsInitialized { get; }
		bool IsPlaying { get; }
		int TimeSamples { get; }
		void Initialize(IAverageProvider averageProvider = null, int sampleSize = IAverageProvider.SampleSize);
		void PlayClip(AudioClip clip);
		void PlayClip(AudioClip clip, bool stream = false);
		IEnumerator PlayClipRoutine(AudioClip clip, float playbackLength = 0);
		void Stop();
		float GetSampleAverage();
	}
}