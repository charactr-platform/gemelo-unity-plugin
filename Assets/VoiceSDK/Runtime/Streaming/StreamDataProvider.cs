using System;
using System.Collections.Generic;
using System.IO;
using Gemelo.Voice.Audio;

namespace Gemelo.Voice.Streaming
{
	internal class StreamDataProvider : IDataProvider
	{
		private readonly MemoryStream _memory;
		private readonly BinaryWriter _writer;
		
		private PcmFrame _currentPcmFrame;
		private int _bufferReadout = 0;
		private readonly Queue<PcmFrame> _pcmFrames;
		public StreamDataProvider()
		{
			_memory = new MemoryStream();
			_writer = new BinaryWriter(_memory);
			_pcmFrames = new Queue<PcmFrame>();
			CreateNewPcmFrame();
		}
		public void AddRawData(byte[] data)
		{
			_writer.Write(data);
			_writer.Flush();
		}

		public bool HasData()
		{
			return _bufferReadout < _memory.Length;
		}

		public void ReadHeaderData(AudioDataType dataType, out byte[] header)
		{
			header = null;

			
			_memory.Seek(0, SeekOrigin.Begin);
			
			//Assuming 44 bytes for Wav file
			if (dataType == AudioDataType.Wav)
			{
				header = new byte[44];
				_memory.Read(header, 0, 44);
			}

			if (dataType == AudioDataType.Mp3)
			{
				header = new byte[_memory.Length];
				_memory.Read(header);
			}

			_memory.Seek(0, SeekOrigin.Begin);
		}

		public bool ReadPcmFrames(out List<PcmFrame> pcmFrames)
		{
			bool framesFound = false;
			pcmFrames = new List<PcmFrame>();
			
			while (HasData())
			{
				var bufferSize = _memory.Seek(0, SeekOrigin.End) - _bufferReadout;
				var buffer = new byte[bufferSize];
				
				_memory.Seek(_bufferReadout, SeekOrigin.Begin);
				_bufferReadout += _memory.Read(buffer);
				
				CreateFrameData(buffer);
					
				for (int i = 0; i < _pcmFrames.Count; i++)
				{
					if (_pcmFrames.TryDequeue(out var frame))
						pcmFrames.Add(frame);
				}

				framesFound = pcmFrames.Count > 0;
			}

			return framesFound;
		}

		public void CreateFrameData(Span<byte> data)
		{
			if (!_currentPcmFrame.AddData(data.ToArray(), out var overflow))
				return;
			
			_pcmFrames.Enqueue(_currentPcmFrame);
			
			CreateNewPcmFrame();
			CreateFrameData(overflow);
		}

		public bool ReadLastFrame(out PcmFrame frame)
		{
			frame = null;

			if (!_currentPcmFrame.HasData)
				return false;
			
			_currentPcmFrame.WriteSamples(true);
			frame = _currentPcmFrame;
			return true;
		}

		private void CreateNewPcmFrame()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			_currentPcmFrame = new PcmFrame(WebGlAudioBufferProcessor.BufferSize);
#else
			_currentPcmFrame = new PcmFrame();
#endif
		}
		
		public void Dispose()
		{
			_pcmFrames.Clear();
			_writer.Close();
			CreateNewPcmFrame();
		}
	}
}