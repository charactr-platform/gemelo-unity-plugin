using System;
using System.Collections.Generic;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public class WavBuilder
	{
		private readonly Memory<byte> _data;
		private readonly WavHeaderData _header;
		
		private int _lastBytesReadCount = 0;
		private int _wavBufferSamplesLength = 0;
		private int _wavBufferReadLength = 0;
		private List<float> _wavDataBuffer;

		private readonly Queue<float[]> _waveBuffers;

		public WavBuilder(byte[] data)
		{
			_waveBuffers = new Queue<float[]>();
			_data = data;
			_header = new WavHeaderData(data);
		}

		public AudioClip CreateAudioClipStream(string name, int seconds = 30)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			var clip = AudioClip.Create(name, _header.SampleRate * seconds, _header.Channels, _header.SampleRate, false);
#else
			var clip = AudioClip.Create(name, _header.SampleRate * seconds, _header.Channels, _header.SampleRate, true, PcmReaderCallback);
#endif
			Debug.Log($"Created AudioClip instance [Length = {clip.length}, load type = {clip.loadType}]");
			return clip;
		}

		public AudioClip CreateAudioClip(string name = "clip")
		{
			ConvertByteToFloat(_data.ToArray(), out var waveData, _header.DataOffset);
			var clip = AudioClip.Create(name, waveData.Length, _header.Channels, _header.SampleRate, false);
			clip.SetData(waveData, 0);
			return clip;
		}

		public float BufferData(byte[] newData, out float[] pcmData)
		{
			_lastBytesReadCount += ConvertByteToFloat(newData, out var waveData);
			pcmData = waveData;
			
			_wavBufferSamplesLength += waveData.Length;
			_waveBuffers.Enqueue(waveData);
			float length = _wavBufferSamplesLength / (_header.SampleRate * 1f);
			Debug.Log("Loaded bytes: "+ _lastBytesReadCount + " audioSamples: "+ _wavBufferSamplesLength + " length:"+length);
			return length;
		}

		private bool LoadNextBuffer()
		{
			if (_waveBuffers.Count == 0)
				return false;

			var read = _waveBuffers.Dequeue();
			_wavDataBuffer.AddRange(read);

			return read.Length > 0;
		}

		private void PcmReaderCallback(float[] data)
		{
			var readSize = data.Length - 1;
			
			if (_wavDataBuffer == null)
			{
				_wavDataBuffer = new List<float>();
				LoadNextBuffer();
			}
			
			for (int i = 0; i < readSize; i++)
			{
				var readIndex = _wavBufferReadLength + i;
				
				if (readIndex >= _wavDataBuffer.Count)
				{
					if (!LoadNextBuffer())
					{
						data[i] = 0f;
						Debug.LogWarning("Can't load next pcm buffer");
						break;
					}
				}

				try
				{
					data[i] = _wavDataBuffer[readIndex];
				}
				catch (Exception e)
				{
					Debug.LogError($"Exception: {e}, readindex: {readIndex} size: {_wavDataBuffer.Count}");
				}
			}

			_wavBufferReadLength += readSize;
		}

		private int ConvertByteToFloat(byte[] data, out float[] waveData, int offset = 0)
		{
			var pos = 0;
			var size = data.Length - offset;
			var blockSize = sizeof(short);
		
			waveData = new float[size / blockSize];
		
			for (int i = 0; i < waveData.Length; i++)
			{
				pos = offset + (i * blockSize);
				waveData[i] = BitConverter.ToInt16(data, pos) / 32768f;
			}
			return pos;
		}
	}
}