using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

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

		public AudioClip CreateAudioClipStream(string name, int seconds = 10)
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
			PcmFrame.ConvertByteToFloat(_data.ToArray(), out var waveData, _header.DataOffset);
			_clip = AudioClip.Create(name, waveData.Length, _header.Channels, _header.SampleRate, false);
			_clip.SetData(waveData, 0);
			return _clip;
		}

		public float BufferData(PcmFrame frame)
		{
			_lastBytesReadCount += PcmFrame.ByteSize;
			_processedSamplesCount += frame.Samples.Length;
			_samplesBuffer.AddRange(frame.Samples);
			var length = _processedSamplesCount / (_header.SampleRate * 1f);
			
			Debug.Log("Loaded bytes: "+ _lastBytesReadCount + " audioSamples: "+ _processedSamplesCount + " length:"+length);
			return length;
		}
		
		public void WriteAudioClipDataToFile()
		{
			if (_debugSave != null)
			{
				_debugSave.ConvertAndWrite(_samplesBuffer.ToArray());
				_debugSave.Close();
			}
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
		
		
		public void Dispose()
		{
			_debugSave?.Close();
		}
	}
}