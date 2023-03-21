using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Charactr.Api.Rest
{
	public class EditorHttpClient
	{
		private readonly HttpClient _client;
		private Action<FrameworkErrorMessage> OnError { get;}
		public EditorHttpClient(string apiClient,string apiKey, Action<FrameworkErrorMessage> onError)
		{
			OnError = onError;
			_client = new HttpClient();
			_client.DefaultRequestHeaders.Add("X-Client-Key", apiClient);
			_client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
			_client.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("Charactr.Unity.SDK"));
		}
		
		public async Task<T> GetAsync<T>(string url) where T: IAPIResponse
		{
			var req = await _client.GetAsync(url);

			if (req.StatusCode != HttpStatusCode.OK)
			{
				var error = new FrameworkErrorMessage()
				{
					Source = ToString(),
					StatusCode = (int) req.StatusCode,
					Message = $"Can't process request, status code is: {req.StatusCode}"
				};
				
				OnError.Invoke(error);
				return default;
			}

			var content = await req.Content.ReadAsStringAsync();
			var json = JsonConvert.DeserializeObject<T>(content);
			return json;
		}
		
		public async Task<HttpStatusCode> SendAsync(string url, string method, string jsonPayload)
		{
			var request = new HttpRequestMessage(new HttpMethod(method), url);
			request.Content = new StringContent(jsonPayload, Encoding.UTF8,"application/json" );
			var response = await _client.SendAsync(request);
			return response.StatusCode;
		}
	}
}