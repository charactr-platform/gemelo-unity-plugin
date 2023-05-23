using System;
using System.Collections.Generic;
using Charactr.SDK.Streaming;
using Charactr.VoiceSDK;
using Charactr.VoiceSDK.Streaming;
using UnityEngine;
using WebSocketSharp;

namespace GptDemo.Streaming
{
	public class DefaultAudioStreamingClient : AudioStreamingClientBase
	{
		private readonly WebSocket _socket;
		private readonly AudioSource _audioSource;
		public DefaultAudioStreamingClient(string url, Configuration configuration, AudioSource audioSource): base(configuration, audioSource.gameObject)
		{
			_socket = new WebSocket(url);
			_socket.OnOpen += (sender, args) => OnOpen.Invoke();
			_socket.OnClose += (sender, args) => OnClose.Invoke(args.Reason);
			_socket.OnError += (sender, args) => OnError.Invoke(args.Message);
			_socket.OnMessage += (sender, args) => OnData.Invoke(args.RawData);
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