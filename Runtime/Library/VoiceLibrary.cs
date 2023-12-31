﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

		public bool GetItemByTimestamp(long timestamp, out VoiceItem voiceItem)
		{
			var index = items.FindIndex(f => f.Timestamp == timestamp);
			
			voiceItem = null;
			if (index < 0)
			{
				var itemsFound = string.Join(", ", items.Select(s=>s.Timestamp));
				Debug.LogError($"Can't find VoiceItem with timestamp = {timestamp}, Founded: {itemsFound}");
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
				var itemsFound = string.Join(", ", items.Select(s=>s.Id));
				Debug.LogError($"Can't find VoiceItem with id = {id}, Founded: {itemsFound}");
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
			Debug.Log($"Created new VoiceItem with id = {item.Id}, timestamp = {item.Timestamp}");
			
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
	}
}