using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Gemelo.Voice.Rest.Model;
using Newtonsoft.Json;

namespace Gemelo.Voice.Rest.Client
{
	public class EditorRestClient: RestClient, IDisposable
	{
		private readonly HttpClient _client;
		
		public EditorRestClient(Configuration configuration, Action<FrameworkErrorMessage> onError): base(configuration, onError)
		{
			_client = new HttpClient();
			_client.DefaultRequestHeaders.Add(Configuration.API_CLIENT, ClientKey);
			_client.DefaultRequestHeaders.Add(Configuration.API_KEY, APIKey);
			_client.DefaultRequestHeaders.UserAgent.ParseAdd(Configuration.USER_AGENT);
		}
		
		public async Task<T> GetAsync<T>(string url) where T: IVoicesResponse
		{
			var req = await _client.GetAsync(url);

			if (req.StatusCode != HttpStatusCode.OK)
			{
				ReturnErrorMessage(ToString(), (int)req.StatusCode);
				throw new Exception($"Can't read data from HTTP resource [{req.StatusCode}]");
			}

			var content = await req.Content.ReadAsStringAsync();
			var json = JsonConvert.DeserializeObject<T>(content);
			return json;
		}

		public async Task<byte[]> GetDataAsync(string url)
		{
			var req = await _client.GetAsync(url);

			if (req.StatusCode != HttpStatusCode.OK)
			{
				ReturnErrorMessage(ToString(), (int)req.StatusCode);
				throw new Exception($"Can't read data from HTTP resource [{req.StatusCode}]");
			}

			return await req.Content.ReadAsByteArrayAsync();
		}
		
		public async Task<byte[]> PostAsync(string url, string jsonPayload)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Content = new StringContent(jsonPayload, Encoding.UTF8,"application/json");

			var req = await _client.SendAsync(request);
			
			if (req.StatusCode != HttpStatusCode.OK)
			{
				ReturnErrorMessage(ToString(), (int)req.StatusCode);
				throw new Exception($"Can't write data to HTTP resource [{req.StatusCode}]");
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