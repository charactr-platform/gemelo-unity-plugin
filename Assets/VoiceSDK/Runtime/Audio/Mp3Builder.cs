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
		private readonly MemoryStream _mpegStream;
		private readonly MpegFile _mpegFile;
		
		private readonly float[] _samplesBuffer;
		private int _readout, _samplesCount;
		private long _streamReadCount;
		private long _readPosition = 0L;
		public Mp3Builder(int sampleRate, byte[] headerData) : base(sampleRate)
		{
			_samplesBuffer = new float[44100];
			_stream = new MemoryStream();
			_mpegStream = new MemoryStream();
			WriteToStream(headerData);
			_mpegFile = new MpegFile(_mpegStream);
		}

		private void WriteToStream(Span<byte> data)
		{
			_stream.Write(data);
			_stream.Flush();
			_readout += CopyTo(_mpegStream, _readout);
		}

		private List<float> DecodeBytesToPcm(int readSize = 1024)
		{
			var count = 0;
			var chunkSize = 0;

			do
			{
				if (chunkSize + readSize > _samplesBuffer.Length)
				{
					Debug.LogWarning("Overflow of data buffer");
					return _samplesBuffer.ToList();
				}
				
				count = _mpegFile.ReadSamples(_samplesBuffer, chunkSize, readSize);
				chunkSize += count;
			
			}	while (count > 0) ;

			return _samplesBuffer.ToList().GetRange(0, chunkSize);
		}
		
		public override List<PcmFrame> ToPcmFrames(byte[] bytes)
		{
			WriteToStream(bytes);
			
			var frames = new List<PcmFrame>();
			
			while (_mpegFile.Reader.HasNextFrame)
			{
				var pcmSamples = DecodeBytesToPcm();
				var pcmFrames = WritePcmFrames(pcmSamples.ToArray());
				frames.AddRange(pcmFrames);
				_samplesCount += pcmSamples.Count;
				_readPosition = _stream.Position;
				Debug.Log($"Position: {_readPosition} / {_stream.Length}");
			}
			
			Debug.Log($"Stream Eof: {_mpegFile.Reader.EndOfStream}, Bytes: [{_stream.Length}], Samples: {_samplesCount}");
			Debug.Log($"Created {frames.Count}");

			return frames;
		}
		
		public int CopyTo(Stream stream, long offset)
		{
			byte[] buffer = new byte[128];

			int bytesRead = 0, totalRead = 0;

			_stream.Seek(offset, SeekOrigin.Begin);
			stream.Seek(offset, SeekOrigin.Begin);

			while ((bytesRead = _stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				stream.Write(buffer, 0, bytesRead);
				totalRead += bytesRead;
			}
		
			stream.Flush();
			return totalRead;
		}
	}
}