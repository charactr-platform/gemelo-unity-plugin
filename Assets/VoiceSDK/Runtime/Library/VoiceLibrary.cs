using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Charactr.VoiceSDK.Library
{
	[CreateAssetMenu(menuName = "VoiceSDK/Create VoiceLibrary", fileName = "VoiceLibrary", order = 0)]
	public class VoiceLibrary : ScriptableObject
	{
		public const string SAVE_PATH = "Charactr/Resources";
		
		public bool IsEmpty
		{
			get => items.Count == 0;
		}
		
		public List<VoiceItem> Items
		{
			get => items;
		}
        
		[SerializeField] private List<VoiceItem> items;
		
		public VoiceLibrary()
		{
			items = new List<VoiceItem>();
		}
		
		public bool GetItemByVoiceId(int voiceId, out VoiceItem voiceItem)
		{
			var index = items.FindIndex(f => f.VoiceId == voiceId);

			voiceItem = null;
			
			if (index < 0)
			{
				Debug.LogError($"Can't find VoiceItem with voiceId = {voiceId}");
				return false;
			}

			voiceItem = items[index];
			return true;
		}

		public bool GetItemById(int id, out VoiceItem voiceItem)
		{
			var index = items.FindIndex(f => f.Id == id);
			
			voiceItem = null;
			
			if (index < 0)
			{
				Debug.LogError($"Can't find VoiceItem with id = {id}");
				return false;
			}

			//Warning, copy here
			voiceItem = items[index];
			
			return true;
		}

		public bool GetAudioClipById(int id, out AudioClip audioClip)
		{
			audioClip = null;
			
			if (GetItemById(id, out var voiceItem) && voiceItem.AudioClip != null)
			{
				audioClip = voiceItem.AudioClip;
				return true;
			}
			
			Debug.LogError($"Can't find AudioClip for item with id={id}");
			return false;
		}
		
		public int AddNewItem(string text, int voiceId)
		{
			if (items == null)
				throw new Exception("Items not initialized yet");

			var item = new VoiceItem()
			{
				VoiceId = voiceId,
				Text = text,
			};
			
			if (!item.IsValid())
			{
				Debug.LogError("Item is not valid, missing text or proper voiceId");
				return -1;
			}
			
			items.Add(item);
			Debug.Log($"Created new VoiceItem with id = {item.Id}");
			
			return item.Id;
		}
		
		public async Task AddAudioClip(int id)
		{
			if (!GetItemById(id, out var item))
				throw new Exception("Item not found in library");

			if (item.IsValid())
			{
				await item.GetAudioClip();
			}
			else
			{
				Debug.LogError($"Can't find valid voice item with id = {id}");
			}
		}
		
#if UNITY_EDITOR
		public async void ConvertTextsToAudioClips()
		{
			var processedItems = 0;
			
			foreach (var voiceItem in items)
			{
				await voiceItem.GetAudioClip();
				processedItems++;
				Debug.Log(voiceItem.AudioClip.name);
			}
			
			if (processedItems > 0)
			{
				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssetIfDirty(this);
				Debug.Log($"Saved library asset = {name}, updated items count = {processedItems}");
			}
		}
#endif
	}
}