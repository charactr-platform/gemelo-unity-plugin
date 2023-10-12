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
		
		public WavBuilder(int sampleRate, int bitDepth, byte[] data) : base(sampleRate, bitDepth)
		{
			_data = data;
			_header = new WavHeaderData(data);
		}

		public WavBuilder(int sampleRate, int bitDepth) : base(sampleRate, bitDepth) { }
		
		public AudioClip CreateAudioClip(string name = "clip")
		{
			if (_data.IsEmpty)
				throw new Exception("Can't create AudioClip without data!");
			
			PcmFrame.ConvertByteToFloat(_data.ToArray(), out var waveData, _header.DataOffset, PcmFrame.BitDepthToBlockSize(BitDepth));
			var clip = AudioClip.Create(name, waveData.Length, _header.Channels, _header.SampleRate, false);
			clip.SetData(waveData, 0);
			return clip;
		}

		public override List<PcmFrame> ToPcmFrames(byte[] bytes) => WritePcmFrames(bytes);
	}
}