using System;

namespace Charactr.VoiceSDK.Rest
{
	public class RestClient
	{
		public const string API_CLIENT = "X-Client-Key";
		public const string API_KEY = "X-Api-Key";
		
		protected readonly string ClientKey;
		protected readonly string APIKey;
		private Action<FrameworkErrorMessage> OnError { get;}

		protected RestClient(Configuration configuration, Action<FrameworkErrorMessage> onError)
		{
			ClientKey = configuration.ApiClient;
			APIKey = configuration.ApiKey;
			OnError = onError;
		}
		
		protected void ReturnErrorMessage(string source, int httpCode)
		{
			var error = new FrameworkErrorMessage()
			{
				Source = source,
				StatusCode = httpCode,
				Message = $"Can't process request, status code is: {httpCode}"
			};
			
			OnError.Invoke(error);
		}
	}
}