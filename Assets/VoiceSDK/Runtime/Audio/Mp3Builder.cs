using System;
using System.Collections.Generic;
using System.IO;
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
		private int _readout, _writeCount;
		private long _streamReadCount;
		
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
		
		public override List<PcmFrame> ToPcmFrames(byte[] bytes)
		{
			WriteToStream(bytes);
			
			var frames = new List<PcmFrame>();
			
			var chunkSize = 0;
			var count = 0;

			_mpegFile.Position = _writeCount;

			while (_mpegFile.Reader.HasNextFrame)
			{
				if (chunkSize + 1024 > _samplesBuffer.Length)
				{
					Debug.LogWarning("Overflow of data buffer");
					break;
				}

				count = _mpegFile.ReadSamples(_samplesBuffer, chunkSize, 1024);
				chunkSize += count;
			}
			
			
			frames.AddRange(WritePcmFrames(_samplesBuffer.AsSpan(0, chunkSize).ToArray()));

			_writeCount += chunkSize;
			Debug.Log($"Stream Eof: {_mpegFile.Reader.EndOfStream}, Bytes: [{_stream.Length}], Samples: {chunkSize}/{_writeCount}");
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