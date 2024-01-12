using Gemelo.Voice.Rest.Client;
using Gemelo.Voice.Rest.Model;
using NUnit.Framework;
using UnityEngine;

namespace Gemelo.Voice.Tests
{
	public class TestBase
	{
	#if DEVELOPMENT
		public const int VOICE_ID = 142; //Olivia
	#else
		public const int VOICE_ID = 151; //Liam
	#endif
		
		protected EditorRestClient EditorHttp;
		protected RuntimeRestClient RuntimeHttp;
		protected Configuration Configuration;
        
		[SetUp]
		public void Setup()
		{
			Configuration = Configuration.Load();
			Assert.NotNull(Configuration);
			
			EditorHttp = new EditorRestClient(Configuration, Debug.LogWarning);
			RuntimeHttp = new RuntimeRestClient(Configuration);
		}

		protected ConvertRequest CreateDefaultRequest() => new ConvertRequest()
		{
			Text = "This is sample text to convert using Charactr API",
			VoiceId = VOICE_ID
		};

		protected ConvertRequest CreateRequest(VoiceType voiceType, int voiceId) => new ConvertRequest()
		{
			Text = "This is sample text to convert using Charactr API",
			VoiceId = voiceId,
			VoiceType = voiceType.ToString().ToLower()
		};

	}
}