using System;
using System.Text;

namespace Gemelo.Voice.Editor.Library
{
	public static class CrcHelper
	{
		public static ushort CRC16(int intValue)
		{
			return CRC16_Simple(BitConverter.GetBytes(intValue));
		}

		public static ushort CRC16(string stringValue)
		{
			return CRC16_Simple(Encoding.Default.GetBytes(stringValue));
		}
		
		//Credits:
		//https://www.alfasanayi.com/content/img/ILT/Sample_CRC_cACalcAulating_algorithm.pdf
		private static ushort CRC16_Simple(byte[] bytes)
		{
			const ushort generator = 0x8005;
			ushort crc = 0; 
			foreach (byte b in bytes)
			{
				crc ^= (ushort)(b << 8);
				
				for (int i = 0; i < 8; i++)
				{
					if ((crc & 0x8000) != 0)
						crc = (ushort)((crc << 1) ^ generator);
					else {
						crc <<= 1; }
				}
			}
			return crc;
		}
	}
}