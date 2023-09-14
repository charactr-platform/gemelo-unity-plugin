using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLayer;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public class Mp3Builder : AudioClipBuilder
	{
		public MpegFile MpegFile { get => _mpegFile; }
		
		private readonly MemoryStream _stream;
		private MpegFile _mpegFile;
		
		private readonly float[] _samplesBuffer;
		private int _readout, _samplesCount;
		private long _streamReadCount;
		private long _readPosition = 0L, _currentPosition;
		private bool _endOfData;
		public Mp3Builder(int sampleRate, byte[] headerData) : base(sampleRate)
		{
			_samplesBuffer = new float[44100];
			_stream = new MemoryStream();
			WriteToStream(headerData);
			_mpegFile = new MpegFile(_stream);
		}

		private void WriteToStream(Span<byte> data)
		{
			_stream.Write(data);
			_stream.Flush();
		}

		private bool DecodeBytesToPcmSamples(out Span<float> pcmData, int readSize = 128, int totalReadSize = 1024)
		{
			var chunkSize = 0;
			
			do
			{
				if (chunkSize + readSize > totalReadSize)
				{
					Debug.LogWarning("Overflow of data buffer");
					pcmData = ReturnSamplesBufferRange(chunkSize);
					return false;
				}
				
				var count = _mpegFile.ReadSamples(_samplesBuffer, chunkSize, readSize);
				
				if (count == 0)
				{
					pcmData = ReturnSamplesBufferRange(chunkSize);
					Debug.LogWarning("Decoder: no more data");
					return true;
				}

				chunkSize += count;

			}	while (chunkSize < totalReadSize);

			pcmData = ReturnSamplesBufferRange(totalReadSize);

			return false;
		}

		private Span<float> ReturnSamplesBufferRange(int length)
		{
			return _samplesBuffer.ToList().GetRange(0, length).ToArray();
		}
		
		public override List<PcmFrame> ToPcmFrames(byte[] bytes)
		{
			WriteToStream(bytes);
			
			var frames = new List<PcmFrame>();

			if (_endOfData)
			{
				_mpegFile = new MpegFile(_stream);
				_mpegFile.Position = (_samplesCount * 4);
			}

			var length = _mpegFile.Length / 2;
			
			while (_samplesCount < length)
			{
				_endOfData = DecodeBytesToPcmSamples(out var pcmData,128, 4096);

				var pcmFrames = WritePcmFrames(pcmData.ToArray());
				
				frames.AddRange(pcmFrames);
				_samplesCount += pcmData.Length;

				if (_endOfData)
					break;
			}
			
			Debug.Log($"Stream Eof: {_mpegFile.Reader.EndOfStream}, Bytes: [{_readPosition}/{_stream.Length}], Samples: {_samplesCount}");
			Debug.Log($"Created {frames.Count}");

			return frames;
		}
	}
}