using System;
using Charactr.VoiceSDK.SDK;
using UnityEngine;

namespace GptDemo.Streaming
{
	public class CustomAudioStreamingClient : AudioStreamingClientBase
	{
		private readonly AudioSource _audioSource;
		public CustomAudioStreamingClient(AudioSource audioSource) : base(null, audioSource.gameObject)
		{
			_audioSource = audioSource;
		}
		
		public void Disconnect(string reason) => OnClose.Invoke(reason);
		
		public void FillBuffer(byte[] data) => OnData.Invoke(data);
		public override void Connect() => OnOpen.Invoke();
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