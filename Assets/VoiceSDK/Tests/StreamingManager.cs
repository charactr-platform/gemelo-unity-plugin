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
		yield return ConvertAndPlayStreamingManager(Text, AudioDataType.Wav);
	}

	[UnityTest]
	[RequiresPlayMode()]
	public IEnumerator Stream_ShortText_Returns_Mp3()
	{
		yield return ConvertAndPlayStreamingManager(Text, AudioDataType.Mp3);
	}

	[UnityTest]
	[RequiresPlayMode()]
	public IEnumerator Stream_LongText_Returns_Wav()
	{
		yield return ConvertAndPlayStreamingManager(LongText, AudioDataType.Wav);
	}

	[UnityTest]
	[RequiresPlayMode()]
	public IEnumerator Stream_LongText_Returns_Mp3()
	{
		yield return ConvertAndPlayStreamingManager(LongText, AudioDataType.Mp3);
	}

	private IEnumerator ConvertAndPlayStreamingManager(string text, AudioDataType dataType)
	{
		var gameobject = new GameObject("_audioStreamingManager");
		var manager = gameobject.AddComponent<AudioStreamingManager>();

		Assert.IsNotNull(manager);
		Assert.IsTrue(manager.TryGetComponent<AudioPlayer>(out _));
		manager.SetAudioDataType(dataType);
		yield return manager.Convert(text);
		Assert.IsNotNull(manager.AudioClip);
		Assert.IsFalse(manager.AudioEnd);

		manager.InitializePlayer(new AverageProvider(), 512);

		yield return manager.Play();
		Assert.IsTrue(manager.AudioEnd);
	}
	}
}