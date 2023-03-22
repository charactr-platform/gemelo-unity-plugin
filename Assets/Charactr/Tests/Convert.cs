using System;
using System.Collections;
using System.Threading.Tasks;
using Charactr.VoiceSDK.SDK;
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
			var wavBytes = await Http.PostAsync(Configuration.API + ENDPOINT, CreateRequest().ToJson());
			
			Assert.NotNull(wavBytes);
			Assert.IsNotEmpty(wavBytes);
		}

		[Test]
		public async Task PlayConversion_ConvertToWav_Returns_OK()
		{
			var wavBytes = await Http.PostAsync(Configuration.API + ENDPOINT, CreateRequest().ToJson());
			
			Assert.NotNull(wavBytes);
			Assert.IsNotEmpty(wavBytes);
			
			var audioClip = WavUtility.ToAudioClip(wavBytes);
			
			Assert.AreEqual(130560, audioClip.samples);
			Assert.AreEqual(32000, audioClip.frequency);
			Assert.AreEqual(4.08f, audioClip.length);
			
			var audioPlayer = CreatePlayerObject();
			audioPlayer.PlayOneShot(audioClip);
			
			await Task.Delay((int)audioClip.length * 1000);
		}

		[UnityTest]
		public IEnumerator PlayConversion_Coroutine_Returns_OK()
		{
			AudioClip audioClip = null;

			var audioPlayer = CreatePlayerObject();
			
			yield return Http.GetAudioClipRoutine(Configuration.API + ENDPOINT, CreateRequest().ToJson(), clip =>
			{
				audioClip = clip;
				audioPlayer.PlayOneShot(clip);
				Debug.Log($"Clip: {clip.frequency} {clip.length} {clip.samples}");
			});
			
			//Give it time to play till end 
			while (audioPlayer.isPlaying)
			{
				yield return null;
			}
			
			Assert.NotNull(audioClip);
			Assert.AreEqual(130560, audioClip.samples);
			Assert.AreEqual(32000, audioClip.frequency);
			Assert.AreEqual(4.08f, audioClip.length);
		}
	}
}