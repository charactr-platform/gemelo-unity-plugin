using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public class WavBuilder : AudioClipBuilder
	{
		public const int HeaderSize = 44;
		private readonly Memory<byte> _data;
		private readonly WavHeaderData _header;
		
		private readonly WavDebugSave _debugSave;
		
		public WavBuilder(int sampleRate, byte[] data) : base(sampleRate)
		{
			_data = data;
			_header = new WavHeaderData(data);
		}

		public AudioClip CreateAudioClip(string name = "clip")
		{
			PcmFrame.ConvertByteToFloat(_data.ToArray(), out var waveData, _header.DataOffset);
			var clip = AudioClip.Create(name, waveData.Length, _header.Channels, _header.SampleRate, false);
			clip.SetData(waveData, 0);
			return clip;
		}

		public override List<PcmFrame> ToPcmFrames(byte[] bytes) => WritePcmFrames(bytes);
	}
}