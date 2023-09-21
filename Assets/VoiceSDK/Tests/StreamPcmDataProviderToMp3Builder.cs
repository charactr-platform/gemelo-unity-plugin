using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Streaming;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FileMode = System.IO.FileMode;

namespace Gemelo.Voice.Tests
{
	public class StreamPcmDataProviderToMp3Builder
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
			Assert.AreEqual(127329, _data.Length);
			
			_dataProvider = new StreamPcmDataProvider();
			Assert.NotNull(_dataProvider);
		}

		[Test]
		public void AudioBuilder_NotNull()
		{
			var builder = CreateAudioBuilderFromHeader(Mp3Builder.HeaderSize);
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
		public void MpegFile_FirstFrame_Bitrate_Equals_64kbps()
		{
			CreateAudioBuilderFromHeader();
		
			var mp3Builder = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3Builder);
			var mpegFile = mp3Builder.MpegFile;
			Assert.NotNull(mpegFile);
			Assert.AreEqual(64000, mpegFile.Reader.BitRate);
		}

		[Test]
		public void MpegFile_FirstFrame_SamplesPerFrame_Equals_1152()
		{
			CreateAudioBuilderFromHeader();
		
			var mp3Builder = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3Builder);
			var mpegFile = mp3Builder.MpegFile;
			Assert.NotNull(mpegFile);
			Assert.AreEqual(Mp3Builder.SamplesPerDecoderFrame, mpegFile.Reader.SamplesPerFrame);
		}

		[Test]
		public void Create_AudioBuilder_Throws_MoreDataNeeded()
		{
			Assert.Throws<Exception>(() => CreateAudioBuilderFromHeader(Mp3Builder.HeaderSize - 1));
		}
		
		[Test]
		public void Create_AudioBuilder_Decoder_Duration_Returns_State_EndOfStream()
		{
			CreateAudioBuilderFromHeader(Mp3Builder.HeaderSize);
			var mp3Builder = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3Builder);
			Assert.AreEqual(DecodingState.EndOfStream, mp3Builder.DecodeBytesToPcmSamples(out _));
		}
		
		[Test]
		public void Create_AudioBuilder_Decoder_Duration_Returns_State_Ok()
		{
			CreateAudioBuilderFromHeader(1300);
			var mp3Builder = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3Builder);
			Assert.AreEqual(DecodingState.Success, mp3Builder.DecodeBytesToPcmSamples(out var pcm));
			Assert.AreEqual(Mp3Builder.SamplesPerDecoderFrame, pcm.Length);
		}
		
		[Test]
		public void Load_Builder_PcmFrames_From_Buffer_NotZero()
		{
			CreateAudioBuilderFromHeader();
			
			var buffer = ReadNextByteSample(1024);
			var frames = _dataProvider.AudioClipBuilder.ToPcmFrames(buffer);
			
			Assert.NotNull(frames);
			Assert.NotZero(frames.Count);
			
			var mp3 = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3);
			Assert.IsFalse(mp3.MpegFile.Reader.EndOfStream);
			Assert.IsTrue(mp3.MpegFile.Reader.HasNextFrame);
		}

		[Test]
		public void Load_Builder_PcmFrames_From_Buffer_EndOfData_NotZero()
		{
			CreateAudioBuilderFromHeader(Mp3Builder.HeaderSize);
			
			var buffer = ReadNextByteSample(1050);
			var frames = _dataProvider.AudioClipBuilder.ToPcmFrames(buffer);
			
			Assert.NotNull(frames);
			Assert.NotZero(frames.Count);
			
			var mp3 = _dataProvider.AudioClipBuilder as Mp3Builder;
			Assert.NotNull(mp3);
			Assert.IsFalse(mp3.MpegFile.Reader.EndOfStream);
			Assert.IsTrue(mp3.MpegFile.Reader.HasNextFrame);
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
			
			Assert.AreEqual(127329, _data.Position);
			
			Assert.NotZero(_dataProvider.CreatePcmFramesFromData());
				
			Assert.IsTrue(_dataProvider.BufferLastFrame());
	
		}
		
		[UnityTest]
		[RequiresPlayMode()]
		public IEnumerator Load_Chunked_Data_Play_PCM_Samples_NotZero()
		{
			var clipBuilder = CreateAudioBuilderFromHeader();
			Assert.NotNull(clipBuilder);
			Assert.NotNull(_dataProvider.AudioClipBuilder);

			_dataProvider.CreatePcmFramesFromData();
			
			var bytesCount = 1024;
			var processedFrames = 0;
			
			while (_data.Position + bytesCount < (_data.Length - 1 ) / 4)
			{
				var bytes = ReadNextByteSample(bytesCount);
				Assert.GreaterOrEqual(bytes.Length, bytesCount);
				
				_dataProvider.AddRawData(bytes);
				
				if (_dataProvider.HasData())
					processedFrames += _dataProvider.CreatePcmFramesFromData();
			}
			
			Assert.NotZero(processedFrames);
			
			Assert.AreEqual(31744, _data.Position);

			yield return new WaitForSeconds(5);
			processedFrames = 0;
			
			while (_data.Position + bytesCount < (_data.Length - 1 ) / 2)
			{
				var bytes = ReadNextByteSample(bytesCount);
				Assert.GreaterOrEqual(bytes.Length, bytesCount);
				
				_dataProvider.AddRawData(bytes);
				
				if (_dataProvider.HasData())
					processedFrames += _dataProvider.CreatePcmFramesFromData();
			}
			
			Assert.NotZero(processedFrames);

			var lastBit = _data.Length - _data.Position;
			
			Assert.NotZero(lastBit);

			var lastBuffer = ReadNextByteSample((int) lastBit);
			Assert.AreEqual(127329, _data.Position);
			
			_dataProvider.AddRawData(lastBuffer);
			
			Assert.NotZero(_dataProvider.CreatePcmFramesFromData());
			Assert.IsTrue(_dataProvider.BufferLastFrame());

			
			var clip = clipBuilder.CreateAudioClipStream("test",16);
			
			//702720 Assert.AreEqual(706176, _dataProvider.AudioClipBuilder.ProcessedSamplesCount);
			Assert.AreEqual(702720, _dataProvider.AudioClipBuilder.ProcessedSamplesCount);
			Assert.AreEqual(16f, clip.length); //Initial length of audio clip
			Assert.AreEqual(44100, clip.frequency);
			Assert.AreEqual(16f, clip.samples / clip.frequency); //Proper length from samples
			
			yield return AudioPlayer.PlayClipRoutineStatic(clip);
		}
		
		[Test]
		public async Task Load_Data_Play_PCM_Samples_NotZero()
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
			Assert.AreEqual(127329, _data.Position);
			
			_dataProvider.AddRawData(lastBuffer);

			Assert.NotZero(_dataProvider.CreatePcmFramesFromData());
				
			Assert.IsTrue(_dataProvider.BufferLastFrame());
			
			var clip = clipBuilder.CreateAudioClipStream("test",16);

//639360			Assert.AreEqual(706176, _dataProvider.AudioClipBuilder.ProcessedSamplesCount);
			
			Assert.AreEqual(16f, clip.length); //Initial length of audio clip
			Assert.AreEqual(44100, clip.frequency);
			Assert.AreEqual(16f, clip.samples / clip.frequency); //Proper length from samples
			await AudioPlayer.PlayClipStatic(clip);
		}

		private byte[] ReadNextByteSample(int bytesCount = 1024)
		{
			var buffer = new byte[bytesCount];
			var readSize = _data.Read(buffer, 0, bytesCount);
			Assert.AreEqual(bytesCount, readSize);
			return buffer;
		}
		
		private AudioClipBuilder CreateAudioBuilderFromHeader(int dataSize = 1024)
		{
			var buffer = ReadNextByteSample(dataSize);
			Assert.AreEqual(dataSize, buffer.Length);
			
			_dataProvider.AddRawData(buffer);
			return _dataProvider.CreateAudioBuilder(AudioDataType.Mp3, 44100);
		}
	}
}