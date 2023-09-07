using System;
using System.IO;
using Gemelo.Voice.Audio;

namespace Gemelo.Voice.Streaming
{
	internal class StreamPcmDataProvider : IPcmDataProvider
	{
		public Action<PcmFrame> OnPcmFrame { get; set; }
		public AudioClipBuilder AudioClipBuilder { get => _builder; }
		private readonly MemoryStream _memory;
		private readonly BinaryWriter _writer;
		private AudioClipBuilder _builder;
		private int _bufferReadout = 0;
		public StreamPcmDataProvider()
		{
			_memory = new MemoryStream();
			_writer = new BinaryWriter(_memory);
		}
		
		public void AddRawData(byte[] data)
		{
			_writer.Write(data);
			_writer.Flush();
		}

		public bool HasData()
		{
			return _bufferReadout + 1024 < _memory.Length;
		}

		public AudioClipBuilder CreateAudioBuilder(AudioDataType dataType, int sampleRate)
		{
			byte[] header = null;

			var offset = 0;
			_memory.Seek(offset, SeekOrigin.Begin);
			
			//Assuming 44 bytes for Wav file
			if (dataType == AudioDataType.Wav)
			{
				var headerSize = 44;
				header = new byte[headerSize];
				_memory.Read(header, 0, headerSize);
				offset = headerSize;
			}

			//And all current data for mp3,as we copy it later to another stream
			if (dataType == AudioDataType.Mp3)
			{
				header = new byte[_memory.Length];
				_memory.Read(header);
				offset = header.Length;
			}

			_memory.Seek(offset, SeekOrigin.Begin);
			
			switch (dataType)
			{
				case AudioDataType.Mp3:
					_builder = new Mp3Builder(sampleRate, header);
					break;
				
				case AudioDataType.Wav:
					_builder = new WavBuilder(sampleRate, header);
					break;
				
				case AudioDataType.None:
					throw new Exception("AudioData type not selected!");
			}
			
			return _builder;
		}
		
		public int BufferPcmFrames()
		{
			var count = 0;
			
			while (HasData())
			{
				var bufferSize = _memory.Seek(0, SeekOrigin.End) - _bufferReadout;
				var buffer = new byte[bufferSize];
				
				_memory.Seek(_bufferReadout, SeekOrigin.Begin);
				_bufferReadout += _memory.Read(buffer);

				var frames = _builder.ToPcmFrames(buffer);

				count = frames.Count;
				
				for (int i = 0; i < count; i++)
				{
					var frame = frames[i];
					_builder.BufferPcmFrame(frame);
					OnPcmFrame?.Invoke(frame);
				}
			}

			return count;
		}
		
		public bool BufferLastFrame()
		{
			return _builder.BufferLastFrame();
		}
		
		public void Dispose()
		{
			_writer.Close();
		}
	}
}