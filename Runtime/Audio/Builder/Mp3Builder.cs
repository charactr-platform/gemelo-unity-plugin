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
		public MpegFile MpegFile => _mpegFile;
		public const int SamplesPerDecoderFrame = 1152;
		public const int HeaderSize = 500;
		public int Bitrate { get;}
		public float AvailableDuration => _writeCount / ((float)Bitrate / 8);
		public float DecodedDuration => ((float) _mpegFile.DecodedFrames * SamplesPerDecoderFrame) / SampleRate;
		
		private readonly MemoryStream _stream;
		private readonly float[] _samplesBuffer;
		private readonly float _eofMargin;
		private MpegFile _mpegFile;
		private int _samplesCount, _writeCount;
		public Mp3Builder(int sampleRate, byte[] headerData) : base(sampleRate, 16)
		{
			if (sampleRate < 44100)
				throw new Exception("Supported sample rate for Mp3 decoding is 44100");
			
			if (headerData.Length < HeaderSize)
				throw new Exception("Need more data to process header");
			
			_samplesBuffer = new float[sampleRate];
			_stream = new MemoryStream();
			_eofMargin = ((float)SamplesPerDecoderFrame / SampleRate) * 2f;

			WriteToStream(headerData);
		
			_mpegFile = new MpegFile(_stream);
			Bitrate = _mpegFile.Reader.BitRate;
			
			Debug.Log($"Mp3Builder: SampleRate [{SampleRate}] Bitrate [{Bitrate}] EofMargin [{_eofMargin}]");
		}
		
		public void WriteToStream(Span<byte> data)
		{
			_writeCount += data.Length;
			_stream.Write(data);
			_stream.Flush();
		}

		/// <summary>
		/// Decodes Stream data into PCM samples, reads by small chunk until totalReadSize is reached
		/// </summary>
		/// <param name="pcmData">Output of PCM samples </param>
		/// <param name="outputSize">Samples per frame = 0.02s</param>
		/// <returns></returns>
		public DecodingState DecodeBytesToPcmSamples(out Span<float> pcmData, int outputSize = SamplesPerDecoderFrame)
		{
			var decodedPcmSamples = 0;
			pcmData = null;
			
			var readChunk = outputSize / 8; //144 samples per chunk

			do
			{
				if (DecodedDuration > AvailableDuration - _eofMargin)
				{
					pcmData = ReturnSamplesBufferRange(decodedPcmSamples);
					return DecodingState.BufferEmpty;
				}

				var count = _mpegFile.ReadSamples(_samplesBuffer, decodedPcmSamples, readChunk);

				if (count == 0)
				{
					pcmData = ReturnSamplesBufferRange(decodedPcmSamples);
					Debug.LogWarning($"Decoder: EndOfStream, frames [{_mpegFile.DecodedFrames}], EOS: [{_mpegFile.Reader.EndOfStream}]");
					return DecodingState.EndOfStream;
				}

				decodedPcmSamples += count;
				
				pcmData = ReturnSamplesBufferRange(decodedPcmSamples);
				
			} while (decodedPcmSamples < outputSize);
			
			return DecodingState.Success;
		}

		private Span<float> ReturnSamplesBufferRange(int length)
		{
			return _samplesBuffer.ToList().GetRange(0, length).ToArray();
		}
		
		public override List<PcmFrame> ToPcmFrames(byte[] bytes)
		{
			WriteToStream(bytes);

			var frames = new List<PcmFrame>();
			var state = DecodingState.None;
			
			do 
			{
				state = DecodeBytesToPcmSamples(out var pcmData);

				var pcmFrames = WritePcmFrames(pcmData.ToArray());

				frames.AddRange(pcmFrames);
				_samplesCount += pcmData.Length;

			} while (state == DecodingState.Success);
			
			Debug.Log($"State: {state}, Eof: {_mpegFile.Reader.EndOfStream}, Bytes: [{_writeCount}], Samples: {_samplesCount}");

			if (state == DecodingState.EndOfStream)
				ResetDecoder(_mpegFile.Position);
			
			return frames;
		}

		private void ResetDecoder(long position)
		{
			_mpegFile = new MpegFile(_stream);
			_mpegFile.Position = position;
		}
	}
}