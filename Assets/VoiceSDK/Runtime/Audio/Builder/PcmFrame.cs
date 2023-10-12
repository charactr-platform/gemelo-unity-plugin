using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	[Serializable]
	public class PcmFrame: IDisposable
	{
		public static int BlockSize16 = sizeof(short);
		public static int BlockSize32 = sizeof(float);
		public int BlockSize => _blockSize;
		public int SamplesSize => _samplesSize;
		public float[] Samples => _samples.ToArray();
		public bool HasData => _samples.Count > 0 ;
		public int BitDepth => _bitDepth;
		
		[SerializeField] private List<float> _samples;
		
		private readonly int _samplesSize;
		private readonly string _id;
		private readonly int _bitDepth;
		private readonly int _blockSize;
		
		public PcmFrame(int samplesCount = 4096, int bitDepth = 16)
		{
			_bitDepth = bitDepth;
			_samplesSize = samplesCount;
			_blockSize = BitDepthToBlockSize(bitDepth);
			_samples = new List<float>();
		}
		
		public PcmFrame(string id, int samplesCount = 4096) : this(samplesCount) => _id = id;
		public override string ToString() => _id;
		
		public bool AddPcmData(ArraySegment<byte> data, out byte[] overflow)
		{
			float[] samples = null;

			var initialSize = _samples.Count;
			var offset = (SamplesSize * BlockSize) - (initialSize * BlockSize);
			
			if (data.Count > offset)
			{
				ConvertByteToFloat(data.Slice(0,offset).ToArray(), out samples,0, _blockSize);
				overflow = data.Slice(offset, data.Count - offset).ToArray();
				AddPcmData(samples, out _);
				return true;
			}
			
			ConvertByteToFloat(data.ToArray(), out samples,0, _blockSize);
			overflow = null;
			AddPcmData(samples, out _);
			
			return false;
		}

		public bool AddPcmData(ArraySegment<float> pcm, out float[] overflow)
		{
			var initialSize = _samples.Count;
			var offset = SamplesSize - initialSize;
			
			if (pcm.Count > offset)
			{
				_samples.AddRange(pcm.Slice(0, offset));

				overflow = pcm.Slice(offset, pcm.Count - offset).ToArray();
				
				return true;
			}
			
			_samples.AddRange(pcm);

			overflow = null;
			
			return false;
		}
		
		public static int BitDepthToBlockSize(int bitDepth) => bitDepth > 16 ? BlockSize32 : BlockSize16;
		public static void ConvertByteToFloat(byte[] data, out float[] waveData, int offset = 0, int blockSize = 2)
		{
			var i = 0;
			
			var samplesSize = (data.Length - offset) / blockSize;
			waveData = new float[samplesSize];

			while (i < samplesSize)
			{
				var pos = (i * blockSize) + offset;
				float v = 0;

				if (blockSize == BlockSize32)
					v = BitConverter.ToSingle(data, pos);
				else
					v = (float) BitConverter.ToInt16(data, pos) / short.MaxValue;
				
				waveData[i] = v;
				++i;
			}

			data = null;
		}

		public void Dispose()
		{
			_samples = null;
			GC.Collect();
		}
	}
}