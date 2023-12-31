﻿using System;
using Gemelo.Voice.Rest.Model;
using UnityEngine;

namespace Gemelo.Voice.Rest.Client
{
	public class RuntimeRestClient : RestClient, IDisposable
	{
		public RuntimeRestClient(Configuration configuration) : base(configuration, Debug.LogError) { }
		
		public UnityAudioConvertRequest GetAudioClipRequest(string url, ConvertRequest convertRequest)
		{
			var request = new UnityAudioConvertRequest(url, convertRequest);
			request.AddHeader(Configuration.API_CLIENT, ClientKey);
			request.AddHeader(Configuration.API_KEY, APIKey);
			request.AddHeader("user-agent", Configuration.USER_AGENT);
			return request;
		}
		
		public void Dispose() { }
	}
}