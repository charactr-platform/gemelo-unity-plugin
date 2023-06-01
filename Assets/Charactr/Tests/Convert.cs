using System.Collections;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Charactr.VoiceSDK.Tests
{
	public class Convert : TestBase
	{
		private const string ENDPOINT = "convert";

		[Test]
		public async Task GetConversion_Returns_WAV()
		{
			var wavBytes = await EditorHttp.PostAsync(Configuration.API + ENDPOINT, CreateRequest().ToJson());
			
			Assert.NotNull(wavBytes);
			Assert.IsNotEmpty(wavBytes);
		}

		[Test]
		public async Task GetBytesAndConvertToWAV_Returns_WAV()
		{
			var wavBytes = await EditorHttp.PostAsync(Configuration.API + ENDPOINT, CreateRequest().ToJson());
			
			Assert.NotNull(wavBytes);
			Assert.IsNotEmpty(wavBytes);
			
			var wav = new WavBuilder(wavBytes);
			Assert.NotNull(wav);
			
			var header = new WavHeaderData(wavBytes);
			Assert.NotNull(header);
			Assert.IsTrue(header.IsExtensibeWav);
			Assert.NotZero(header.AudioFormat);
			Assert.NotZero(header.DataOffset);
			Assert.AreEqual(16,header.BitDepth);

			var clip = wav.CreateAudioClip();
			Assert.NotNull(clip);
			Assert.NotZero(clip.length);
			Assert.AreEqual(130560, clip.samples);
			Assert.AreEqual(32000, clip.frequency);
			Assert.AreEqual(4.08f, clip.length);
			
			AudioPlayer.PlayClip(clip);
			await Task.Delay((int)clip.length * 1000);
		}

		[UnityTest]
		public IEnumerator PlayConversion_UnityAudioRequestClass_Returns_OK()
		{
			AudioClip audioClip = null;
			
			var request = RuntimeHttp.GetAudioClipRequest(Configuration.API + ENDPOINT, CreateRequest());
			yield return request.SendRequest();
			
			audioClip = request.AudioClip;
			
			Debug.Log($"Clip: {audioClip.frequency} {audioClip.length} {audioClip.samples}");
			
			Assert.NotNull(audioClip);
			Assert.AreEqual(130560, audioClip.samples);
			Assert.AreEqual(32000, audioClip.frequency);
			Assert.AreEqual(4.08f, audioClip.length);
			
			yield return AudioPlayer.PlayClipRoutine(audioClip);
		}
	}
}