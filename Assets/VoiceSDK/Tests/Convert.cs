using System.Collections;
using System.Threading.Tasks;
using Gemelo.VoiceSDK.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gemelo.VoiceSDK.Tests
{
	public class Convert : TestBase
	{
		[Test]
		public async Task GetConversion_Returns_WAV()
		{
			var wavBytes = await EditorHttp.PostAsync(Configuration.CONVERT_API, CreateRequest().ToJson());
			
			Assert.NotNull(wavBytes);
		}

		[Test]
		public async Task GetBytesAndConvertToWAV_Returns_WAV()
		{
			var wavBytes = await EditorHttp.PostAsync(Configuration.CONVERT_API, CreateRequest().ToJson());
			
			Assert.NotNull(wavBytes);

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
			Assert.AreEqual(130562, clip.samples);
			Assert.AreEqual(32000, clip.frequency);
			Assert.AreEqual(4.08f, clip.length);
			
			await AudioPlayer.PlayClipStatic(clip);
		}

		[UnityTest]
		public IEnumerator PlayConversion_UnityAudioRequestClass_Returns_OK()
		{
			AudioClip audioClip = null;
			
			var request = RuntimeHttp.GetAudioClipRequest(Configuration.CONVERT_API, CreateRequest());
			yield return request.SendRequest();
			
			audioClip = request.AudioClip;
			
			Debug.Log($"Clip: {audioClip.frequency} {audioClip.length} {audioClip.samples}");
			
			Assert.NotNull(audioClip);
			Assert.AreEqual(130560, audioClip.samples);
			Assert.AreEqual(32000, audioClip.frequency);
			Assert.AreEqual(4.08f, audioClip.length);
			
			yield return AudioPlayer.PlayClipRoutineStatic(audioClip);
		}
	}
}