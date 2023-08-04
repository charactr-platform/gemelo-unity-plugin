using System;
using System.Collections.Generic;
using Gemelo.Voice.Audio;
using NativeWebSocket;
using UnityEngine;

namespace Gemelo.Voice.Streaming
{
	public class WebGlAudioStreamingClient : AudioStreamingClientBase, IAudioStreamingClient
	{
		private readonly NativeWebSocket.WebSocket _socket;
		private readonly GameObject _gameObject;
		private WebGlAudioBufferProcessor _bufferProcessor;
		
		public WebGlAudioStreamingClient(string url, Configuration configuration, int maxLength = 30) : base(configuration, maxLength)
		{
			var sampleRate = WebGlAudioBufferProcessor.GetSupportedSampleRate();

			if (sampleRate == -1)
				throw new Exception("Can't read sample rate from Browser AudioContext!");
			
			var header = new Dictionary<string, string>()
			{
				{"user-agent", Configuration.USER_AGENT}
			};
			
			_socket = new NativeWebSocket.WebSocket(AddAudioFormat(url, sampleRate), header);
	
			_socket.OnOpen += OnOpen;
			_socket.OnClose += code => OnClose(code.ToString());
			_socket.OnError += OnError;
			_socket.OnMessage += OnData;
		}
		protected override void OnPcmFrame(int frameIndex, PcmFrame frame)
		{
			WebGlAudioBufferProcessor.OnPcmBuffer(frame.Samples);
		}
		
		public override void Connect()
		{
			EnqueueCommand(GetAuthCommand());
			_socket.Connect();
		}

		//Close stream manually
		public override void Dispose()
		{
			base.Dispose();
			
			if (Connected)
				_socket.Close();
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