using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Gemelo.Voice.Rest.Client;
using Gemelo.Voice.Rest.Model;
using UnityEngine;

namespace Gemelo.Voice.Tests.Preview
{
	public class VoicesDatabase: ScriptableObject
	{
		public List<VoicePreview> Voices => voices;
		public static string CachePath => Path.Combine(Application.temporaryCachePath, "PreviewCache");

		[SerializeField] private List<VoicePreview> voices;
		public static VoicesDatabase CreateInstance()
		{
			return ScriptableObject.CreateInstance<VoicesDatabase>();
		}

		public async Task<bool> AddVoicePreview(VoicePreviewItem previewItem)
		{
			voices ??= new List<VoicePreview>();

			var preview = new VoicePreview(previewItem);

			if (await preview.GetVoicePreviewData() == false)
				return false;
            
			voices.Add(preview);
			Debug.Log($"Added voice preview for Voice: {preview.Name}");
			return true;
		}

		public VoicePreview GetVoicePreviewByName(string itemName)
		{
			var index = voices.FindIndex(f => f.Name.Equals(itemName));
			if (index < 0)
				throw new Exception($"Item with name [{itemName}] not found!");
            
			return voices[index];
		}

		public VoicePreview GetVoicePreviewById(int itemId)
		{
			var index = voices.FindIndex(f => f.Id == itemId);
			if (index < 0)
				throw new Exception($"Item with name [{name}] not found!");
            
			return voices[index];
		}
        
		public async Task<bool[]> UpdatePreviewsDatabase()
		{
			var configuration = Configuration.Load();
			var http = new EditorRestClient(configuration, message => Debug.LogError(message.Message));
			var voicesResponse = await http.GetAsync<VoicesResponse>(Configuration.VOICES_API);
            
			voices = new List<VoicePreview>();

			var tasks = new List<Task<bool>>();
			
			foreach (var voiceData in voicesResponse)
			{
				if (string.IsNullOrEmpty(voiceData.PreviewUrl))
				{
					Debug.LogWarning($"Can't download preview, missing URL: {voiceData.Name}");
					continue;
				}
				
				var t = AddVoicePreview(voiceData);
				tasks.Add(t);
			}

			return await Task.WhenAll(tasks);
		}

		public static int PurgeCache()
		{
			var dirInfo = new DirectoryInfo(CachePath);
			var files = dirInfo.GetFiles();
			Debug.Log($"Found files: {files.Length}");
			
			foreach (var file in files)
			{
				file.Delete();
			}

			return files.Length;
		}
	}
}