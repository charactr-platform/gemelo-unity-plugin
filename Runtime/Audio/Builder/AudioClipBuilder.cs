using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public enum AudioDataType
	{
		WAV = 1,
		MP3 = 2, 
	}

	public abstract class AudioClipBuilder
	{
		public int EmptySamples => _silenceSamplesCount;
		public int ProcessedSamplesCount => _processedSamplesCount;
		public int SampleRate => _sampleRate;
		public int BitDepth => _bitDepth;
		public float Duration => ProcessedSamplesCount / (float)SampleRate;
		public bool AwaitsForData => _awaitsForData;
		
		private int _processedSamplesCount = 0;
		private int _playbackPosition = 0;
		private int _silenceSamplesCount = 20000;
		
		private readonly int _sampleRate;
		private readonly int _bitDepth;
		private List<float> _samplesBuffer;
		private AudioClip _clip;
		private PcmFrame _currentFrame;
		private readonly Queue<PcmFrame> _frames;
		private bool _awaitsForData;

		protected AudioClipBuilder(int sampleRate, int bitDepth)
		{
			_bitDepth = bitDepth;
			_sampleRate = sampleRate;
			_samplesBuffer = new List<float>();
			_frames = new Queue<PcmFrame>();
			CreateNewPcmFrame(_bitDepth);
		}

		public abstract List<PcmFrame> ToPcmFrames(byte[] bytes); 
		
		protected List<PcmFrame> WritePcmFrames(ArraySegment<float> samples)
		{
			if (!_currentFrame.AddPcmData(samples, out var overflow))
				return DequeueLastFrames();
			
			//Enqueue full frame and create new one to write to 
			_frames.Enqueue(_currentFrame);
			CreateNewPcmFrame(_bitDepth);
			return WritePcmFrames(overflow);
		}
		
		protected List<PcmFrame> WritePcmFrames(ArraySegment<byte> samples)
		{
			if (!_currentFrame.AddPcmData(samples, out var overflow))
				return DequeueLastFrames();
			
			_frames.Enqueue(_currentFrame);
			CreateNewPcmFrame(_bitDepth);
			return WritePcmFrames(overflow);
		}

		public bool BufferLastFrame(out PcmFrame frame)
		{
			if (!_currentFrame.HasData)
			{
				frame = null;
				return false;
			}
			
			frame = _currentFrame;
			return true;
		}
		
		private List<PcmFrame> DequeueLastFrames()
		{
			var list = new List<PcmFrame>();

			//Store size before decrementing queue, this is important
			var queueSize = _frames.Count;
			
			for (int i = 0; i < queueSize; i++)
			{
				if (_frames.TryDequeue(out var frame))
					list.Add(frame);
			}
			
			return list;
		}
		
		private void CreateNewPcmFrame(int bitDepth)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			_currentFrame = new PcmFrame(WebGlAudioBufferProcessor.BufferSize, bitDepth);
#else
			_currentFrame = new PcmFrame(4096, bitDepth);
#endif
		}
		
		public AudioClip CreateAudioClipStream(string name, int seconds = 30)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			_clip = AudioClip.Create(name, _sampleRate * seconds, 1, _sampleRate, false);
#else
			_clip = AudioClip.Create(name, _sampleRate * seconds, 1, _sampleRate, true, PcmReaderCallback);
#endif
			Debug.Log($"Created AudioClip [Buffer duration: {Duration}, Rate: {_sampleRate}, Bits: {_bitDepth}, CH: {_clip.channels}, BufferLength: {_clip.length}, Type: {_clip.loadType}]");
		
			return _clip;
		}
		
		public float BufferSamples(PcmFrame frame)
		{
			_processedSamplesCount += frame.Samples.Length;
			_samplesBuffer.AddRange(frame.Samples);
			var length = _processedSamplesCount / (_sampleRate * 1f);
			return length;
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
			_awaitsForData = playbackTime == 0;
		}

		public void Dispose()
		{
			_samplesBuffer = null;
		}
	}
}