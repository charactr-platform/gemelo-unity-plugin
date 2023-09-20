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
		public enum State
		{
			None,
			End,
			EndOfStream,
			DecoderEndOfStream,
			DecoderNoMoreFrames,
			DecoderOk,
			DataOverflow,
			DataBufferEmpty
		}
		
		public MpegFile MpegFile { get => _mpegFile; }
		public const int SamplesPerDecoderFrame = 1152;
		public const int MinimalHeaderSize = 500;
		private const int SamplesChunkRead = SamplesPerDecoderFrame / 9;
		
		private readonly MemoryStream _stream;
		private readonly MpegFile _mpegFile;
		private readonly int _bytesPerSecond;
		private readonly float[] _samplesBuffer;
		private int _samplesCount;
		public Mp3Builder(int sampleRate, byte[] headerData) : base(sampleRate)
		{
			if (headerData.Length < MinimalHeaderSize)
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
		public State DecodeBytesToPcmSamples(out Span<float> pcmData, int chunkSize = SamplesChunkRead, int outputSize = SamplesPerDecoderFrame)
		{
			var decodedPcmSamples = 0;
			
			do
			{
				if (decodedPcmSamples + chunkSize > outputSize)
				{
					Debug.LogWarning("Overflow of data buffer");
					pcmData = ReturnSamplesBufferRange(decodedPcmSamples);
					return State.DataOverflow;
				}
				
				if (CheckForDecoderEof())
				{
					pcmData = ReturnSamplesBufferRange(decodedPcmSamples);
					return State.DecoderEndOfStream;
				}
				
				var count = _mpegFile.ReadSamples(_samplesBuffer, decodedPcmSamples, chunkSize);
				
				if (count == 0)
				{
					pcmData = ReturnSamplesBufferRange(decodedPcmSamples);
					Debug.LogWarning($"Decoder: no more data, decoded frames: {_mpegFile.DecodedFrames}, eof: {_mpegFile.Reader.EndOfStream}");
					return State.DecoderNoMoreFrames;
				}
				
				decodedPcmSamples += count;
				
			}	while (decodedPcmSamples < outputSize);
			
			pcmData = ReturnSamplesBufferRange(decodedPcmSamples);

			return State.DecoderOk;
		}

		private Span<float> ReturnSamplesBufferRange(int length)
		{
			return _samplesBuffer.ToList().GetRange(0, length).ToArray();
		}

		private bool CheckForDecoderEof()
		{
			var framesProcessed = _mpegFile.DecodedFrames;
			
			var chunkReadDuration = (float) SamplesPerDecoderFrame / SampleRate;

			//Calculate and add slight margin for VBR frames (10%)
			var decodedDuration = (((float) framesProcessed * SamplesPerDecoderFrame) / SampleRate );

			var availableDuration = (float) (_stream.Length - MinimalHeaderSize) / _bytesPerSecond;
			
			if (decodedDuration + chunkReadDuration > availableDuration)
			{
				Debug.LogWarning($"Decoder buffer is running out [{decodedDuration:F2}/{availableDuration:F2}]!");
				return true;
			}
			
			return false;
		}
		public override List<PcmFrame> ToPcmFrames(byte[] bytes)
		{
			WriteToStream(bytes);
			
			var state = State.None;
			var frames = new List<PcmFrame>();
			
			do
			{
				state = DecodeBytesToPcmSamples(out var pcmData);

				var pcmFrames = WritePcmFrames(pcmData.ToArray());

				frames.AddRange(pcmFrames);
				_samplesCount += pcmData.Length;

			} while (state == State.DecoderOk);
		
			Debug.Log($"Stream State: {state}, Eof: {_mpegFile.Reader.EndOfStream}, Bytes: [{_stream.Length}], Samples: {_samplesCount}");

			return frames;
		}
	}
}