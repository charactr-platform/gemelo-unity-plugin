using System;
using System.Collections;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Rest.Model;
using Gemelo.Voice.Streaming;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gemelo.Voice.Tests
{
	public class StreamingManager : StreamingTestsData
	{

	#if DEVELOPMENT
		private const int SystemVoiceId = 204;
		private const int ClonedVoiceId = 357;
	#else
		private const int SystemVoiceId = 151;
		private const int ClonedVoiceId = 357;
	#endif
		
		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Stream_ShortText_Returns_Wav()
		{
			yield return ConvertAndPlayStreamingManager(Text, SystemVoiceId, VoiceType.System, AudioDataType.WAV);
		}

		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Stream_ShortText_Returns_Mp3()
		{
			yield return ConvertAndPlayStreamingManager(Text, SystemVoiceId, VoiceType.System, AudioDataType.MP3);
		}
		
		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Stream_ShortText_ClonedVoice_Returns_Wav()
		{
			yield return ConvertAndPlayStreamingManager(Text, ClonedVoiceId, VoiceType.Cloned, AudioDataType.WAV);
		}

		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Stream_ShortText_ClonedVoice_Returns_Mp3()
		{
			yield return ConvertAndPlayStreamingManager(Text, ClonedVoiceId, VoiceType.Cloned, AudioDataType.MP3);
		}
		
		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Stream_LongText_Returns_Wav()
		{
			yield return ConvertAndPlayStreamingManager(LongText,SystemVoiceId, VoiceType.System, AudioDataType.WAV);
		}

		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Stream_LongText_Returns_Mp3()
		{
			yield return ConvertAndPlayStreamingManager(LongText,SystemVoiceId, VoiceType.System, AudioDataType.MP3);
		}
		
		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Stream_LongText_ClonedVoice_Returns_Wav()
		{
			yield return ConvertAndPlayStreamingManager(LongText, ClonedVoiceId, VoiceType.Cloned, AudioDataType.WAV);
		}

		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Stream_LongText_ClonedVoice_Returns_Mp3()
		{
			yield return ConvertAndPlayStreamingManager(LongText, ClonedVoiceId, VoiceType.Cloned, AudioDataType.MP3);
		}
		
		private IEnumerator ConvertAndPlayStreamingManager(string text, int voiceId, VoiceType voiceType, AudioDataType dataType)
		{
			var manager = InitializeManager(voiceType, dataType);

			manager.SetVoiceId(voiceId);
			
			yield return manager.Convert(text);
			Assert.IsNotNull(manager.AudioClip);
			Assert.IsFalse(manager.AudioEnd);

			manager.InitializePlayer(new AverageProvider(), 512);

			yield return manager.Play();
			Assert.IsTrue(manager.AudioEnd);
		}

		private AudioStreamingManager InitializeManager(VoiceType voiceType, AudioDataType dataType)
		{
			var gameObject = new GameObject("_audioStreamingManager");
			var manager = gameObject.AddComponent<AudioStreamingManager>();

			Assert.IsNotNull(manager);
			Assert.IsTrue(manager.TryGetComponent<AudioPlayer>(out _));

			manager.SetAudioDataType(dataType);
			manager.SetVoiceType(voiceType);
			
			Assert.NotNull(manager.AudioParameters);
			Assert.IsInstanceOf<AudioParameters>(manager.AudioParameters);
			Assert.AreEqual(dataType, manager.AudioParameters.AudioDataType);
			
			return manager;
		}
	}
}