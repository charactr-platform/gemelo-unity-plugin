using Gemelo.VoiceSDK.Rest.Client;
using Gemelo.VoiceSDK.Rest.Model;
using NUnit.Framework;
using UnityEngine;

namespace Gemelo.VoiceSDK.Tests
{
	public class TestBase
	{
		public const int VOICE_ID = 151; //Liam voice
		
		protected EditorRestClient EditorHttp;
		protected RuntimeRestClient RuntimeHttp;
		protected Configuration Configuration;
        
		[SetUp]
		public void Setup()
		{
			Configuration = Configuration.Load();
			Assert.NotNull(Configuration);
			
			EditorHttp = new EditorRestClient(Configuration, Debug.LogError);
			RuntimeHttp = new RuntimeRestClient(Configuration);
		}

		protected ConvertRequest CreateRequest() => new ConvertRequest()
		{
			Text = "This is sample text to convert using Charactr API",
			VoiceId = VOICE_ID
		};
	}
}