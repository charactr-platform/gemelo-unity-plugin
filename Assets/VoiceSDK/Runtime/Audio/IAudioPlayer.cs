using System.Collections;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public interface IAudioPlayer
	{
		bool IsPlaying { get; }
		int TimeSamples { get; }
		void Initialize(IAverageProvider averageProvider = null, int sampleSize = 0);
		void PlayClip(AudioClip clip);
		void PlayClip(AudioClip clip,bool stream = false, IAverageProvider averageProvider = null, int samplesSize = 0);
		IEnumerator PlayClipRoutine(AudioClip clip, float playbackLength = 0);
		void Stop();
		float GetSampleAverage();
	}
}