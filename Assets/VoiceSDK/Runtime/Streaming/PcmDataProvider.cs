using System;
using System.Collections.Generic;
using Gemelo.Voice.Audio;

namespace Gemelo.Voice.Streaming
{
	internal class PcmDataProvider : IDataProvider
	{
		private readonly Queue<byte[]> _dataQueue;
		private readonly Queue<PcmFrame> _pcmFrames;
			
		private PcmFrame _currentPcmFrame;
		
		public PcmDataProvider()
		{
			_dataQueue = new Queue<byte[]>();
			_pcmFrames = new Queue<PcmFrame>();
			CreateNewPcmFrame();
		}

		public void AddRawData(byte[] data)
		{
			lock (_dataQueue)
			{
				_dataQueue.Enqueue(data);
			}
		}

		public bool HasData()
		{
			lock (_dataQueue)
			{
				return _dataQueue.Count > 0;
			}
		}
		
		/// <summary>
		/// Read header data and remove it from PCM frames buffer readout
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="header"></param>
		public void ReadHeaderData(AudioDataType dataType, out byte[] header)
		{
			lock (_dataQueue)
			{
				var tempArray = _dataQueue.ToArray();
				var headerPushback = false;
				header = tempArray[0];
				
				//Assuming 44 bytes for Wav file
				if (dataType == AudioDataType.Wav && header.Length > 44)
				{
					tempArray[0] = header.AsSpan(44).ToArray();
					headerPushback = true;
				}
				
				if (tempArray.Length <= 1)
					return;
				
				//Push back items to queue if more items found, without header
				_dataQueue.Clear();
					
				for (int i = headerPushback ? 0 : 1 ; i < tempArray.Length; i++)
					_dataQueue.Enqueue(tempArray[i]);
			}
		}
			
		public bool ReadPcmFrames(out List<PcmFrame> pcmFrames)
		{
			bool framesFound = false;
			pcmFrames = new List<PcmFrame>();
				
			lock (_dataQueue)
			{
				while (HasData())
				{
					CreateFrameData(_dataQueue.Dequeue());
						
					for (int i = 0; i < _pcmFrames.Count; i++)
					{
						if (_pcmFrames.TryDequeue(out var frame))
							pcmFrames.Add(frame);
					}

					framesFound = pcmFrames.Count > 0;
				}

				return framesFound;
			}
		}

		public void CreateFrameData(Span<byte> data)
		{
			if (!_currentPcmFrame.AddData(data.ToArray(), out var overflow))
				return;
			
			_pcmFrames.Enqueue(_currentPcmFrame);
			
			CreateNewPcmFrame();
			CreateFrameData(overflow);
		}
			
		private void CreateNewPcmFrame()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			_currentPcmFrame = new PcmFrame(WebGlAudioBufferProcessor.BufferSize);
#else
			_currentPcmFrame = new PcmFrame();
#endif
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

		public void Dispose()
		{
			lock (_dataQueue)
			{
				_dataQueue.Clear();
				_pcmFrames.Clear();
			}
		}
	}
}