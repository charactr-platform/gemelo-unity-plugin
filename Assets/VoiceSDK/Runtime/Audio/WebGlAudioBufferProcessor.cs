using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace gemelo.VoiceSDK.Audio
{
	public class WebGlAudioBufferProcessor
	{
		public const int BufferSize = 4096;
		
#if UNITY_WEBGL
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
		public static extern int WebGL_GetAudioContextSampleRate();

		[DllImport("__Internal")]
		private static extern void WebGL_FillBuffer(float[] array, int size);

		[DllImport("__Internal")]
		private static extern bool WebGL_GetAmplitude(string uniqueName, float[] sample, int sampleSize);
#endif
		
		private string _clipId;
		private readonly int _sampleSize;
		private readonly float[] _sample;
		private readonly bool _streaming;
		public WebGlAudioBufferProcessor(int sampleSize, bool streaming)
		{
			_streaming = streaming;
			_sampleSize = sampleSize;
			_sample = new float[sampleSize];
			
			//Allocate heap memory buffer to fix buffer growth
			//BUG: https://github.com/emscripten-core/emscripten/issues/6747
			//TODO: Calculate approximate buffer size from letters used in text 

#if UNITY_WEBGL

			if (streaming)
			{
				var memAllocSize = int.MaxValue / 8; //~30mb
				WebGL_Initialize(BufferSize, memAllocSize);
			}
#else
			throw new NotSupportedException("This class can be only used on WebGL platform");
#endif
		}

		public static int GetSupportedSampleRate()
		{
#if UNITY_WEBGL
			return WebGL_GetAudioContextSampleRate();
#endif
			return -1;
		}
		public void StartSampling(AudioClip clip)
		{
#if UNITY_WEBGL
			_clipId = clip.GetInstanceID().ToString();
			var bufferIndex = WebGL_GetBufferInstanceOfLastAudioClip();
			WebGL_StartSampling(_clipId, bufferIndex, _sampleSize, _streaming);
#endif
		}
		public float[] GetSample()
		{
#if UNITY_WEBGL
			WebGL_GetAmplitude(_clipId, _sample, _sampleSize);
#endif
			return _sample;

		}
		
		public static void OnPcmBuffer(float[] buffer)
		{
#if UNITY_WEBGL
			WebGL_FillBuffer(buffer,  buffer.Length);
#endif
		}

		public void StopSampling()
		{
#if UNITY_WEBGL
			WebGL_StopSampling(_clipId);
#endif
		}
	}
}