using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Charactr.VoiceSDK.Audio;
using Newtonsoft.Json;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
	public abstract class AudioStreamingClientBase
	{
		public bool Connected => IsConnected();
		public bool Initialized => _clip != null;
		public AudioClip AudioClip => _clip;
		public bool BufferingCompleted { get; private set; }
		public float AudioLength { get; private set; }
		public int TimeSamples { get; private set; }
		private WavBuilder WavBuilder { get; set; }
		
		private AudioClip _clip = null;
		private readonly Queue<string> _commands;
		private readonly Queue<byte[]> _dataQueue;
		private int _frameCount, _totalFramesRead;
		private readonly Configuration _configuration;
		private readonly WavDebugSave _debugSave;
		private readonly AverageProvider _averageProvider;
		
		protected AudioStreamingClientBase(Configuration configuration)
		{
			_commands = new Queue<string>();
			_dataQueue = new Queue<byte[]>();

			_averageProvider = new AverageProvider();
			_configuration = configuration;
		}
		protected void EnqueueCommand(string command)
		{
			_commands.Enqueue(command);
		}

		protected void OnData(byte[] data)
		{
			//Invoke on main thread 
			lock (_dataQueue)
			{
				_dataQueue.Enqueue(data);
			}
		}
		public void DepleteBufferQueue()
		{
			lock (_dataQueue)
			{
				if (_dataQueue.Count > 0)
				{
					if (WavBuilder == null)
					{
						CreateWavBuilderFromHeader(_dataQueue.Dequeue());
						return;
					}

					var span = new Span<byte>(_dataQueue.Dequeue());
					LoadData(span);
				}
			}

			CheckForBufferEnd();
		}

		private void LoadData(Span<byte> data)
		{
			AudioLength = WavBuilder.BufferData(data, out var pcmData);
			TimeSamples = WavBuilder.ProcessedSamplesCount + WavBuilder.EmptySamples;
			
			OnPcmData(_frameCount, pcmData);
			_frameCount++;
		}

		private void CreateWavBuilderFromHeader(byte[] data)
		{
			WavBuilder = new WavBuilder(data, true);
			_clip = null;
			_frameCount = 1;
			_totalFramesRead = 0;
			BufferingCompleted = false;
			AudioLength = 0f;
			TimeSamples = 0;
			
			var clip = WavBuilder.CreateAudioClipStream("test");
			if (clip.LoadAudioData() == false)
				throw new Exception("Data not loaded");
			
			Debug.Log($"Loaded audio clip...{clip.loadState}");
			//Assign clip when Audio is loaded
			_clip = clip;
		}
		
		private void CheckForBufferEnd()
		{
			if (Initialized && !Connected && _totalFramesRead != 0)
			{
				Debug.Log($"Buffer loaded [{_totalFramesRead}]: {AudioLength}s");
				_totalFramesRead = 0;
				BufferingCompleted = true;
				WavBuilder.WriteAudioClipDataToFile();
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
			WavBuilder.Dispose();
			WavBuilder = null;
			_clip = null;
			_commands.Clear();
			_dataQueue.Clear();

			Debug.Log("Disposed streaming client");
		}
		
		public abstract void Connect();
		protected abstract bool IsConnected();
		public abstract void Play();

		protected float GetSampleAverage(float[] sample)
		{
			return _averageProvider.GetSampleAverage(sample);
		}
		protected abstract void Send(string text);
		protected abstract void OnPcmData(int frameIndex, float[] buffer);
		
		public virtual void SendConvertCommand(string text) => Send(GetConvertCommand(text));
		protected virtual void OnError(string obj) => Debug.LogError("Error: " + obj);

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