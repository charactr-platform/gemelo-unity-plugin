using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public class WavBuilder: AudioClipBuilder
	{
		private readonly Memory<byte> _data;
		private readonly WavHeaderData _header;
		
		private readonly WavDebugSave _debugSave;
		private PcmFrame _currentFrame;
		private List<PcmFrame> _frames;
		public WavBuilder(int sampleRate, byte[] data): base(sampleRate)
		{
			_data = data;
			_header = new WavHeaderData(data);
			_frames = new List<PcmFrame>();
			_currentFrame = new PcmFrame();
		}
		public AudioClip CreateAudioClip(string name = "clip")
		{
			PcmFrame.ConvertByteToFloat(_data.ToArray(), out var waveData, _header.DataOffset);
			var clip = AudioClip.Create(name, waveData.Length, _header.Channels, _header.SampleRate, false);
			clip.SetData(waveData, 0);
			return clip;
		}

		public override List<PcmFrame> DecodeDataToPcm(byte[] bytes)
		{
			WritePcmFrames(bytes);
			Debug.Log($"Created frames: {_frames}");
			return _frames;
		}
		
		private void WritePcmFrames(Span<byte> rawData)
		{
			var more = _currentFrame.AddData(rawData.ToArray(), out var overflow);
				
			_frames.Add(_currentFrame);

			if (!more) return;
			
			_currentFrame = new PcmFrame();
			WritePcmFrames(overflow);
		}
	}
}