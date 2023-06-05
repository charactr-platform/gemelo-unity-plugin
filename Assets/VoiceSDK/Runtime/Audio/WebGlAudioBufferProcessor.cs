using System.Runtime.InteropServices;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public class WebGlAudioBufferProcessor
	{
		[DllImport("__Internal")]
		private static extern bool WebGL_StartSampling(string uniqueName, int bufferIndex, int sampleSize, bool streaming = false);

		[DllImport("__Internal")]
		private static extern bool WebGL_StopSampling(string uniqueName);
		
		[DllImport("__Internal")]
		private static extern bool WebGL_Stats();
		[DllImport("__Internal")]
		public static extern int WebGL_GetBufferInstanceOfLastAudioClip();
		
		[DllImport("__Internal")]
		private static extern void WebGL_FillBuffer(float[] array, int size, int index);
		[DllImport("__Internal")]
		private static extern bool WebGL_GetAmplitude(string uniqueName, float[] sample, int sampleSize);

		private string _clipId;
		private readonly int _sampleSize;
		private float[] _sample;
		public WebGlAudioBufferProcessor(int sampleSize)
		{
			_sampleSize = sampleSize;
			_sample = new float[sampleSize];
		}

		public void StartSampling(AudioClip clip, bool streaming = true)
		{
			_clipId = clip.GetInstanceID().ToString();
			var bufferIndex = WebGL_GetBufferInstanceOfLastAudioClip();
			WebGL_StartSampling(_clipId, bufferIndex, _sampleSize, streaming);
		}
		public float[] GetSample()
		{
			WebGL_GetAmplitude(_clipId, _sample, _sampleSize);
			return _sample;
		}
		
		public void OnPcmBuffer(int index, float[] buffer)
		{
			WebGL_FillBuffer(buffer,  buffer.Length, index);
		}

		public void StopSampling()
		{
			WebGL_StopSampling(_clipId);
		}
	}
}