using System;
using System.Collections.Generic;
using Gemelo.Voice.Audio;
using Newtonsoft.Json;
using UnityEngine;

namespace Gemelo.Voice.Streaming
{
	public abstract class AudioStreamingClientBase
	{
		public bool Connected => IsConnected();
		public bool Initialized => _clip != null;
		public AudioClip AudioClip => _clip;
		public AudioDataType DataType => _dataType;
		public int SampleRate => _sampleRate;
		public bool BufferingCompleted { get; private set; }
		public float AudioLength { get; private set; }
		public int TimeSamples { get; private set; }
		private AudioClipBuilder AudioClipBuilder { get; set; }

		private readonly Queue<string> _commands;
		private readonly Queue<byte[]> _dataQueue;
		private readonly Configuration _configuration;
		private readonly WavDebugSave _debugSave;
		private readonly Queue<PcmFrame> _pcmFrames;
		private readonly AudioDataType _dataType;
		private readonly int _maxClipLenght;
		private readonly int _sampleRate;

		private AudioClip _clip = null;
		private PcmFrame _currentPcmFrame;
		private int _frameCount, _totalFramesRead;

		protected AudioStreamingClientBase(Configuration configuration, AudioDataType dataType, int sampleRate, int maxClipLenght)
		{
			_commands = new Queue<string>();
			_dataQueue = new Queue<byte[]>();
			_pcmFrames = new Queue<PcmFrame>();
			_configuration = configuration;
			_maxClipLenght = maxClipLenght;
			_dataType = dataType;
			_sampleRate = sampleRate;
		}

		protected void EnqueueCommand(string command)
		{
			_commands.Enqueue(command);
		}

		//Invoke on main thread 
		protected void OnData(byte[] data)
		{
			lock (_dataQueue)
			{
				_dataQueue.Enqueue(data);
			}
		}

		protected string AddAudioFormat(string url)
		{
			if (DataType == AudioDataType.None)
				throw new Exception("Please select AudioDataType!");
			
			switch (SampleRate)
			{
				//Some default sampling rate values
				case 48000:
				case 44100:
				case 32000:
				case 22050:
					Debug.Log($"Transcoder sampling rate set: {SampleRate}");
					break;
                
				default:
					throw new Exception($"Can't set unsupported transcoder sampling rate: {SampleRate}");
			}
			
			return url + $"&format={DataType.ToString().ToLower()}&sr={SampleRate}";
		}
		
		public void DepleteBufferQueue()
		{
			lock (_dataQueue)
			{
				bool HasData() => _dataQueue.Count > 0;
				
				if (HasData() && AudioClipBuilder == null)
				{
					CreateAudioBuilderInstance(_dataQueue.Dequeue());
					return;
				}

				while (HasData())
				{
					CreateFrameData(_dataQueue.Dequeue());
					
					for (int i = 0; i < _pcmFrames.Count; i++)
					{
						if (_pcmFrames.TryDequeue(out var frame))
							BufferPcmFrameData(frame);
					}
				}
			}

			CheckForBufferEnd();
		}

		private void CreateAudioBuilderInstance(byte[] initialData)
		{
			Initialize();
			
			switch (DataType)
			{
				case AudioDataType.Mp3:
					AudioClipBuilder = new Mp3Builder(SampleRate, initialData);
					break;
				case AudioDataType.Wav:
					AudioClipBuilder = new WavBuilder(SampleRate, initialData);
					break;
				case AudioDataType.None:
					throw new Exception("AudioData type not selected!");
			}
		}

		private void CreateFrameData(Span<byte> data)
		{
			if (!_currentPcmFrame.AddData(data.ToArray(), out var overflow))
				return;
			
			_pcmFrames.Enqueue(_currentPcmFrame);
			
			CreateNewPcmFrame();
			CreateFrameData(overflow);
		}
		
