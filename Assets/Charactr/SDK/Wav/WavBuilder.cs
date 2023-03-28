using System;
using UnityEngine;

namespace Charactr.SDK.Wav
{
	public class WavBuilder
	{
		private readonly byte[] _data;
		private readonly WavHeaderData _header;
		public WavBuilder(byte[] data)
		{
			_data = data;
			_header = new WavHeaderData(_data);
		}
		
		public AudioClip CreateAudioClip(string name = "clip")
		{
			var data = ConvertByteToFloat(_header.DataOffset);
			var clip = AudioClip.Create(name, data.Length, _header.Channels, _header.SampleRate, false);
			clip.SetData(data, 0);
			return clip;
		}
		
		private float BytesToFloat(byte firstByte, byte secondByte) {
			// convert two bytes to one short (little endian)
			short s = (short) ((secondByte << 8) | firstByte);
			// convert to range from -1 to (just below) 1
			return s / 32768.0F;
		}
		private float[] ConvertByteToFloat(int offset)
		{
			var size = _data.Length - offset;
			var blockSize = sizeof(short);
		
			var floatArr = new float[size / blockSize];
			var endian = BitConverter.IsLittleEndian;
			
			for (int i = 0; i < floatArr.Length; i++)
			{
				var pos = offset + (i * blockSize);
				
				floatArr[i] = BytesToFloat(endian ? _data[pos] : _data[pos-1],
					endian ? _data[pos-1] : _data[pos] );
			}
			
			return floatArr;
		}
	}
}