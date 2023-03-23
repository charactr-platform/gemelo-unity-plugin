using Charactr.VoiceSDK.Model;
using Charactr.VoiceSDK.Rest;
using Charactr.VoiceSDK.SDK;
using NUnit.Framework;
using UnityEngine;

namespace Charactr.VoiceSDK.Tests
{
	public class TestBase
	{
		private const string CLIENT_KEY = "87Pgd9jP4BoFeblC8xstxSHGVdnhZl1B";
		private const string API_KEY = "qynCJRdjOc3hNvAAmDZUMFgTqi1hOXTC";
		
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
			VoiceId = 151 //Liam voice
		};

		protected AudioSource CreatePlayerObject()
		{
			var audioPlayer = new GameObject("_player").AddComponent<AudioSource>();
			var audioListener = audioPlayer.gameObject.AddComponent<AudioListener>();
			
			Assert.NotNull(audioPlayer);
			Assert.NotNull(audioListener);
			
			return audioPlayer;
		}
	}
}