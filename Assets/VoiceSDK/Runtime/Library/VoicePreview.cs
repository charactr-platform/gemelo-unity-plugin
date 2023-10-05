using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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
	public class PreviewItemData
	{
		public string Name;
		public int Id;
		public int SampleRate;
		public float Duration;
		public int DataOffset;
		public string PreviewUrl;
		public string Description;
		public string[] Labels;
		public float Rating;
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

		public string CacheFileName => previewDataName;
		
		public DateTimeOffset SaveTime => DateTimeOffset.FromUnixTimeMilliseconds(string.IsNullOrEmpty(previewDataName) ?
														0 : long.Parse(previewDataName));
		
		[SerializeField] private PreviewItemData itemData;
		[SerializeField] private long previewDataSize;
		[SerializeField] private string previewDataName;
		
		private List<Audio.PcmFrame> _pcmFrames;
		
		public VoicePreview(VoicePreviewItem item)
		{
			itemData = new PreviewItemData()
			{
				Id = item.Id,
				Name = item.Name,
				PreviewUrl = item.PreviewUrl,
				Description = item.Description,
				Rating = item.Rating,
				Labels = item.Labels.Select(s=>s.Label).ToArray()
			};
			
			_pcmFrames = new List<Audio.PcmFrame>();
		}

		public long EncodePcmFramesToData(out string fileName)
		{
			var size = 0L;
			fileName = string.Empty;
			
			using (var memory = new MemoryStream())
			{
				using (Stream stream = new DeflateStream(memory, CompressionLevel.Optimal, true))
				{
					var binaryFormatter = new BinaryFormatter();
					binaryFormatter.Serialize(stream, _pcmFrames);
					stream.Flush();
				}

				fileName = WriteToFileCache(memory.GetBuffer());
				size = memory.Length;
			}

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

		private byte[] ReadFromCache(string fileName)
		{
			var path = Path.Combine(Configuration.CachePath, fileName);
			return File.ReadAllBytes(path);
		}
		
		public int DecodeCacheDataToPcmFrames(string fileName)
		{
			if (!File.Exists(Path.Combine(Configuration.CachePath, fileName)))
			{
				Debug.LogError("Can't find cache file, cache was purged?");
				return 0;
			}
				
			using (var memory = new MemoryStream(ReadFromCache(fileName)))
			{
				using (Stream stream = new DeflateStream(memory, CompressionMode.Decompress, true))
				{
					var binaryFormatter = new BinaryFormatter();
					_pcmFrames = (List<PcmFrame>)binaryFormatter.Deserialize(stream);
				}
			}

			return _pcmFrames.Count;
		}
		
		public AudioClip GenerateAudioClip()
		{
			return CreateAudioClipFromPcmFrames(_pcmFrames);
		}

		private AudioClip CreateAudioClipFromPcmFrames(List<PcmFrame> data)
		{
			var builder = new WavBuilder(itemData.SampleRate);
			
			foreach (var pcmFrame in data)
				builder.BufferSamples(pcmFrame);

			if (builder.BufferLastFrame(out var frame))
				builder.BufferSamples(frame);

			itemData.Duration = builder.Duration;
			
			return builder.CreateAudioClipStream(itemData.Name, Mathf.CeilToInt(builder.Duration));
		}
        
		public int WriteAudioFrames(byte[] data)
		{
			var header = new WavHeaderData(data);
			itemData.DataOffset = header.DataOffset;
			itemData.SampleRate = header.SampleRate;
			
			var wavBuilder = new WavBuilder(header.SampleRate, data.AsSpan(0, header.DataOffset).ToArray());
			_pcmFrames.AddRange(wavBuilder.ToPcmFrames(data.AsSpan(header.DataOffset).ToArray()));
			return _pcmFrames.Count;
		}
        
		public static async Task<byte[]> GetAudioPreviewData(string previewUrl)
		{
			var configuration = Voice.Configuration.Load();
			Assert.NotNull(configuration);
			var http = new EditorRestClient(configuration, Debug.LogError);
			return await http.GetDataAsync(previewUrl);
		}

		public async Task<bool> GetVoicePreviewData()
		{
			if (string.IsNullOrEmpty(itemData.PreviewUrl))
				throw new Exception("Can't download voice preview, URL is empty");
            
			var frames = WriteAudioFrames(await GetAudioPreviewData(itemData.PreviewUrl));
			
			previewDataSize = EncodePcmFramesToData(out previewDataName);
			
			Debug.Log($"Saved PcmFrames: {frames}, filename: {previewDataName}");
			
			return frames != 0 && previewDataSize != 0;
		}
	}
}