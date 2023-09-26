using System;
using Gemelo.Voice.Audio;
using UnityEngine;

namespace Gemelo.Voice.Streaming
{
	[Serializable]
	public class AudioParameters
	{
		public AudioDataType AudioDataType => audioDataType;
		public int SampleRate => sampleRate;
		public int MaxClipLenght => maxClipLength;
		
		[SerializeField] private AudioDataType audioDataType = AudioDataType.Mp3;
		[SerializeField] private int sampleRate = 44100;
		[SerializeField] private int maxClipLength = 30;
		
		public void SetSamplingRate(int rate) => sampleRate = rate;
		public void SetMaxLenght(int lenght) => maxClipLength = lenght;
		public void SetAudioDataType(AudioDataType dataType) => audioDataType = dataType;
	}
}