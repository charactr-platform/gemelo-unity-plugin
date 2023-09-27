using System;
using System.Collections;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Streaming;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gemelo.Voice.Tests
{
	public class StreamingManager: StreamingTestsData
	{
	[UnityTest]
	[RequiresPlayMode()]
	public IEnumerator Stream_ShortText_Returns_Wav()
	{
		yield return ConvertAndPlayStreamingManager(Text, AudioDataType.WAV);
	}

	[UnityTest]
	[RequiresPlayMode()]
	public IEnumerator Stream_ShortText_Returns_Mp3()
	{
		yield return ConvertAndPlayStreamingManager(Text, AudioDataType.MP3);
	}

	[UnityTest]
	[RequiresPlayMode()]
	public IEnumerator Stream_LongText_Returns_Wav()
	{
		yield return ConvertAndPlayStreamingManager(LongText, AudioDataType.WAV);
	}

	[UnityTest]
	[RequiresPlayMode()]
	public IEnumerator Stream_LongText_Returns_Mp3()
	{
		yield return ConvertAndPlayStreamingManager(LongText, AudioDataType.MP3);
	}

	private IEnumerator ConvertAndPlayStreamingManager(string text, AudioDataType dataType)
	{
		var gameObject = new GameObject("_audioStreamingManager");
		var manager = gameObject.AddComponent<AudioStreamingManager>();

		Assert.IsNotNull(manager);
		Assert.IsTrue(manager.TryGetComponent<AudioPlayer>(out _));
		
		manager.SetAudioDataType(dataType);
		Assert.NotNull(manager.AudioParameters);
		Assert.IsInstanceOf<AudioParameters>(manager.AudioParameters);
		Assert.AreEqual(dataType, manager.AudioParameters.AudioDataType);
		
		yield return manager.Convert(text);
		Assert.IsNotNull(manager.AudioClip);
		Assert.IsFalse(manager.AudioEnd);

		manager.InitializePlayer(new AverageProvider(), 512);

		yield return manager.Play();
		Assert.IsTrue(manager.AudioEnd);
	}
	}
}