		private void BufferPcmFrameData(PcmFrame frame)
		{
			AudioLength = AudioClipBuilder.BufferData(frame);
			TimeSamples = AudioClipBuilder.ProcessedSamplesCount + AudioClipBuilder.EmptySamples;
			
			//WebGL needs first buffer before start of sampling
			OnPcmFrame(_frameCount, frame);

			//Buffer some data before we start audio play, 1 sec approx.
			var startFrame = Mathf.RoundToInt((float) AudioClipBuilder.SampleRate / frame.Samples.Length);
			
			if (_frameCount == startFrame)
			{
				Debug.Log($"Creating audio clip, buffered length: {AudioLength}sec.");
				CreateAudioClip();
			}
			
			frame.Dispose();
			
			_frameCount++;
		}
		
		private void Initialize()
		{
			_clip = null;
			_frameCount = 1;
			_totalFramesRead = 0;
			BufferingCompleted = false;
			AudioLength = 0f;
			TimeSamples = 0;
			CreateNewPcmFrame();
		}

		private void CreateNewPcmFrame()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			_currentPcmFrame = new PcmFrame(WebGlAudioBufferProcessor.BufferSize);
#else
			_currentPcmFrame = new PcmFrame();
#endif
		}
		private void CreateAudioClip()
		{
			var clip = AudioClipBuilder.CreateAudioClipStream("test", _maxClipLenght);
			
			if (clip.LoadAudioData() == false)
				throw new Exception("Data not loaded");
			
			Debug.Log($"Loaded audio clip...{clip.loadState}");
		
			_clip = clip;
		}
		
		private void CheckForBufferEnd()
		{
			if (Initialized && !Connected && _totalFramesRead != 0)
			{
				if (_currentPcmFrame.HasData)
				{
					_currentPcmFrame.WriteSamples(true);
					BufferPcmFrameData(_currentPcmFrame);
					CreateNewPcmFrame();
				}

				Debug.Log($"Buffer loaded [{_totalFramesRead}]: {AudioLength}s");
				_totalFramesRead = 0;
				BufferingCompleted = true;
			}
		}

		protected void OnOpen()
		{
			while (_commands.Count > 0)
			{
				Send(_commands.Dequeue());
			}
		}
		
		public virtual void Dispose()
		{
			AudioClipBuilder.Dispose();
			AudioClipBuilder = null;
			_clip = null;
			_commands.Clear();
			_dataQueue.Clear();
			_pcmFrames.Clear();
			Debug.Log("Disposed streaming client");
		}
		
		public abstract void Connect();
		protected abstract bool IsConnected();
		protected abstract void Send(string text);
		public virtual void SendConvertCommand(string text) => Send(GetConvertCommand(text));
		protected virtual void OnError(string obj) => Debug.LogError("Error: " + obj);
		protected virtual void OnPcmFrame(int frameIndex, PcmFrame pcmFrame) { }

		protected virtual void OnClose(string obj)
		{
			_totalFramesRead = _frameCount;
			Debug.Log("Closed: " + obj);
		}

		protected string GetAuthCommand() => 
			GetAuthCommand(_configuration.ApiKey, _configuration.ApiClient);
		
		public static string GetAuthCommand(string apiKey, string clientKey) 
		{
			var authData = new AuthCommand()
			{
				ApiKey = apiKey,
				ClientKey = clientKey,
			};
			
			return JsonConvert.SerializeObject(authData);
		}

		public static string GetConvertCommand(string text)
		{
			var textCommand = new ConvertCommand()
			{
				Text = text
			};

			return JsonConvert.SerializeObject(textCommand);
		}
	}

	public struct AuthCommand
	{
		[JsonProperty(PropertyName = "type")] public string Type => "authApiKey";
		[JsonProperty(PropertyName = "clientKey")] public string ClientKey { get; set; }
		[JsonProperty(PropertyName = "apiKey")] public string ApiKey { get; set; }
	}

	public struct ConvertCommand
	{
		[JsonProperty(PropertyName = "type")] public string Type => "convert";
		[JsonProperty(PropertyName = "text")] public string Text { get; set; }
	}
}