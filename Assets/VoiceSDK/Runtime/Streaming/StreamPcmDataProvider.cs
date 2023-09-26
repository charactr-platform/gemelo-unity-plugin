using System;
using System.Collections.Generic;
using System.IO;
using Gemelo.Voice.Audio;

namespace Gemelo.Voice.Streaming
{
	public class StreamPcmDataProvider : MemoryStream, IPcmDataProvider
	{
		public Action<PcmFrame> OnPcmFrame { get; set; }
		public AudioClipBuilder AudioClipBuilder => _builder;

		private const int PRE_BUFFER_SIZE = 4096;
		
		private readonly BinaryWriter _writer;
		private AudioClipBuilder _builder;
		private int _readPosition = 0;
		public StreamPcmDataProvider()
		{
			_writer = new BinaryWriter(this);
		}
		
		public void AddRawData(byte[] data)
		{
			_writer.Write(data);
			_writer.Flush();
		}

		public bool HasData() => Length > _readPosition + PRE_BUFFER_SIZE;

		public AudioClipBuilder CreateAudioBuilder(AudioParameters parameters)
		{
			Span<byte> header = null;
			
			switch (parameters.AudioDataType)
			{
				case AudioDataType.MP3:
					ReadNextBuffer(out header, Mp3Builder.HeaderSize);
					_builder = new Mp3Builder(parameters.SampleRate, header.ToArray());
					break;
				
				case AudioDataType.WAV:
					ReadNextBuffer(out header, WavBuilder.HeaderSize);
					_builder = new WavBuilder(parameters.SampleRate, header.ToArray());
					break;
			}
			
			return _builder;
		}
		
		private int ReadNextBuffer(out Span<byte> output, int bytesCount = 1024)
		{
			var buffer = GetBuffer();

			if (buffer.Length < bytesCount)
				bytesCount = buffer.Length;
			
			output = buffer.AsSpan(_readPosition, bytesCount);
			_readPosition += output.Length;
			return output.Length;
		}
		
		public int CreatePcmFramesFromData()
		{
			var count = 0;
			
			while (HasData())
			{
				count += PushDataToBuilder();
			}

			return count;
		}

		private int PushDataToBuilder(int readSize = PRE_BUFFER_SIZE)
		{
			ReadNextBuffer(out var buffer, readSize);
			var frames = _builder.ToPcmFrames(buffer.ToArray());
			return BufferForAudioClip(frames);
		}
		
		private int BufferForAudioClip(List<PcmFrame> frames)
		{
			var count = frames.Count;
			
			for (int i = 0; i < count ; i++)
			{
				var frame = frames[i];
				_builder.BufferSamples(frame);
				OnPcmFrame?.Invoke(frame);
			}

			return count;
		}

		public bool BufferLastFrame()
		{
			var leftBytes = Position - _readPosition;
		
			PushDataToBuilder((int) leftBytes);

			if (!_builder.BufferLastFrame(out var frame))
				return false;

			BufferForAudioClip(new List<PcmFrame>(){frame});
			
			return true;
		}
	}
}