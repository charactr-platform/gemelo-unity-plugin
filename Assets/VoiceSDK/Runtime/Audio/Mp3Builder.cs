using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NLayer;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public class Mp3Builder: AudioClipBuilder
	{
		private readonly MpegFile _mpegFile;
		private readonly MemoryStream _stream;
		private PcmFrame _currentFrame;
		private readonly List<PcmFrame> _frames;
		public Mp3Builder(int sampleRate, byte[] headerData) : base(sampleRate)
		{
			_stream = new MemoryStream();
			_stream.Write(headerData);
			_mpegFile = new MpegFile(_stream);
			_currentFrame = new PcmFrame();	
			_frames = new List<PcmFrame>();
		}
		
		public override List<PcmFrame> DecodeDataToPcm(byte[] bytes)
		{
			_stream.Write(bytes);
			
			var samplesToRead = (_mpegFile.Length - _mpegFile.Position);
			var samplesCount = 0;

			var buffer = new float[samplesToRead];
			samplesCount += _mpegFile.ReadSamples(buffer, 0, (int)samplesToRead);
			Debug.Log($"Stream L: [{_stream.Length}] P: [{_mpegFile.Position}/{_mpegFile.Length}] , S: [{samplesCount}/{samplesToRead}]");

			WritePcmFrames(buffer);

			Debug.Log($"Created {_frames.Count}");
			return _frames;
		}

		private void WritePcmFrames(float[] samples)
		{
			var more = _currentFrame.AddPcmData(samples, out var overflow);
			_frames.Add(_currentFrame);

			if (!more) return;
			
			_currentFrame = new PcmFrame();
			WritePcmFrames(overflow);
		}
	}
}