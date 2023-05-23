using System;
using System.Runtime.InteropServices;
using Charactr.VoiceSDK.SDK;
using GptDemo.Streaming;
using NativeWebSocket;
using UnityEngine;

namespace Charactr.SDK.Streaming
{
	//TODO: Implement GetAverage() method from `original` Amplitude.cs
	
	public class WebGlAudioStreamingClient : AudioStreamingClientBase
	{
		
		[DllImport("__Internal")]
		private static extern bool WebGL_StartSampling(string uniqueName, int bufferIndex, int sampleSize, bool streaming = false);

		[DllImport("__Internal")]
		private static extern bool WebGL_StopSampling(string uniqueName);
		
		[DllImport("__Internal")]
		private static extern bool WebGL_Stats();
		[DllImport("__Internal")]
		public static extern int WebGL_GetBufferInstanceOfLastAudioClip();
		
		[DllImport("__Internal")]
		private static extern void WebGL_FillBuffer(float[] array, int size, int index);
		
		private readonly NativeWebSocket.WebSocket _socket;
		private readonly AudioSource _audioSource;
		private readonly GameObject _gameObject;
		private bool _autoPlay;
		public WebGlAudioStreamingClient(string url,Configuration configuration, AudioSource audioSource) :
					base(configuration, audioSource.gameObject)
		{
			_audioSource = audioSource;
			
			_socket = new NativeWebSocket.WebSocket(url);
			_socket.OnOpen += () => OnOpen.Invoke();
			_socket.OnClose += code => OnClose.Invoke(code.ToString());
			_socket.OnError += msg => OnError.Invoke(msg);
			_socket.OnMessage += data => OnData.Invoke(data);
		}
		
		public override void Play()
		{
			if (!Initialized)
				throw new Exception("Not initialized, await for AudioClip data");

			var clip = AudioClip;
			var clipId = clip.GetInstanceID().ToString();
			var sampleSize = 32;
			
			_audioSource.clip = clip;
			_audioSource.Play();
			
			var bufferIndex = WebGL_GetBufferInstanceOfLastAudioClip();
			WebGL_StartSampling(clipId, bufferIndex, sampleSize, true);
		}
		
		protected override void OnPcmData(int frameIndex, float[] buffer)
		{
			//Send buffer in with zero based index (Wav Header is frameIndex = 0)
			WebGL_FillBuffer(buffer,  buffer.Length, frameIndex - 1);
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