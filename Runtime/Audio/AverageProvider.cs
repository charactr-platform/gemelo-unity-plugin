using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public class AverageProvider
	{
		public const int SampleSize = 1024;
		
		private float _avgTotal = 0;
		private float _maxTotal = 0;
		private float _max;
		private const float BoostBase = 1f;
		private const float Boost = 0.2f;
		
		public float GetSampleAverage(float[] sample)
		{
			float tempVal = 0;
			
			_avgTotal = 0;
			_maxTotal = 0;

			for (int i = 0; i < sample.Length; i++)
			{
				// Get the sample value
				tempVal = sample[i];
				// Get the absolute value
				tempVal = Mathf.Abs(tempVal);
				// Add boost
				tempVal = Mathf.Pow(tempVal, BoostBase - Boost);
				// Write boosted value back to the original sample
				sample[i] = tempVal;

				_avgTotal += sample[i];

				if (sample[i] > _maxTotal)
					_maxTotal = sample[i];
			}
			
			_max = _maxTotal;
			return _avgTotal / SampleSize;
		}
	}
}