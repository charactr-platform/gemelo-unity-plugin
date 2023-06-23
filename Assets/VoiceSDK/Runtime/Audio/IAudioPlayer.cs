using System.Collections;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public interface IAudioPlayer
	{
		void Initialize(int sampleSize = 0);
		void PlayClip(AudioClip clip);
		IEnumerator PlayClipRoutine(AudioClip clip);
		void Stop();
		float GetSampleAverage();
	}
}