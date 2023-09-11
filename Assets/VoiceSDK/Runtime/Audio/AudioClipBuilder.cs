using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gemelo.Voice.Audio
{
	public enum AudioDataType
	{
		Wav = 1,
		Mp3 = 2, 
	}

	public abstract class AudioClipBuilder
	{
		public int EmptySamples => _silenceSamplesCount;
		public int ProcessedSamplesCount => _processedSamplesCount;
		public int SampleRate => _sampleRate;
		
		private int _processedSamplesCount = 0;
		private int _playbackPosition = 0;
		private int _silenceSamplesCount = 0;
		
		private readonly int _sampleRate;
		private List<float> _samplesBuffer;
		private AudioClip _clip;
		private PcmFrame _currentFrame;
		private readonly Queue<PcmFrame> _frames;

		protected AudioClipBuilder(int sampleRate)
		{
			_sampleRate = sampleRate;
			_samplesBuffer = new List<float>();
			_frames = new Queue<PcmFrame>();
			CreateNewPcmFrame();
		}

		public abstract List<PcmFrame> ToPcmFrames(byte[] bytes); 
		
		protected List<PcmFrame> WritePcmFrames(ArraySegment<float> samples)
		{
			if (!_currentFrame.AddPcmData(samples, out var overflow))
				return DequeueLastFrames();
			
			//Enqueue full frame and create new one to write to 
			_frames.Enqueue(_currentFrame);
			CreateNewPcmFrame();
			return WritePcmFrames(overflow);
		}
		
		protected List<PcmFrame> WritePcmFrames(ArraySegment<byte> samples)
		{
			if (!_currentFrame.AddPcmData(samples, out var overflow))
				return DequeueLastFrames();
			
			_frames.Enqueue(_currentFrame);
			CreateNewPcmFrame();
			return WritePcmFrames(overflow);
		}

		public bool BufferLastFrame()
		{
			if (!_currentFrame.HasData)
				return false;
			
			BufferPcmFrame(_currentFrame);
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
		
		private void CreateNewPcmFrame()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			_currentFrame = new PcmFrame(WebGlAudioBufferProcessor.BufferSize);
#else
			_currentFrame = new PcmFrame();
#endif
		}
		
		public AudioClip CreateAudioClipStream(string name, int seconds = 30)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			_clip = AudioClip.Create(name, _sampleRate * seconds, 1, _sampleRate, false);
#else
			_clip = AudioClip.Create(name, _sampleRate * seconds, 1, _sampleRate, true, PcmReaderCallback);
#endif
			Debug.Log($"Created AudioClip [Rate: {_sampleRate}, CH: {_clip.channels}, Length: {_clip.length}, Type: {_clip.loadType}]");
		
			return _clip;
		}
		
		public float BufferPcmFrame(PcmFrame frame)
		{
			_processedSamplesCount += frame.Samples.Length;
			_samplesBuffer.AddRange(frame.Samples);
			var length = _processedSamplesCount / (_sampleRate * 1f);
			Debug.Log($"BufferAdd: [{frame.Samples.Length}/{_processedSamplesCount}] samples [{length}s]");
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
			
			if (playbackTime == 0)
				Debug.Log("awaiting data...");
		}

		public void Dispose()
		{
			_samplesBuffer = null;
		}
	}
}