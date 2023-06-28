using System.Runtime.InteropServices;
using UnityEngine;

namespace Charactr.VoiceSDK.Audio
{
	public class WebGlAudioBufferProcessor
	{
		public const int BufferSize = 4096;
		
		[DllImport("__Internal")]
		private static extern bool WebGL_Initialize(int bufferSize, int allocationSize);
		[DllImport("__Internal")]
		private static extern bool WebGL_StartSampling(string uniqueName, int bufferIndex, int sampleSize, bool streaming = false);

		[DllImport("__Internal")]
		private static extern bool WebGL_StopSampling(string uniqueName);
		
		[DllImport("__Internal")]
		private static extern bool WebGL_Stats();
		[DllImport("__Internal")]
		public static extern int WebGL_GetBufferInstanceOfLastAudioClip();
		
		[DllImport("__Internal")]
		private static extern void WebGL_FillBuffer(float[] array, int size);
		[DllImport("__Internal")]
		private static extern bool WebGL_GetAmplitude(string uniqueName, float[] sample, int sampleSize);

		private string _clipId;
		private readonly int _sampleSize;
		private readonly float[] _sample;
		private bool _streaming;
		public WebGlAudioBufferProcessor(int sampleSize, bool streaming)
		{
			_streaming = streaming;
			_sampleSize = sampleSize;
			_sample = new float[sampleSize];
			
			//Allocate heap memory buffer to fix buffer growth
			//BUG: https://github.com/emscripten-core/emscripten/issues/6747
			//TODO: Calculate approximate buffer size from letters used in text 

			if (streaming)
			{
				var memAllocSize = int.MaxValue / 8; //~30mb
				WebGL_Initialize(BufferSize, memAllocSize);
			}
		}

		public void StartSampling(AudioClip clip)
		{
			_clipId = clip.GetInstanceID().ToString();
			var bufferIndex = WebGL_GetBufferInstanceOfLastAudioClip();
			WebGL_StartSampling(_clipId, bufferIndex, _sampleSize, _streaming);
		}
		public float[] GetSample()
		{
			WebGL_GetAmplitude(_clipId, _sample, _sampleSize);
			return _sample;
		}
		
		public static void OnPcmBuffer(float[] buffer)
		{
			WebGL_FillBuffer(buffer,  buffer.Length);
		}

		public void StopSampling()
		{
			WebGL_StopSampling(_clipId);
		}
	}
}