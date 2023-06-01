using System.IO;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public class WavDebugSave
	{
		private readonly Stream _stream;
		private bool _closed;
		private int _position = 0;
		public WavDebugSave(byte[] header)
		{
			_stream = new FileStream(Application.streamingAssetsPath + "../../../DebugSave.wav", FileMode.Create);
			_stream.Write(header,0,header.Length);
			_position += header.Length;
		}

		public void OnData(byte[] data)
		{
			if (_closed)
			{
				Debug.LogWarning("File stream already closed, more data You have ?");
				return;
			}

			var size = data.Length;
			_stream.Write(data,0, size);
			_position += size;
		}

		public bool Close()
		{
			if (_closed) 
				return false;
			
			_stream.Close();
			_stream.Dispose();
			_closed = true;
			_position = 0;
			return true;
		}
	}
}