using System;
using System.Collections.Generic;
using System.IO;
using NLayer;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public class Mp3Builder : AudioClipBuilder
	{
		private readonly MemoryStream _stream, _mpegStream;
		private readonly BinaryWriter _writer;
		private long _writeCount;
		private long _lastWriteCount;
		private MpegFile _mpegFile;
		private readonly float[] _samplesBuffer;
		private long _mpegPosition = 0;

		public Mp3Builder(int sampleRate, byte[] headerData) : base(sampleRate)
		{
			_samplesBuffer = new float[sampleRate * 1]; //1second
			_stream = new MemoryStream();
			_writer = new BinaryWriter(_stream);
			_writer.Write(headerData);
		
		}

		public override List<PcmFrame> ToPcmFrames(byte[] bytes)
		{
			_writer.Write(bytes);
		
			var frames = new List<PcmFrame>();

			var samples = new ArraySegment<float>(_samplesBuffer);
			
			var count = 0;
			var readout = 0;
			
			_mpegFile = new MpegFile(_stream);
			_mpegFile.Position = _mpegPosition;
			
			do
			{
				count = _mpegFile.ReadSamples(samples.Array, 0, _samplesBuffer.Length);
				readout += count;
				
			} while (count > 0);

			
			Debug.Log($"Stream, Bytes: [{_stream.Length}], ReadOut: {readout}");

			frames.AddRange(WritePcmFrames(samples.Slice(0, readout).ToArray()));
			Debug.Log($"Created {frames.Count}");
			_mpegPosition = _mpegFile.Position;
			return frames;
		}
		
	}
}