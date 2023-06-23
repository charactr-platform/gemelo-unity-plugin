using System;
using System.IO;
namespace Charactr.VoiceSDK.Audio
{
	public class PcmFrame
	{
		public static int BlockSize = sizeof(short);//16bit per sample 
		public int ByteSize => _bytesSize;
		public float[] Samples => _samples;
		public bool HasData => _bytes?.Length > 0 && _samples == null;
		
		private readonly MemoryStream _bytes;
	
		private float[] _samples;
		private readonly int _bytesSize;

		public PcmFrame(int samplesCount = 4096)
		{
			_bytesSize = samplesCount * BlockSize;
			_bytes = new MemoryStream();
		}
			
		public bool AddData(byte[] data, out byte[] overflow)
		{
			if (_bytes.Length < ByteSize)
				_bytes.Write(data);
				
			if (_bytes.Length > ByteSize)
			{
				overflow = WriteSamples();
				return true;
			}
				
			overflow = null;

			return false;
		}

		public byte[] WriteSamples(bool endOfData = false)
		{
			var bytesCount = endOfData ? (int) _bytes.Length : ByteSize;

			if (!_bytes.TryGetBuffer(out var segment))
				return null;
			
			var frameBytes = segment.Slice(0, bytesCount);
				
			ConvertByteToFloat(frameBytes.ToArray(), out _samples);

			var overflow = segment.Slice(bytesCount).ToArray();
			
			_bytes.Dispose();

			return endOfData ? null : overflow;
		}
		
		public static void ConvertByteToFloat(byte[] data, out float[] waveData, int offset = 0)
		{
			var i = 0;
			
			var samplesSize = (data.Length - offset) / BlockSize;
			waveData = new float[samplesSize];

			while (i < samplesSize)
			{
				var pos = (i * BlockSize) + offset;
				var v = (float)BitConverter.ToInt16(data, pos) / short.MaxValue;
				waveData[i] = v;
				++i;
			}
		}
	}
}