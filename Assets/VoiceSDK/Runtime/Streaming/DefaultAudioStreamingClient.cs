using System;
using Charactr.VoiceSDK.Audio;
using Charactr.VoiceSDK.Tests;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
	public class DefaultAudioStreamingClient : AudioStreamingClientBase, IAudioStreamingClient
	{
		private readonly NativeSocketWrapper _socket;
		public DefaultAudioStreamingClient(string url, Configuration configuration, int samplingRate = 44100, int maxLenght = 30): base(configuration, maxLenght)
		{
			_socket = new NativeSocketWrapper(AddAudioFormat(url, samplingRate));
			_socket.OnOpen += OnOpen;
			_socket.OnClose += OnClose;
			_socket.OnError += OnError;
			_socket.OnData += OnData;
		}

		protected override void OnPcmFrame(int frameIndex, PcmFrame pcmFrame) { }

		public override void Connect()
		{
			EnqueueCommand(GetAuthCommand());
			_socket.Connect();
		}

		protected override bool IsConnected() =>
			_socket.Status == System.Net.WebSockets.WebSocketState.Open;
		
		public override void Dispose()
		{
			base.Dispose();
			
			if (Connected)
				_socket.Close();
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