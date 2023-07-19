using System;
using Charactr.VoiceSDK.Audio;
using NativeWebSocket;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
	public class WebGlAudioStreamingClient : AudioStreamingClientBase, IAudioStreamingClient
	{
		private readonly NativeWebSocket.WebSocket _socket;
		private readonly GameObject _gameObject;
		private WebGlAudioBufferProcessor _bufferProcessor;
		
		public WebGlAudioStreamingClient(string url, Configuration configuration) : base(configuration)
		{
			var sampleRate = WebGlAudioBufferProcessor.GetSupportedSampleRate();

			if (sampleRate == -1)
				throw new Exception("Can't read sample rate from Browser AudioContext!");
			
			_socket = new NativeWebSocket.WebSocket(AddAudioFormat(url, sampleRate));
			
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