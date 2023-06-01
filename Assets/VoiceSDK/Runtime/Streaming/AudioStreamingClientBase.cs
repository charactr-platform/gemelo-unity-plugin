using System.Collections;
using System.Collections.Generic;
using Charactr.VoiceSDK.Audio;
using Newtonsoft.Json;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
	
	public abstract class AudioStreamingClientBase
	{
		public const int SampleSize = 1024;
		public bool Connected => IsConnected();
		public bool Initialized => _clip != null;
		public AudioClip AudioClip => _clip;
		public bool BufferingCompleted { get; private set; }
		public float AudioLength { get; private set; }
		public int AudioSamples { get; private set; }
		private WavBuilder WavBuilder { get; set; }

		private const int MinimalFrameCount = 5;
		
		private AudioClip _clip = null;
		private readonly Queue<string> _commands;
		private readonly Queue<byte[]> _dataQueue;
		private int _frameCount, _totalFramesRead;
		private readonly MonoBehaviour _behaviour;
		private readonly Configuration _configuration;
		private readonly WavDebugSave _debugSave;

		#region Amplitude

		private readonly int _sampleSize;
		private readonly float[] _sample;
		private float _avgTotal = 0;
		private float _maxTotal = 0;
		private float _max;
		private const float BoostBase = 1f;
		private const float Boost = 0.2f;

		#endregion

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
				_dataQueue.Enqueue(data);
			}
		}

		public void DepleteBufferQueue()
		{
			lock (_dataQueue)
			{
				if (WavBuilder == null && !IsDataQueueEmpty())
				{
					CreateWavBuilderFromHeader(_dataQueue.Dequeue());
					return;
				}

				while (!IsDataQueueEmpty())
				{
					LoadData(_dataQueue.Dequeue());
				} 
				
				CheckForBufferEnd();
			}
		}

		private bool IsDataQueueEmpty()
		{
			return _dataQueue.Count == 0;
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
		}

		private void CreateWavBuilderFromHeader(byte[] data)
		{
			WavBuilder = new WavBuilder(data);
			_clip = null;
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
		public float GetSampleAverage(float[] sample)
		{
			float tempVal = 0;
			
			_avgTotal = 0;
			_maxTotal = 0;

			for (int i = 0; i < sample.Length; i++)
			{
				// Get the sample value
				tempVal = sample[i];
				// Get the absolute value
				tempVal = Mathf.Abs(tempVal);
				// Add boost
				tempVal = Mathf.Pow(tempVal, BoostBase - Boost);
				// Write boosted value back to the original sample
				sample[i] = tempVal;

				_avgTotal += sample[i];

				if (sample[i] > _maxTotal)
					_maxTotal = sample[i];
			}
			
			_max = _maxTotal;
			return _avgTotal / _sampleSize;
		}
		
		public virtual void Dispose()
		{
			WavBuilder.Dispose();
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