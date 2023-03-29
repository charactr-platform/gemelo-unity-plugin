using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Charactr.SDK.Library
{
	[CreateAssetMenu(menuName = "VoiceSDK/Create VoiceLibrary", fileName = "VoiceLibrary", order = 0)]
	public class VoiceLibrary : ScriptableObject
	{
		public const string SAVE_PATH = "Charactr/Resources";
		public List<VoiceItem> Items
		{
			get => items;
		}
        
		[SerializeField] private List<VoiceItem> items;
		
		public VoiceLibrary()
		{
			items = new List<VoiceItem>();
		}

		public static void Create()
		{
			var instance = CreateInstance<VoiceLibrary>();
			var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{SAVE_PATH}/VoiceLibrary.asset");
			AssetDatabase.CreateAsset(instance, path);
		}
		public bool GetItemByVoiceId(int voiceId, out VoiceItem voiceItem)
		{
			var index = items.FindIndex(f => f.VoiceId == voiceId);

			voiceItem = default;
			
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
			
			voiceItem = default;
			
			if (index < 0)
			{
				Debug.LogError($"Can't find VoiceItem with id = {id}");
				return false;
			}

			//Warning, copy here
			voiceItem = items[index];
			
			return true;
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
				Debug.LogAssertion(item.AudioClip != null);
			}
			else
			{
				Debug.LogError("Can't find valid voice item with id = {id}");
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