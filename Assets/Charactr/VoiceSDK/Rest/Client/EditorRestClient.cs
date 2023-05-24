using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Model;
using Newtonsoft.Json;

namespace Charactr.VoiceSDK.Rest
{
	public class EditorRestClient: RestClient, IDisposable
	{
		private readonly HttpClient _client;
		
		public EditorRestClient(Configuration configuration, Action<FrameworkErrorMessage> onError): base(configuration, onError)
		{
			_client = new HttpClient();
			_client.DefaultRequestHeaders.Add(Configuration.API_CLIENT, ClientKey);
			_client.DefaultRequestHeaders.Add(Configuration.API_KEY, APIKey);
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
		
		public void Dispose()
		{
			_client?.Dispose();
		}
	}
}