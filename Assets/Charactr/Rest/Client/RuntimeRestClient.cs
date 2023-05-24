using System;
using Charactr.VoiceSDK.Model;
using Charactr.VoiceSDK.SDK;
using UnityEngine;

namespace Charactr.VoiceSDK.Rest
{
	public class RuntimeRestClient : RestClient, IDisposable
	{
		public RuntimeRestClient(Configuration configuration) : base(configuration, Debug.LogError) { }
		
		public UnityAudioConvertRequest GetAudioClipRequest(string url, ConvertRequest convertRequest)
		{
			var request = new UnityAudioConvertRequest(url, convertRequest);
			request.AddHeader(API_CLIENT, ClientKey);
			request.AddHeader(API_KEY, APIKey);
			return request;
		}
		
		public void Dispose()
		{
			
		}
	}
}