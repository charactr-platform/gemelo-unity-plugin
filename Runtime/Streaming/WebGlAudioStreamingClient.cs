using System;
using Charactr.VoiceSDK.Audio;
using NativeWebSocket;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
	public class WebGlAudioStreamingClient : AudioStreamingClientBase, IAudioStreamingClient
	{
		public AudioSource AudioSource => _audioSource;

		private readonly NativeWebSocket.WebSocket _socket;
		private readonly AudioSource _audioSource;
		private readonly GameObject _gameObject;
		private WebGlAudioBufferProcessor _bufferProcessor;
		
		public WebGlAudioStreamingClient(string url, Configuration configuration, AudioSource audioSource) : base(configuration)
		{
			_audioSource = audioSource;
			
			_socket = new NativeWebSocket.WebSocket(url);
			
			_socket.OnOpen += OnOpen;
			_socket.OnClose += code => OnClose(code.ToString());
			_socket.OnError += OnError;
			_socket.OnMessage += OnData;
		}
		
		public override void Play()
		{
			if (!Initialized)
				throw new Exception("Not initialized, await for AudioClip data");
			
			_audioSource.clip = AudioClip;
			_audioSource.Play();
			_bufferProcessor.StartSampling(AudioClip);
		}

		protected override void OnPcmFrame(int frameIndex, PcmFrame frame)
		{
			//Send buffer in with zero based index (Wav Header is frameIndex = 0)
			_bufferProcessor.OnPcmBuffer(frameIndex - 1, frame.Samples);
		}

		protected override void OnHeaderData(int sampleRate)
		{
			_bufferProcessor = new WebGlAudioBufferProcessor(AverageProvider.SampleSize);
		}

		public override void Connect()
		{
			EnqueueCommand(GetAuthCommand());
			_socket.Connect();
		}

		//Close stream manually

		public override void Dispose()
		{
			_bufferProcessor.StopSampling();
			base.Dispose();
			
			if (Connected)
				_socket.Close();
		}

		public float GetAverage()
		{
			return GetSampleAverage(_bufferProcessor.GetSample());
		}

		protected override bool IsConnected() =>
			_socket.State == WebSocketState.Open;
		
		protected override void Send(string text)
		{
			if (Connected)
				_socket.SendText(text);
			else
				EnqueueCommand(text);
		}
	}
}