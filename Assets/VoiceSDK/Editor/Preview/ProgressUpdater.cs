using System;

namespace Charactr.VoiceSDK.Editor.Preview
{
	public class ProgressUpdater : IProgress<float>
	{
		public float Value { get; private set; }
		private readonly Action<float> _onUpdate;

		public void Report(float value)
		{
			Value = value;
			_onUpdate.Invoke(value);
		}

		public ProgressUpdater(Action<float> onProgress)
		{
			_onUpdate = onProgress;
		}
	}
}