using System.Collections;
using System.Threading.Tasks;
using Gemelo.Voice.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gemelo.Voice.Tests
{
	public class Convert : TestBase
	{
		[Test]
		public async Task GetConversion_Returns_WavClip()
		{
			var wavBytes = await EditorHttp.PostAsync(Configuration.CONVERT_API, CreateRequest().ToJson());
			
			Assert.NotNull(wavBytes);
		}

		[Test]
		public async Task GetBytesAndConvertToWav_Returns_WavClip()
		{
			var wavBytes = await EditorHttp.PostAsync(Configuration.CONVERT_API, CreateRequest().ToJson());
			
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
		public IEnumerator PlayConversion_UnityAudioRequestClass_Returns_OK()
		{
			AudioClip audioClip = null;
			
			var request = RuntimeHttp.GetAudioClipRequest(Configuration.CONVERT_API, CreateRequest());
			yield return request.SendRequest();
			
			audioClip = request.AudioClip;
			
			Debug.Log($"Clip: {audioClip.frequency} {audioClip.length} {audioClip.samples}");
			
			Assert.NotNull(audioClip);
			Assert.AreEqual(124160, audioClip.samples);
			Assert.AreEqual(32000, audioClip.frequency);
			Assert.AreEqual(3.88f, audioClip.length);
			
			yield return AudioPlayer.PlayClipRoutineStatic(audioClip);
		}
	}
}