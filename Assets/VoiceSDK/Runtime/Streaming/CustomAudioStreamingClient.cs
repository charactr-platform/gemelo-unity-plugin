
using gemelo.VoiceSDK.Audio;

namespace gemelo.VoiceSDK.Streaming
{
	public class CustomAudioStreamingClient : AudioStreamingClientBase
	{
		public CustomAudioStreamingClient() : base(null) { }
		
		public void Disconnect(string reason) => OnClose(reason);
		public void FillBuffer(byte[] data) => OnData(data);
		public override void Connect() => OnOpen();
		protected override bool IsConnected() => true;

		protected override void Send(string text) { }
		protected override void OnPcmFrame(int frameIndex, PcmFrame pcmFrame) { }
	}
}