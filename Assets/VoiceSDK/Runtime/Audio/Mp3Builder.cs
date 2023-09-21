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
		public const int SamplesPerDecoderFrame = 1152;
		public const int HeaderSize = 500;
		private const float EOF_MARGIN = 0.02f;
		private const int SamplesChunkRead = SamplesPerDecoderFrame / 9;
		
		private readonly MemoryStream _stream;
		private readonly MpegFile _mpegFile;
		private readonly int _bytesPerSecond;
		private readonly float[] _samplesBuffer;
		private int _samplesCount;
		public Mp3Builder(int sampleRate, byte[] headerData) : base(sampleRate)
		{
			if (headerData.Length < HeaderSize)
				throw new Exception("Need more data to process header");
			
			_samplesBuffer = new float[44100];
			_stream = new MemoryStream();
			
			WriteToStream(headerData);
		
			_mpegFile = new MpegFile(_stream);
			_bytesPerSecond = (_mpegFile.Reader.BitRate / 8);
			Debug.Log($"Created Mp3Builder: SampleRate: {SampleRate}, Bitrate: {_mpegFile.Reader.BitRate / 1000}, BytesPerSecond: {_bytesPerSecond}");
		}
		
		private void WriteToStream(Span<byte> data)
		{
			_stream.Write(data);
			_stream.Flush();
		}

		/// <summary>
		/// Decodes Stream data into PCM samples, reads by small chunk until totalReadSize is reached
		/// </summary>
		/// <param name="pcmData">Output of PCM samples </param>
		/// <param name="chunkSize"></param>
		/// <param name="outputSize"></param>
		/// <returns></returns>
		public DecodingState DecodeBytesToPcmSamples(out Span<float> pcmData, int chunkSize = SamplesChunkRead, int outputSize = SamplesPerDecoderFrame)
		{
			var decodedPcmSamples = 0;
			
			do
			{
				if (CheckForDecoderEof())
				{
					pcmData = ReturnSamplesBufferRange(decodedPcmSamples);
					return DecodingState.EndOfStream;
				}
				
				var count = _mpegFile.ReadSamples(_samplesBuffer, decodedPcmSamples, chunkSize);
				
				if (count == 0)
				{
					pcmData = ReturnSamplesBufferRange(decodedPcmSamples);
					Debug.LogError($"Decoder: no more data, decoded frames: {_mpegFile.DecodedFrames}, eof: {_mpegFile.Reader.EndOfStream}");
					return DecodingState.End;
				}
				
				decodedPcmSamples += count;
				
			}	while (decodedPcmSamples < outputSize);
			
			pcmData = ReturnSamplesBufferRange(decodedPcmSamples);

			return DecodingState.Success;
		}

		private Span<float> ReturnSamplesBufferRange(int length)
		{
			return _samplesBuffer.ToList().GetRange(0, length).ToArray();
		}

		private bool CheckForDecoderEof()
		{
			var framesProcessed = _mpegFile.DecodedFrames;
			var chunkReadDuration = (float) SamplesPerDecoderFrame / SampleRate;
			
			var decodedDuration = ((float) framesProcessed * SamplesPerDecoderFrame) / SampleRate;

			var availableDuration = (float) (_stream.Length - HeaderSize) / _bytesPerSecond;
			
			if (decodedDuration + chunkReadDuration > availableDuration - EOF_MARGIN)
			{
				Debug.LogWarning($"Decoder buffer is running out [{decodedDuration:F2}/{availableDuration:F2}]!");
				return true;
			}
			
			return false;
		}
		public override List<PcmFrame> ToPcmFrames(byte[] bytes)
		{
			WriteToStream(bytes);
			
			var state = DecodingState.None;
			var frames = new List<PcmFrame>();
			
			do
			{
				state = DecodeBytesToPcmSamples(out var pcmData);

				var pcmFrames = WritePcmFrames(pcmData.ToArray());

				frames.AddRange(pcmFrames);
				_samplesCount += pcmData.Length;

			} while (state == DecodingState.Success);
		
			Debug.Log($"Stream State: {state}, Eof: {_mpegFile.Reader.EndOfStream}, Bytes: [{_stream.Length}], Samples: {_samplesCount}");

			return frames;
		}
	}

	public enum DecodingState
	{
		None,
		End,
		EndOfStream,
		Success,
		DataOverflow,
		DataBufferEmpty
	}
}