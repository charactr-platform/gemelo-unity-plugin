using Charactr.Editor.Library;
using Charactr.VoiceSDK.Model;
using Charactr.VoiceSDK.Rest;
using Charactr.VoiceSDK.SDK;
using NUnit.Framework;
using UnityEngine;

namespace Charactr.VoiceSDK.Tests
{
	public class TestBase
	{
		public const int VOICE_ID = 151; //Liam voice
		
		public const string CLIENT_KEY = "87Pgd9jP4BoFeblC8xstxSHGVdnhZl1B";
		public const string API_KEY = "qynCJRdjOc3hNvAAmDZUMFgTqi1hOXTC";
		
		protected RestHttpClient Http;
		protected Configuration Configuration;
        
		[SetUp]
		public void Setup()
		{
			Configuration.Save(CLIENT_KEY, API_KEY);
			Configuration = Configuration.Load();
			Assert.NotNull(Configuration);
			
			Http = new RestHttpClient(Configuration.ApiClient, Configuration.ApiKey, (error) => Debug.LogError(error.Message));
		}

		protected ConvertRequest CreateRequest() => new ConvertRequest()
		{
			Text = "This is sample text to convert using Charactr API",
			VoiceId = VOICE_ID
		};
	}
}