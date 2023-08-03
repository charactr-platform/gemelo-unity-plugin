using UnityEngine;

namespace Gemelo.VoiceSDK.Streaming
{
	public interface IAudioStreamingClient
	{
		public AudioClip AudioClip { get; }
		public float AudioLength { get; }
		public int TimeSamples { get; }
		public bool BufferingCompleted { get; }
		public bool Initialized { get; }
		public void Connect();
		public void SendConvertCommand(string text);
		public void DepleteBufferQueue();
		public void Dispose();
	}
}