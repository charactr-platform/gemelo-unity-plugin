using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public class WavBuilder
	{
		public int EmptySamples => _silenceSamplesCount;
		public int ProcessedSamplesCount => _processedSamplesCount;
		
		private readonly Memory<byte> _data;
		private readonly WavHeaderData _header;
		
		private int _lastBytesReadCount = 0;
		private int _processedSamplesCount = 0;
		private int _playbackPosition = 0;
		private int _silenceSamplesCount = 0;
		private readonly List<float> _samplesBuffer;
		private readonly WavDebugSave _debugSave;
		private AudioClip _clip;
		public WavBuilder(byte[] data, bool debug = false)
		{
			_samplesBuffer = new List<float>();
			_data = data;
			_header = new WavHeaderData(data);
			if (debug) _debugSave = new WavDebugSave(data);
		}

		public AudioClip CreateAudioClipStream(string name, int seconds = 30)
		{
			var rate = _header.SampleRate;
#if UNITY_WEBGL && !UNITY_EDITOR
			_clip = AudioClip.Create(name, rate * seconds, 1, rate, false);
#else
			_clip = AudioClip.Create(name, rate * seconds, 1, rate, true, PcmReaderCallback);
#endif
			Debug.Log($"Created AudioClip [Rate: {rate}, CH: {_header.Channels}, Length: {_clip.length}, Type: {_clip.loadType}]");
		
			return _clip;
		}
		public AudioClip CreateAudioClip(string name = "clip")
		{
			ConvertByteToFloat(_data.ToArray(), out var waveData, _header.DataOffset);
			_clip = AudioClip.Create(name, waveData.Length, _header.Channels, _header.SampleRate, false);
			_clip.SetData(waveData, 0);
			return _clip;
		}

		public float BufferData(Span<byte> newData, out float[] pcmData)
		{
			_lastBytesReadCount += ConvertByteToFloat(newData, out var waveData);
			pcmData = waveData;
			_samplesBuffer.AddRange(waveData);
			_processedSamplesCount += waveData.Length;

			var length = _processedSamplesCount / (_header.SampleRate * 1f);
			
			Debug.Log("Loaded bytes: "+ _lastBytesReadCount + " audioSamples: "+ _processedSamplesCount + " length:"+length);
			return length;
		}
		
		public void WriteAudioClipDataToFile()
		{
			_debugSave.ConvertAndWrite(_samplesBuffer.ToArray());
			_debugSave.Close();
		}
		
		private void PcmReaderCallback(float[] data)
		{
			var readSize = data.Length;
			var skipped = 0;
			
			for (int i = 0; i < readSize; i++)
			{
				var readIndex = _playbackPosition + i;
				
				if (readIndex >= _samplesBuffer.Count)
				{
					data[i] = 0f;
					skipped++;
				}
				else
					data[i] = _samplesBuffer[readIndex];
			}

			var playbackTime = readSize - skipped;
			_playbackPosition += playbackTime;
			_silenceSamplesCount += skipped;
			
			if (playbackTime == 0)
				Debug.Log("awaiting data...");
		}
		
		private int ConvertByteToFloat(Span<byte> data, out float[] waveData, int offset = 0)
		{
			var pos = 0;
			var size = data.Length;
			var blockSize = sizeof(short);
			
			waveData = new float[size / blockSize];

			var audioData = data.ToArray();
			
			for (int i = 0; i < waveData.Length; i++)
			{
				pos = (i * blockSize);
				
				waveData[i] = ConvertBytes(audioData,pos, blockSize);
			}
			
			return size;
		}

		private float ConvertBytes(byte[] data, int index, int size)
		{
			var mem = new ReadOnlySpan<byte>(data, index, size);
			
			return BinaryPrimitives.ReadInt16LittleEndian(mem) / 32768f;
		}
		
		public void Dispose()
		{
			_debugSave?.Close();
		}
	}
}