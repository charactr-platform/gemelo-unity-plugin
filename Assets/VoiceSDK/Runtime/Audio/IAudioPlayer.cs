using System.Collections;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public interface IAudioPlayer
	{
		bool IsInitialized { get; }
		bool IsPlaying { get; }
		int TimeSamples { get; }
		void Initialize(bool streaming, IAverageProvider averageProvider = null, int sampleSize = IAverageProvider.SampleSize);
		void Play(AudioClip clip);
		IEnumerator PlayClipRoutine(AudioClip clip, float playbackLength = 0);
		void Stop();
		float GetSampleAverage();
	}
}