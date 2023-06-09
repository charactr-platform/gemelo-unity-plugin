using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;

namespace Charactr.VoiceSDK.Audio
{
	public class PcmFrame
	{
		public const int ByteSize = 8192;
		public float[] Samples => _samples;
		public bool HasData => _bytes?.Length > 0 && _samples == null;
		
		private readonly MemoryStream _bytes;
		private Memory<byte> _frameBytes;
		private float[] _samples;

		public PcmFrame()
		{
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
			
			_frameBytes = segment.Slice(0, bytesCount);
				
			ConvertByteToFloat(_frameBytes.ToArray(), out _samples);

			return endOfData ? null : segment.Slice(ByteSize).ToArray();
		}
		
		public static int ConvertByteToFloat(Span<byte> data, out float[] waveData, int offset = 0)
		{
			var pos = 0;
			var size = data.Length;
			var blockSize = sizeof(short);
			
			byte[] audioData = data.ToArray();

			waveData = new float[size / blockSize];

			for (int i = 0; i < waveData.Length; i++)
			{
				pos = (i * blockSize) + offset;
				
				waveData[i] = ConvertBytes(audioData,pos, blockSize);
			}
			
			return size;
		}

		private static float ConvertBytes(byte[] data, int index, int size)
		{
			var mem = new ReadOnlySpan<byte>(data, index, size);
			
			return BinaryPrimitives.ReadInt16LittleEndian(mem) / 32768f;
		}
	}
}