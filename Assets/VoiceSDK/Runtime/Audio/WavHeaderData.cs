using System;
using System.Text;

namespace Charactr.VoiceSDK.Audio
{
	public struct WavHeaderData
	{
		public int SampleRate { get; }
		public int AudioFormat { get; }
		public int Channels { get; }
		public int BitDepth { get; }
		public int DataOffset { get; }
		public bool IsExtensibeWav { get; }

		private const int ExtensibleWavFormat = 65534;
		
		public WavHeaderData(byte[] data)
		{
			SampleRate = AudioFormat = Channels = BitDepth = DataOffset = -1;
			IsExtensibeWav = false;
			
			if (!IsWavFile(ref data))
				throw new Exception("Can't find WAVE file header");

			(AudioFormat, DataOffset, IsExtensibeWav) = ReadFormatData(ref data);
			(Channels, SampleRate, BitDepth) = ReadWavStreamData(ref data);
		}

		private (int, int, bool) ReadFormatData(ref byte[] data)
		{
			int subchunkSize = BitConverter.ToInt32(data, 16);
			var audioFormat = BitConverter.ToUInt16(data, 20);
			var fact = Encoding.ASCII.GetString(data, subchunkSize + 20, 4);
			
			var extensible = audioFormat == ExtensibleWavFormat &&
			                 fact.Equals("FACT", StringComparison.OrdinalIgnoreCase);
				
			//TODO: Validate PCM wav header
			var headerOffset = extensible ? subchunkSize + 20 + 16 : 44;
			
			return (audioFormat, headerOffset, extensible);
		}

		private (int, int, int) ReadWavStreamData(ref byte[] data)
		{
			var channels = BitConverter.ToUInt16 (data, 22);
			var sampleRate = BitConverter.ToInt32 (data, 24);
			var bitDepth = BitConverter.ToUInt16 (data, 34);
			return (channels, sampleRate, bitDepth);
		}
		
		private bool IsWavFile(ref byte[] data)
		{
			var riff = Encoding.ASCII.GetString (data, 0, 4);
			var wave = Encoding.ASCII.GetString (data, 8, 4);
			return riff.Equals("RIFF") && wave.Equals("WAVE");
		}
	}
}