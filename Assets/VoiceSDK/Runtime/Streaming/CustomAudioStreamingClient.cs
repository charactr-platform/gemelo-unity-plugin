using System;
using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
	public class CustomAudioStreamingClient : AudioStreamingClientBase
	{
		private readonly AudioSource _audioSource;
		public CustomAudioStreamingClient(AudioSource audioSource) : base(null)
		{
			_audioSource = audioSource;
		}
		
		public void Disconnect(string reason) => OnClose(reason);
		public void FillBuffer(byte[] data) => OnData(data);
		public override void Connect() => OnOpen();
		protected override bool IsConnected() => true;
		public override void Play()
		{
			if (!Initialized)
				throw new Exception("Not initialized, await for AudioClip data");
			
			_audioSource.clip = AudioClip;
			_audioSource.Play();
		}

		protected override void Send(string text) { }
		protected override void OnPcmData(int frameIndex, float[] buffer) { }
	}
}