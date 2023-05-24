using System;
using UnityEngine;
using WebSocketSharp;

namespace Charactr.VoiceSDK.Streaming
{
	public class DefaultAudioStreamingClient : AudioStreamingClientBase, IAudioStreamingClient
	{
		public AudioSource AudioSource => _audioSource;

		private readonly WebSocket _socket;
		private readonly AudioSource _audioSource;
		public DefaultAudioStreamingClient(string url, Configuration configuration, AudioSource audioSource): base(configuration, audioSource.gameObject)
		{
			_audioSource = audioSource;
			_socket = new WebSocket(url);
			_socket.OnOpen += (sender, args) => OnOpen();
			_socket.OnClose += (sender, args) => OnClose(args.Reason);
			_socket.OnError += (sender, args) => OnError(args.Message);
			_socket.OnMessage += (sender, args) => OnData(args.RawData);
		}

		protected override void OnPcmData(int frameIndex, float[] buffer) { }

		
		public override void Connect()
		{
			EnqueueCommand(GetAuthCommand());
			_socket.Connect();
		}
		
		protected override bool IsConnected() =>
			_socket.ReadyState == WebSocketState.Open;

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
				_socket.Send(text);
			else
				EnqueueCommand(text);
		}
	}
}