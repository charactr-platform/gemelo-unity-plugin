using System;
using Gemelo.Voice.Audio;

namespace Gemelo.Voice.Streaming
{
	public interface IPcmDataProvider
	{
		AudioClipBuilder AudioClipBuilder { get; }
		Action<PcmFrame> OnPcmFrame { get; set; }
		void AddRawData(byte[] data);
		bool HasData();
		AudioClipBuilder CreateAudioBuilder(AudioDataType dataType, int sampleRate);
		int BufferPcmFrames();
		bool BufferLastFrame();
		void Dispose();
	}
}