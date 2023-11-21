using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Rest.Client;
using Gemelo.Voice.Rest.Model;
using NUnit.Framework;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Gemelo.Voice.Editor.Preview
{
	[Serializable]
	public struct PreviewItemData
	{
		public string Name;
		public int Id;
		public string PreviewUrl;
		public string Description;
		public string[] Labels;
		public float Rating;
	}

	[Serializable]
	public struct AudioDetails
	{
		public int SampleRate;
		public int BitDepth;
		public float Duration;
		public int DataOffset;
	}
	
	[Serializable]
	public class VoicePreview
	{
		public string Name
		{
			get => itemData.Name;
			set => itemData.Name = value;
		}

		public int Id
		{
			get => itemData.Id;
		}

		public float Rating
		{
			get => itemData.Rating;
		}
		public AudioDetails AudioDetails
		{
			get => audioDetails;
		}
		
		public string CacheFilePath => Path.Combine(Configuration.CachePath, dataFileName);
		public bool CacheExists => File.Exists(CacheFilePath);
		public override string ToString() => $"{Name} - {dataFileName}";

		[SerializeField] private PreviewItemData itemData;
		[SerializeField] private AudioDetails audioDetails;
		[SerializeField] private long previewDataSize;
		[SerializeField] private string dataFileName;
		
		public VoicePreview(VoicePreviewItem item)
		{
			itemData = new PreviewItemData()
			{
				Id = item.Id,
				Name = item.Name,
				PreviewUrl = item.Url,
				Description = item.Description,
				Rating = item.Rating,
				Labels = item.Labels.Select(s=>s.Label).ToArray()
			};
		}

		public long EncodePcmFramesToCache(List<PcmFrame> pcmFrames, out string fileName)
		{
			var size = 0L;

			using (var memory = new MemoryStream())
			{
				using (Stream stream = new DeflateStream(memory, CompressionLevel.Optimal, true))
				{
					var binaryFormatter = new BinaryFormatter();
					binaryFormatter.Serialize(stream, pcmFrames);
					stream.Flush();
				}
				
				fileName = WriteToFileCache(memory.GetBuffer());
				size = memory.Length;
			}
			
			//Assign dataFileName property
			dataFileName = fileName;
			
			return size;
		}

		private string WriteToFileCache(byte[] data)
		{
			var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
			var fileName = $"{Id}_{unique}";
			var path = Configuration.CachePath;
			
			Directory.CreateDirectory(path);
			File.WriteAllBytes(Path.Combine(path, fileName), data);
			return fileName;
		}

		private byte[] ReadFromCache()
		{
			return File.ReadAllBytes(CacheFilePath);
		}
		
		public bool DecodeCacheDataToPcmFrames(out List<PcmFrame> frames)
		{
			frames = null;
			
			if (!CacheExists)
			{
				Debug.LogError($"Can't find cache data [{CacheFilePath}],\ncache was purged?");
				return false;
			}
			
			using (var memory = new MemoryStream(ReadFromCache()))
			{
				using (Stream stream = new DeflateStream(memory, CompressionMode.Decompress, true))
				{
					var binaryFormatter = new BinaryFormatter();
					frames = (List<PcmFrame>)binaryFormatter.Deserialize(stream);
				}
			}

			return frames != null && frames.Count > 0;
		}
		
		public AudioClip GenerateAudioClip()
		{
			if (!DecodeCacheDataToPcmFrames(out var frames))
				throw new Exception("Can't load cache data, please update cache database");

			return GenerateAudioClip(frames);
		}

		public AudioClip GenerateAudioClip(List<PcmFrame> frames)
		{
			if (frames== null || frames.Count == 0)
				throw new Exception("Can't create audio clip, data is empty");
			
			var builder = new WavBuilder(audioDetails.SampleRate, audioDetails.BitDepth);
			
			foreach (var pcmFrame in frames)
				builder.BufferSamples(pcmFrame);

			if (builder.BufferLastFrame(out var frame))
				builder.BufferSamples(frame);

			audioDetails.Duration = builder.Duration;
			
			return builder.CreateAudioClipStream(itemData.Name, Mathf.CeilToInt(builder.Duration));
		}
        
		public List<PcmFrame> WriteAudioFrames(byte[] data)
		{
			var header = new WavHeaderData(data);
			
			var wavBuilder = new WavBuilder(header.SampleRate, header.BitDepth, data.AsSpan(0, header.DataOffset).ToArray());
			
			audioDetails = new AudioDetails
			{
				SampleRate = header.SampleRate,
				DataOffset = header.DataOffset,
				BitDepth = header.BitDepth,
				Duration = wavBuilder.Duration
			};
			
			return wavBuilder.ToPcmFrames(data.AsSpan(header.DataOffset).ToArray());
		}
        
		public static async Task<byte[]> GetAudioPreviewData(string previewUrl)
		{
			var configuration = Configuration.Load();
			Assert.NotNull(configuration);
			var http = new EditorRestClient(configuration, Debug.LogError);
			return await http.GetDataAsync(previewUrl);
		}

		public async Task<bool> FetchVoicePreviewData()
		{
			if (string.IsNullOrEmpty(itemData.PreviewUrl))
				throw new Exception("Can't download voice preview, URL is empty");

			var data = await GetAudioPreviewData(itemData.PreviewUrl);

			if (data == null)
			{
				Debug.LogError($"Can't download data from remote resource, id: {itemData.Id}");
				return false;
			}

			var frames = WriteAudioFrames(data);
			previewDataSize = EncodePcmFramesToCache(frames, out dataFileName);
			
			Debug.Log($"Saved PcmFrames: {frames.Count}, filename: {dataFileName}, size: {previewDataSize / 1024f:F2}kb");
			
			return frames.Count != 0 && previewDataSize != 0;
		}
	}
}