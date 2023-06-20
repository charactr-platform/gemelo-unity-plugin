using System;
using System.IO;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public class WavDebugSave
	{
		public bool Closed => _closed;
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
		
		public void ConvertAndWrite(float[] samples) {
			
			Int16[] intData = new Int16[samples.Length];
			//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

			Byte[] bytesData = new Byte[samples.Length * 2];
			//bytesData array is twice the size of
			//dataSource array because a float converted in Int16 is 2 bytes.

			int rescaleFactor = 32767; //to convert float to Int16

			for (int i = 0; i < samples.Length; i++) {
				intData[i] = (short) (samples[i] * rescaleFactor);
				Byte[] byteArr = new Byte[2];
				byteArr = BitConverter.GetBytes(intData[i]);
				byteArr.CopyTo(bytesData, i * 2);
			}

			_stream.Write(bytesData, 0, bytesData.Length);
			
			Close();
		}
	}
}