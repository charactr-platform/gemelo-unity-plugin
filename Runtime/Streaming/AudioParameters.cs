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
		public int BitDepth => _bitDepth;
		
		[SerializeField] private AudioDataType audioDataType = AudioDataType.MP3;
		[SerializeField] private int sampleRate = 44100;
		[SerializeField] private int maxClipLength = 30;

		private int _bitDepth;
		public void SetSamplingRate(int rate) => sampleRate = rate;
		public void SetMaxLenght(int lenght) => maxClipLength = lenght;
		public void SetAudioDataType(AudioDataType dataType) => audioDataType = dataType;
		public void SetBitDepth(int bitDepth) => _bitDepth = bitDepth;
	}
}