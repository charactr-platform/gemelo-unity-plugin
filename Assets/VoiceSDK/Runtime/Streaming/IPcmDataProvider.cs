using System;
using Gemelo.Voice.Audio;

namespace Gemelo.Voice.Streaming
{
	internal interface IPcmDataProvider
	{
		Action<PcmFrame> OnPcmFrame { get; set; }
		void AddRawData(byte[] data);
		bool HasData();
		AudioClipBuilder CreateAudioBuilder(AudioDataType dataType, int sampleRate);
		int BufferPcmFrames();
		bool BufferLastFrame();
		void Dispose();
	}
}