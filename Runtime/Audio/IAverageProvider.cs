namespace Gemelo.Voice.Audio
{
	public interface IAverageProvider
	{
		public const int SampleSize = 1024;
		float GetSampleAverage(float[] sample);
	}
}