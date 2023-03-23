using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;

namespace Charactr.VoiceSDK.Rest
{
	public class RestHttpClient: IDisposable
	{
		public const string API_CLIENT = "X-Client-Key";
		public const string API_KEY = "X-Api-Key";
		private readonly HttpClient _client;
		private Action<FrameworkErrorMessage> OnError { get;}
		private string _clientKey;
		private string _apiKey;
		public RestHttpClient(string apiClient, string apiKey, Action<FrameworkErrorMessage> onError)
		{
			_clientKey = apiClient;
			_apiKey = apiKey;
			
			OnError = onError;
			_client = new HttpClient();
			_client.DefaultRequestHeaders.Add(API_CLIENT, apiClient);
			_client.DefaultRequestHeaders.Add(API_KEY, apiKey);
			_client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("Charactr.Unity.SDK"));
		}
		
		public async Task<T> GetAsync<T>(string url) where T: IAPIResponse
		{
			var req = await _client.GetAsync(url);

			if (req.StatusCode != HttpStatusCode.OK)
			{
				ReturnErrorMessage(ToString(), (int)req.StatusCode);
				return default;
			}

			var content = await req.Content.ReadAsStringAsync();
			var json = JsonConvert.DeserializeObject<T>(content);
			return json;
		}

		public async Task<byte[]> PostAsync(string url, string jsonPayload)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Content = new StringContent(jsonPayload, Encoding.UTF8,"application/json");

			var req = await _client.SendAsync(request);
			
			if (req.StatusCode != HttpStatusCode.OK)
			{
				ReturnErrorMessage(ToString(), (int)req.StatusCode);
				return default;
			}
			
			var content = await req.Content.ReadAsByteArrayAsync();
			return content;
		}

		public IEnumerator GetAudioClipRoutine(string url, string jsonPayload, Action<AudioClip> onDownloaded, AudioType audioType = AudioType.WAV)
		{
			var handler = new DownloadHandlerAudioClip(url, audioType);

			var req = UnityWebRequest.Post(url, string.Empty);
			
			req.SetRequestHeader(API_CLIENT, _clientKey);
			req.SetRequestHeader(API_KEY, _apiKey);
			
			req.uploadHandler = new UploadHandlerRaw(Encoding.Default.GetBytes(jsonPayload));
			req.uploadHandler.contentType = "application/json";
			
			handler.streamAudio = handler.compressed = false;
			req.downloadHandler = handler;

			yield return req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError(req.error);
				ReturnErrorMessage(ToString(), (int) req.responseCode);
				yield break;
			}

			var audioClip = handler.audioClip;
			//Dispose native buffers
			req.Dispose();
			onDownloaded(audioClip);
		}

		private void ReturnErrorMessage(string source, int httpCode)
		{
			var error = new FrameworkErrorMessage()
			{
				Source = source,
				StatusCode = httpCode,
				Message = $"Can't process request, status code is: {httpCode}"
			};
			OnError.Invoke(error);
		}
		
		public async Task<HttpStatusCode> SendAsync(string url, string method, string jsonPayload)
		{
			var request = new HttpRequestMessage(new HttpMethod(method), url);
			request.Content = new StringContent(jsonPayload, Encoding.UTF8,"application/json" );
			var response = await _client.SendAsync(request);
			return response.StatusCode;
		}

		public void Dispose()
		{
			_client?.Dispose();
		}
	}
}