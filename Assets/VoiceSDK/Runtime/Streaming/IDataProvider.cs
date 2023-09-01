using System;
using System.Collections.Generic;
using Gemelo.Voice.Audio;

namespace Gemelo.Voice.Streaming
{
	internal interface IDataProvider
	{
		void AddRawData(byte[] data);
		bool HasData();

		/// <summary>
		/// Read header data and remove it from PCM frames buffer readout
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="header"></param>
		void ReadHeaderData(AudioDataType dataType, out byte[] header);

		bool ReadPcmFrames(out List<PcmFrame> pcmFrames);
		void CreateFrameData(Span<byte> data);
		bool ReadLastFrame(out PcmFrame frame);
		void Dispose();
	}
}