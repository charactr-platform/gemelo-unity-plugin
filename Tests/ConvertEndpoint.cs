using System.Collections;
using System.Threading.Tasks;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Rest.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gemelo.Voice.Tests
{
	public class ConvertEndpoint : TestBase
	{
		//Warning! Voice id needs to be present in "cloned voices" database
		private const int ClonedVoiceId = 357;
		
		[Test]
		public async Task GetConversion_Returns_WavClip()
		{
			var wavBytes = await EditorHttp.PostAsync(Configuration.CONVERT_API, CreateDefaultRequest().ToJson());
			
			Assert.NotNull(wavBytes);
		}

		[Test]
		public async Task GetConversion_Cloned_Returns_WavClip()
		{
			var request = CreateRequest(VoiceType.Cloned, ClonedVoiceId).ToJson();
			var wavBytes = await EditorHttp.PostAsync(Configuration.CONVERT_API,request);
			
			Assert.NotNull(wavBytes);
		}
		
		[Test]
		public async Task GetBytesAndConvertToWav_Returns_WavClip()
		{
			var wavBytes = await EditorHttp.PostAsync(Configuration.CONVERT_API, CreateDefaultRequest().ToJson());
			
			Assert.NotNull(wavBytes);
			
			var header = new WavHeaderData(wavBytes);
			Assert.NotNull(header);
			Assert.NotZero(header.AudioFormat);
			Assert.NotZero(header.DataOffset);
			Assert.AreEqual(32000, header.SampleRate);
			Assert.AreEqual(16,header.BitDepth);

			var wav = new WavBuilder(header.SampleRate, header.BitDepth, wavBytes);
			Assert.NotNull(wav);
			
			var clip = wav.CreateAudioClip();
			Assert.NotNull(clip);
			Assert.NotZero(clip.length);
			Assert.AreEqual(124160, clip.samples);
			Assert.AreEqual(32000, clip.frequency);
			Assert.AreEqual(3.88f, clip.length);
			
			await AudioPlayer.PlayClipStatic(clip);
		}

		[UnityTest]
		public IEnumerator Play_System_Conversion_UnityAudioRequestClass_Returns_OK()
		{
			AudioClip audioClip = null;
			
			var request = RuntimeHttp.GetAudioClipRequest(Configuration.CONVERT_API, CreateDefaultRequest());
			yield return request.SendRequest();
			
			audioClip = request.AudioClip;
			
			Debug.Log($"Clip: {audioClip.frequency} {audioClip.length} {audioClip.samples}");
			
			Assert.NotNull(audioClip);
		#if !DEVELOPMENT
			Assert.AreEqual(124160, audioClip.samples);
			Assert.AreEqual(32000, audioClip.frequency);
			Assert.AreEqual(3.88f, audioClip.length);
		#endif
			
			yield return AudioPlayer.PlayClipRoutineStatic(audioClip);
		}
		
		[UnityTest]
		public IEnumerator Play_Cloned_Conversion_UnityAudioRequestClass_Returns_OK()
		{
			AudioClip audioClip = null;

			var convert = CreateRequest(VoiceType.Cloned, ClonedVoiceId);
			var request = RuntimeHttp.GetAudioClipRequest(Configuration.CONVERT_API, convert);
			yield return request.SendRequest();
			
			audioClip = request.AudioClip;
			
			Debug.Log($"Clip: {audioClip.frequency} {audioClip.length} {audioClip.samples}");
			
			Assert.NotNull(audioClip);
		#if !DEVELOPMENT
			Assert.AreEqual(164480, audioClip.samples);
			Assert.AreEqual(32000, audioClip.frequency);
			Assert.AreEqual(3.88f, audioClip.length);
		#endif	
			
			yield return AudioPlayer.PlayClipRoutineStatic(audioClip);
		}
	}
}