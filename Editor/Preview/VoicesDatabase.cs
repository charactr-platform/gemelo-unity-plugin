using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gemelo.Voice.Rest.Client;
using Gemelo.Voice.Rest.Model;
using UnityEditor;
using UnityEngine;

namespace Gemelo.Voice.Editor.Preview
{
	public class VoicesDatabase: ScriptableObject
	{
		public const string FILE_ASSET = "VoicesDatabase";
		public List<VoicePreview> Voices => voices;

		private static VoicesDatabase _instance;
	
		[SerializeField] private List<VoicePreview> voices;
		
		public static VoicesDatabase CreateInstance(bool writeAsset = false)
		{
			var instance = ScriptableObject.CreateInstance<VoicesDatabase>();
			
#if UNITY_EDITOR
			if (writeAsset)
			{
				var path = Voice.Configuration.GLOBAL_SAVE_PATH + FILE_ASSET + ".asset";
				AssetDatabase.CreateAsset(instance, path);
				AssetDatabase.SaveAssetIfDirty(instance);
				AssetDatabase.ImportAsset(path);
			}
#endif

			return instance;
		}
		
		public async Task<bool> AddVoicePreview(IVoicePreviewItem previewItemItem, IProgress<float> onProgress)
		{
			voices ??= new List<VoicePreview>();

			var preview = new VoicePreview(previewItemItem);
			
			onProgress.Report(0.5f);

			var success = await preview.FetchVoicePreviewData();
			
			if (success && AddOrUpdatePreview(preview))
				Debug.Log($"Added new voice preview for Voice: {preview.Name}");

			onProgress.Report(0.5f);

			return success;
		}

		private bool AddOrUpdatePreview(VoicePreview preview)
		{
			if (PreviewExists(preview.Id, out var index))
			{
				voices[index] = preview;
				return false;
			}
			
			voices.Add(preview);
			return true;
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
			
			if (!PreviewExists(itemId, out var index))
				return false;
			
			voicePreview = voices[index];
			return true;
		}

		public bool PreviewExists(int itemId, out int index)
		{
			index = -1;
			
			if (itemId == 0)
				return false;
			
			index = voices.FindIndex(f => f.Id == itemId);
			return index >= 0;
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

		private async Task<SystemVoicesResponse> GetVoicesResponse(bool all = false)
		{
			var configuration = Voice.Configuration.Load();
			var http = new EditorRestClient(configuration, message => Debug.LogWarning(message.Message));
			var url = Voice.Configuration.VOICES_API + (all ? "?show=all" : string.Empty); 
			return await http.GetAsync<SystemVoicesResponse>(url);
		}

		private async Task<ClonedVoicesResponse> GetClonedVoicesResponse()
		{
			var configuration = Voice.Configuration.Load();
			var http = new EditorRestClient(configuration, message => Debug.LogError(message.Message));
			return await http.GetAsync<ClonedVoicesResponse>(Voice.Configuration.CLONED_API);
		}
		
		public async Task<bool[]> UpdatePreviewsDatabase(IProgress<float> onProgress)
		{
			var systemVoices = await GetVoicesResponse();
			var clonedVoices = await GetClonedVoicesResponse();

			var voicesResponse = new VoicesResponse(systemVoices.Concat(clonedVoices.Items));
			
			var tasks = new List<Task<bool>>();

			var completedCount = 0F;
			var totalCount = voicesResponse.Items.Count();
			
			var progress = new Progress<float>((p) =>
			{
				completedCount += p;
				onProgress.Report(completedCount / totalCount);
			});
			
			foreach (var voiceData in voicesResponse.Items)
			{
				var task = AddVoicePreview(voiceData, progress);
				tasks.Add(task);
			}

			var taskAll = await Task.WhenAll(tasks);
			
			return taskAll;
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
			if (_instance != null)
				return _instance;
			
			if (Exists())
			{
				_instance = Resources.Load<VoicesDatabase>(FILE_ASSET);
				Debug.Log($"Loaded database previews instance, previews count: {_instance.Voices.Count}");
			}
			else
			{
				_instance = CreateInstance(true);
			}
			
			return _instance;
		}

		public bool Validate() => Voices?.Count > 0 && 
		                          Voices.All(a => a.CacheExists);

		public static bool Exists() => 
			Resources.Load<VoicesDatabase>(FILE_ASSET) != null;
	}
}