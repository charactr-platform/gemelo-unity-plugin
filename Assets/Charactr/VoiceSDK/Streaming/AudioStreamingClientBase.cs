using System;
using System.Collections;
using System.Collections.Generic;
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
		public int AudioSamples { get; private set; }
		private WavBuilder WavBuilder { get; set; }

		private const int MinimalFrameCount = 4;
		
		private AudioClip _clip = null;
		private readonly Queue<string> _commands;
		private readonly Queue<byte[]> _dataQueue;
		private int _frameCount = 0, _totalFramesRead, _enqueuedFrames;
		private readonly MonoBehaviour _behaviour;
		private readonly Configuration _configuration;

		protected AudioStreamingClientBase(Configuration configuration, GameObject behaviour)
		{
			_commands = new Queue<string>();
			_dataQueue = new Queue<byte[]>();

			_configuration = configuration;
			_behaviour = behaviour.GetComponent<MonoBehaviour>();
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
				_enqueuedFrames++;
				_dataQueue.Enqueue(data);
			}
		}

		public void DepleteBufferQueue()
		{
			if (IsDataQueueEmpty())
			{
				CheckForBufferEnd();
				return;
			}

			lock (_dataQueue)
			{
				if (WavBuilder == null)
				{
					CreateWavBuilderFromHeader(_dataQueue.Dequeue());
					return;
				}

				do
				{
					LoadData(_dataQueue.Dequeue());
				} 
				while (_dataQueue.Count != 0);
			}
		}

		private bool IsDataQueueEmpty()
		{
			lock (_dataQueue)
			{
				return _dataQueue.Count == 0;
			}
		}
		
		private void LoadData(byte[] data)
		{
			AudioLength = WavBuilder.BufferData(data, out var pcmData);
			AudioSamples += pcmData.Length;
			
			OnPcmData(_frameCount, pcmData);
			
			_frameCount++;
			
			if (_frameCount == MinimalFrameCount)
			{
				_behaviour.StartCoroutine(LoadAudioClipBuffer());
			}

			Debug.Log($"On LoadData: {_frameCount}/{_enqueuedFrames}");
		}

		private void CreateWavBuilderFromHeader(byte[] data)
		{
			WavBuilder = new WavBuilder(data);
			_frameCount = 1;
			_totalFramesRead = 0;
			BufferingCompleted = false;
			AudioLength = 0f;
			AudioSamples = 0;
		}
		
		private void CheckForBufferEnd()
		{
			if (Initialized && !Connected && _totalFramesRead != 0)
			{
				Debug.Log($"Buffer loaded [{_totalFramesRead}]: {AudioLength}s");
				_totalFramesRead = 0;
				BufferingCompleted = true;
			}
		}

		private IEnumerator LoadAudioClipBuffer()
		{
			var clip = WavBuilder.CreateAudioClipStream("test");
			yield return new WaitUntil(() => clip.LoadAudioData());
			
			Debug.Log($"Loaded audio clip...{clip.loadState}");
			//Assign clip when Audio is loaded
			_clip = clip;
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
			WavBuilder = null;
			_clip = null;
			_commands.Clear();
			
			if (!IsDataQueueEmpty())
				Debug.LogWarning("Data left in queue");

			_dataQueue.Clear();
			Debug.Log("Disposed streaming client");
		}
		
		public abstract void Connect();
		protected abstract bool IsConnected();
		public abstract void Play();
		protected abstract void Send(string text);
		protected abstract void OnPcmData(int frameIndex, float[] buffer);
		public virtual void SendConvertCommand(string text) => Send(GetConvertCommand(text));
		protected virtual void OnError(string obj) => Debug.LogError("Error: " + obj);

		protected virtual void OnClose(string obj)
		{
			_totalFramesRead = _frameCount;
			Debug.Log("Closed: " + obj);
		}
		
		protected string GetAuthCommand() 
		{
			var authData = new AuthCommand()
			{
				ApiKey = _configuration.ApiKey,
				ClientKey = _configuration.ApiClient,
			};
			
			return JsonConvert.SerializeObject(authData);
		}

		private string GetConvertCommand(string text)
		{
			var textCommand = new ConvertCommand()
			{
				Text = text
			};

			return JsonConvert.SerializeObject(textCommand);
		}
	}

	struct AuthCommand
	{
		[JsonProperty(PropertyName = "type")] public string Type => "authApiKey";
		[JsonProperty(PropertyName = "clientKey")] public string ClientKey { get; set; }
		[JsonProperty(PropertyName = "apiKey")] public string ApiKey { get; set; }
	}

	struct ConvertCommand
	{
		[JsonProperty(PropertyName = "type")] public string Type => "convert";
		[JsonProperty(PropertyName = "text")] public string Text { get; set; }
	}
}