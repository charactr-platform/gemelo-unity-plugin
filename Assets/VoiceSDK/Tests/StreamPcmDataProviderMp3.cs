using System.IO;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Streaming;
using NUnit.Framework;
using UnityEngine;

namespace Gemelo.Voice.Tests
{
	public class StreamPcmDataProviderMp3
	{
		private const string FileName = "gs-16b-1c-44100hz.mp3";
		private IPcmDataProvider _dataProvider;
		private MemoryStream _data;
		[SetUp]
		public void Setup()
		{
			var path = Path.Combine(Application.dataPath, "VoiceSDK/Tests", FileName);
			Assert.IsTrue(File.Exists(path));
			
			using (var stream = new FileStream(path, FileMode.Open))
			{
				Assert.NotNull(stream);
				Assert.IsTrue(stream.CanRead);
				var buffer = new byte[stream.Length];
				Assert.NotZero(stream.Read(buffer, 0, (int)stream.Length));
				_data = new MemoryStream(buffer);
			}	
			
			Assert.NotZero(_data.Length);
			_dataProvider = new StreamPcmDataProvider();
			Assert.NotNull(_dataProvider);
		}

		[Test]
		public void AudioBuilder_NotNull()
		{
			var builder = CreateAudioBuilderFromHeader();
			Assert.NotNull(builder);
			Assert.NotNull(_dataProvider.AudioClipBuilder);
			Assert.IsInstanceOf<Mp3Builder>(_dataProvider.AudioClipBuilder);
		}
		
		[Test]
		public void MpegFile_SampleRate_Equals_44100()
		{
			CreateAudioBuilderFromHeader();
		
			var mp3Builder = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3Builder);
			Assert.NotNull(mp3Builder.MpegFile);
			Assert.AreEqual(1, mp3Builder.MpegFile.Channels);
			Assert.AreEqual(44100, mp3Builder.MpegFile.SampleRate);
		}

		[Test]
		public void Load_Builder_PcmFrames_From_Buffer_NotZero()
		{
			CreateAudioBuilderFromHeader();
			
			var bytesCount = 1024;
			var buffer = ReadNextByteSample(bytesCount);
			var frames = _dataProvider.AudioClipBuilder.ToPcmFrames(buffer);
			
			Assert.NotNull(frames);
			Assert.NotZero(frames.Count);
			
			var mp3 = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3);
			Assert.IsTrue(mp3.MpegFile.Reader.EndOfStream);
			Assert.IsFalse(mp3.MpegFile.Reader.HasNextFrame);
		}
		[Test]
		public void Load_Builder_PcmFrames_From_Buffer_NoFrames()
		{
			CreateAudioBuilderFromHeader();
			
			var bytesCount = 128;
			var buffer = ReadNextByteSample(bytesCount);
			var frames = _dataProvider.AudioClipBuilder.ToPcmFrames(buffer);
			
			Assert.NotNull(frames);
			Assert.Zero(frames.Count);
			
			var mp3 = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3);
			Assert.IsTrue(mp3.MpegFile.Reader.EndOfStream);
			Assert.IsFalse(mp3.MpegFile.Reader.HasNextFrame);
		}
		
		[Test]
		public void Load_PCM_Samples_NotZero()
		{
			var clipBuilder = CreateAudioBuilderFromHeader();
			Assert.NotNull(clipBuilder);
			Assert.NotNull(_dataProvider.AudioClipBuilder);

			var bytesCount = 1024;
			
			while (_data.Position + bytesCount < _data.Length)
			{
				_dataProvider.AddRawData(ReadNextByteSample(bytesCount));
			}

			var lastBit = _data.Length - _data.Position;
			
			Assert.NotZero(lastBit);

			var lastBuffer = ReadNextByteSample((int) lastBit);
			_dataProvider.AddRawData(lastBuffer);

			Assert.NotZero(_dataProvider.BufferPcmFrames());
				
			Assert.IsTrue(_dataProvider.BufferLastFrame());
		}

		private byte[] ReadNextByteSample(int bytesCount = 1024)
		{
			var buffer = new byte[bytesCount];
			var readSize = _data.Read(buffer, 0, bytesCount);
			Assert.AreEqual(bytesCount, readSize);
			return buffer;
		}
		
		private AudioClipBuilder CreateAudioBuilderFromHeader()
		{
			var size = 1024;
			var buffer = ReadNextByteSample(size);
			Assert.AreEqual(size, buffer.Length);
			
			_dataProvider.AddRawData(buffer);
			return _dataProvider.CreateAudioBuilder(AudioDataType.Mp3, 44100);
		}
	}
}