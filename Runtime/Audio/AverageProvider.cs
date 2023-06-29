using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public class AverageProvider: IAverageProvider
	{
		private const float BoostBase = 1f;
		private const float Boost = 0.265f;
		
		private float _avgTotal = 0;
		private float _maxTotal = 0;
		private readonly float _boost;
		private readonly float _boostBase;
		
		public AverageProvider(float boost = Boost, float boostBase = BoostBase)
		{
			_boost = boost;
			_boostBase = boostBase;
		}
		
		public float GetSampleAverage(float[] sample)
		{
			int size = sample.Length;
			
			float tempVal = 0;
			
			_avgTotal = 0;
			_maxTotal = 0;

			for (int i = 0; i < size; i++)
			{
				// Get the sample value
				tempVal = sample[i];
				// Get the absolute value
				tempVal = Mathf.Abs(tempVal);
				// Add boost
				tempVal = Mathf.Pow(tempVal, _boostBase - _boost);
				// Write boosted value back to the original sample
				sample[i] = tempVal;

				_avgTotal += sample[i];

				if (sample[i] > _maxTotal)
					_maxTotal = sample[i];
			}
			
			return _avgTotal / size;
		}
	}
}