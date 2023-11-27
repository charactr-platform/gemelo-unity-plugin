using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gemelo.Voice.Editor.Preview;
using UnityEditor;
using UnityEngine;

namespace Gemelo.Voice.Library
{
	[CreateAssetMenu(menuName = "VoiceSDK/Create VoiceLibrary", fileName = "VoiceLibrary", order = 0)]
	public class VoiceLibrary : ScriptableObject
	{
		public const string SAVE_PATH = "Resources";
		
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

			var item = new VoiceItem(voiceId)
			{
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

		public void SetVoicePreviewForItemId(int itemId, VoicePreview preview)
		{
			var itemIndex = Items.FindIndex(f => f.Id == itemId);
			SetPreviewForItem(itemIndex, preview);
		}
		public void SetVoicePreviewForItemVoiceId(int itemVoiceId, VoicePreview preview)
		{
			var itemIndex = Items.FindIndex(f => f.VoiceId == itemVoiceId);
			SetPreviewForItem(itemIndex, preview);
		}

		private void SetPreviewForItem(int index, VoicePreview preview)
		{
			Items[index].SetVoicePreview(preview);
			var obj = new SerializedObject(this);
			obj.ApplyModifiedProperties();
			obj.Dispose();
		}
		public async Task<int> ConvertTextsToAudioClips(Action<int> onItemDownloaded)
		{
			var processedItems = 0;
			
			foreach (var voiceItem in items)
			{
				await voiceItem.GetAudioClip();
				processedItems++;
				onItemDownloaded?.Invoke(processedItems);
			}
			
			if (processedItems > 0)
			{
				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssetIfDirty(this);
				
				Debug.Log($"Saved library asset = {name}, updated items count = {processedItems}");
			}

			return processedItems;
		}
#endif
	}
}