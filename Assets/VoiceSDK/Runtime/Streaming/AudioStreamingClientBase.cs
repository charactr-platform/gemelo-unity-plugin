using System;
using System.Collections.Generic;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Rest.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace Gemelo.Voice.Streaming
{
	public abstract class AudioStreamingClientBase
	{
		public bool Connected => IsConnected();
		public bool Initialized => _clip != null;
		public AudioClip AudioClip => _clip;
		public AudioDataType DataType => _audioParameters.AudioDataType;
		public int SampleRate => _audioParameters.SampleRate;
		public VoiceType VoiceType => _audioParameters.VoiceType;
		public bool BufferingCompleted { get; private set; }
		public float AudioLength => AudioClipBuilder.Duration;
		public int TimeSamples { get; private set; }
		
		private AudioClipBuilder AudioClipBuilder { get; set; }

		private readonly Queue<string> _commands;
	
		private readonly Configuration _configuration;
		private readonly AudioParameters _audioParameters;

		private AudioClip _clip = null;
		
		private int _frameCount, _totalFramesRead;
		private readonly IPcmDataProvider _pcmDataProvider;

		protected AudioStreamingClientBase(Configuration configuration, AudioParameters audioParameters)
		{
			_commands = new Queue<string>();
			_pcmDataProvider = new StreamPcmDataProvider();
			_configuration = configuration;
			_audioParameters = audioParameters;
			
			//Fix cases where it sets defaulted to All in inspector
			if (VoiceType.Equals(VoiceType.All))
				audioParameters.SetVoiceType(VoiceType.All);
		}

		protected void EnqueueCommand(string command)
		{
			_commands.Enqueue(command);
		}

		//Invoke on main thread 
		protected void OnData(byte[] data)
		{
			_pcmDataProvider.AddRawData(data);
		}

		protected string AddAudioFormat(string url)
		{
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

			var audio = DataType.ToString().ToLower();
			var voice = VoiceType.ToString().ToLower();
			
			return url + $"&format={audio}&sr={SampleRate}&voiceType={voice}";
		}
		
		public void DepleteBufferQueue()
		{
			var hasData = _pcmDataProvider.HasData();
			
			if (hasData && AudioClipBuilder == null)
			{
				AudioClipBuilder = _pcmDataProvider.CreateAudioBuilder(_audioParameters);
				Initialize();
				return;
			}
			
			BufferPcmFramesData();

			CheckForBufferEnd();
		}
		
		private void BufferPcmFramesData()
		{
			var processedCount = _pcmDataProvider.CreatePcmFramesFromData();
			
			if (processedCount == 0)
				return;
			
			Debug.Log($"Added frames: {processedCount}");
			TimeSamples = AudioClipBuilder.ProcessedSamplesCount + AudioClipBuilder.EmptySamples;

			if (_clip == null && AudioLength > 0.5f)
			{
				Debug.Log($"Creating audio clip, buffered length: {AudioLength} sec.");
				_clip = CreateAudioClip();
			}

			_frameCount += processedCount;
		}
		
		private void Initialize()
		{
			_clip = null;
			_frameCount = 1;
			_totalFramesRead = 0;
			BufferingCompleted = false;
			TimeSamples = 0;
			_pcmDataProvider.OnPcmFrame = frame => OnPcmFrame(_frameCount, frame);
		}

		
		private AudioClip CreateAudioClip()
		{
			var clip = AudioClipBuilder.CreateAudioClipStream("test", _audioParameters.MaxClipLenght);
			
			if (clip.LoadAudioData() == false)
				throw new Exception("Data not loaded");
			
			Debug.Log($"Loaded audio clip...{clip.loadState}");
		
			return clip;
		}
		
		private void CheckForBufferEnd()
		{
			if (Initialized && !Connected && _totalFramesRead != 0)
			{
				_pcmDataProvider.BufferLastFrame();
				Debug.Log($"Buffer loaded [{_totalFramesRead}] - [{AudioClipBuilder.ProcessedSamplesCount}]: {AudioLength}s");
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
			_pcmDataProvider.Dispose();
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