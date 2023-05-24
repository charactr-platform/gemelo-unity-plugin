using UnityEngine;

namespace Charactr.VoiceSDK.Streaming
{
	public interface IAudioStreamingClient
	{
		public AudioSource AudioSource { get; }
		public AudioClip AudioClip { get; }
		public float AudioLength { get; }
		public int AudioSamples { get; }
		public bool BufferingCompleted { get; }
		public bool Initialized { get; }
		public void Connect();
		public void Play();
		public void SendConvertCommand(string text);
		public void DepleteBufferQueue();
		public void Dispose();
	}
}