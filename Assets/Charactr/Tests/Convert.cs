using System;
using System.Collections;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Windows;

namespace Charactr.VoiceSDK.Tests
{
	public class Convert : TestBase
	{
		private const string ENDPOINT = "convert";
		
		private ConvertRequest CreateRequest() => new ConvertRequest()
		{
			Text = "This is sample text to convert using Charactr API",
			VoiceId = 151 //Liam voice
		};
		
		[Test]
		public async Task GetConversion_Returns_WAV()
		{
			var wav = await Http.PostAsync(API + ENDPOINT, CreateRequest().ToJson());
			Assert.NotNull(wav);
			Assert.IsNotEmpty(wav);
			File.WriteAllBytes(Application.streamingAssetsPath + "/sample.wav", wav);
		}

		[UnityTest]
		public IEnumerator PlayConversion_Returns_OK()
		{
			AudioClip audioClip = null;
			var audioPlayer = new GameObject("_player").AddComponent<AudioSource>();
			audioPlayer.gameObject.AddComponent<AudioListener>();
			
			yield return Http.GetAudio(API + ENDPOINT, CreateRequest().ToJson(), clip =>
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