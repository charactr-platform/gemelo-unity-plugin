using System;
using Charactr.VoiceSDK.Rest.Model;
using UnityEngine;

namespace Charactr.VoiceSDK.Rest.Client
{
	public class RuntimeRestClient : RestClient, IDisposable
	{
		public RuntimeRestClient(Configuration configuration) : base(configuration, Debug.LogError) { }
		
		public UnityAudioConvertRequest GetAudioClipRequest(string url, ConvertRequest convertRequest)
		{
			var request = new UnityAudioConvertRequest(url, convertRequest);
			request.AddHeader(Configuration.API_CLIENT, ClientKey);
			request.AddHeader(Configuration.API_KEY, APIKey);
			return request;
		}
		
		public void Dispose()
		{
			
		}
	}
}