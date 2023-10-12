using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gemelo.Voice.Rest.Client;
using Gemelo.Voice.Rest.Model;
using UnityEngine;

namespace Gemelo.Voice.Editor.Preview
{
	public class VoicesDatabase: ScriptableObject
	{

		public const string FILE_ASSET = "VoicePreviewDatabase";
		public List<VoicePreview> Voices => voices;
	
		[SerializeField] private List<VoicePreview> voices;
		public static VoicesDatabase CreateInstance()
		{
			return ScriptableObject.CreateInstance<VoicesDatabase>();
		}

		public async Task<bool> AddVoicePreview(VoicePreviewItem previewItem, IProgress<bool> onProgress)
		{
			voices ??= new List<VoicePreview>();

			var preview = new VoicePreview(previewItem);

			var success = await preview.GetVoicePreviewData();
			
			if (success)
			{
				voices.Add(preview);
				Debug.Log($"Added voice preview for Voice: {preview.Name}");
			}
			
			onProgress.Report(success);
			return success;
		}

		public bool GetVoicePreviewByName(string itemName, out VoicePreview voicePreview)
		{
			voicePreview = null;
			var index = voices.FindIndex(f => f.Name.Equals(itemName));
			if (index < 0) return false;
			voicePreview = voices[index];
			return true;
		}

		public bool GetVoicePreviewById(int itemId, out VoicePreview voicePreview)
		{
			voicePreview = null;
			var index = voices.FindIndex(f => f.Id == itemId);
			if (index < 0) return false;
			voicePreview = voices[index];
			return true;
		}

		public bool GetBestVoicePreview(out VoicePreview voicePreview)
		{
			voicePreview = null;
			var bestRate = voices.Max(m => m.Rating);
			var best = voices.FirstOrDefault(f => f.Rating.Equals(bestRate));
			
			if (best != null)
			{
				voicePreview = best;
				return true;
			}

			return false;
		}

		public static async Task<VoicesResponse> GetVoicesResponse(bool all = true)
		{
			var configuration = Voice.Configuration.Load();
			var http = new EditorRestClient(configuration, message => Debug.LogError(message.Message));
			var url = Voice.Configuration.VOICES_API + (all ? "?show=all" : string.Empty); 
			return await http.GetAsync<VoicesResponse>(url);
		}
		
		public async Task<bool[]> UpdatePreviewsDatabase(IProgress<float> onProgress)
		{
			var voicesResponse = await GetVoicesResponse();
			
			voices = new List<VoicePreview>();

			var tasks = new List<Task<bool>>();

			var completedCount = 0;
			var totalCount = voicesResponse.Count;
			var progress = new Progress<bool>((p) =>
			{
				completedCount++;
				onProgress.Report((float) completedCount / totalCount);
			});
			
			foreach (var voiceData in voicesResponse)
			{
				await AddVoicePreview(voiceData, progress);
			}

			return await Task.WhenAll(tasks);
		}

		public static int PurgeCache()
		{
			var count = 0;
			
			var dirInfo = new DirectoryInfo(Voice.Configuration.CachePath);
			
			if (!dirInfo.Exists)
			{
				Debug.Log("Cache directory not found!");
				return count;
			}
			
			foreach (var file in  dirInfo.GetFiles())
			{
				file.Delete();
				count++;
			}

			Debug.Log($"Purged files count: {count}");
			
			return count;
		}

		public static void Clean()
		{
			var instance = Load();
			instance.Voices.Clear();
			PurgeCache();
		}
		
		public static VoicesDatabase Load()
		{
			//TODO: Check for file existence and for voices list size
			return Resources.Load<VoicesDatabase>(FILE_ASSET);
		}
	}
}