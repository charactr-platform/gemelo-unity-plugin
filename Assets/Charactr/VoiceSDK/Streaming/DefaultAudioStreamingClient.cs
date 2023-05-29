using System;
using Charactr.VoiceSDK.Tests;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
	public class DefaultAudioStreamingClient : AudioStreamingClientBase, IAudioStreamingClient
	{
		public AudioSource AudioSource => _audioSource;

		private readonly NativeSocketWrapper _socket;
		private readonly AudioSource _audioSource;
		public DefaultAudioStreamingClient(string url, Configuration configuration, AudioSource audioSource): base(configuration, audioSource.gameObject)
		{
			_audioSource = audioSource;
			_socket = new NativeSocketWrapper(url);
			_socket.OnOpen += OnOpen;
			_socket.OnClose += OnClose;
			_socket.OnError += OnError;
			_socket.OnData += OnData;
		}

		protected override void OnPcmData(int frameIndex, float[] buffer) { }
		
		public override void Connect()
		{
			EnqueueCommand(GetAuthCommand());
			_socket.Connect();
		}

		protected override bool IsConnected() =>
			_socket.Status == System.Net.WebSockets.WebSocketState.Open;

		public override void Play()
		{
			if (!Initialized)
				throw new Exception("AudioClip is not initialized");
			
			_audioSource.clip = AudioClip;
			_audioSource.Play();
		}
		
		protected override void Send(string text)
		{
			if (Connected)
				_socket.SendText(text);
			else
				EnqueueCommand(text);
		}
	}
}