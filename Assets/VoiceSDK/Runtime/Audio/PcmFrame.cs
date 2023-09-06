using System;
using System.IO;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public class PcmFrame: IDisposable
	{
		public static int BlockSize = sizeof(short);//16bit per sample 
		public int ByteSize => _bytesSize;
		public float[] Samples => _samples.ToArray();
		public bool HasData => _bytes?.Length > 0 && _samples.IsEmpty;
		
		private MemoryStream _bytes;
		private Memory<float> _samples;
		private readonly int _bytesSize;

		public PcmFrame(int samplesCount = 4096)
		{
			_bytesSize = samplesCount * BlockSize;
			_bytes = new MemoryStream();
			_samples = new Memory<float>();
		}
			
		public bool AddData(byte[] data, out byte[] overflow)
		{
			if (_bytes.Length < ByteSize)
				_bytes.Write(data);
				
			if (_bytes.Length >= ByteSize)
			{
				overflow = WriteSamples();
				return true;
			}
				
			overflow = null;

			return false;
		}

		public bool AddPcmData(float[] pcm, out float[] overflow)
		{
			pcm.AsMemory().CopyTo(_samples);
			
			var offset = ByteSize / BlockSize;

			if (_samples.Length > offset)
			{
				overflow = _samples.Slice(offset).ToArray();
				Debug.LogWarning($"Offset data: {overflow.Length}");
				return true;
			}
			
			overflow = Array.Empty<float>();
			return false;
		}
		
		public byte[] WriteSamples(bool endOfData = false)
		{
			var bytesCount = endOfData ? (int) _bytes.Length : ByteSize;

			if (!_bytes.TryGetBuffer(out var segment))
				return null;
			
			var frameBytes = segment.Slice(0, bytesCount);
				
			ConvertByteToFloat(frameBytes.ToArray(), out var samples);
			
			AddPcmData(samples, out _);
			
			var overflow = segment.Slice(bytesCount).ToArray();

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

			data = null;
		}

		public void Dispose()
		{
			_samples = null;
			_bytes.Dispose();
			_bytes = null;
			
			GC.Collect();
		}
	}
}