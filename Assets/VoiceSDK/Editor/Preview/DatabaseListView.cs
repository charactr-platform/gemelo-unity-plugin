using System;
using System.Collections.Generic;
using System.Linq;
using Gemelo.Voice.Editor.Preview;
using Gemelo.Voice.Library;
using Gemelo.Voice.Rest.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Library
{
	public class DatabaseListView : EditorWindow
	{
		public VisualTreeAsset visualTreeAsset;
		
		private Button _button;
		private int _voiceItemId = -1;
		private long _voiceItemTimestamp = -1;
		private VoiceLibrary _targetLibrary;
		private ListType _listType;
		private ListView _listView;
		
		private static Dictionary<VoicePreview, VoicePreviewSerializedProperty> _itemsDictionary;
		
		[MenuItem("Tools/Gemelo.ai Voice/Voices database")]
		public static void ShowWindow()
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.SetType(ListType.None);
			wnd.Show(true);
		}
		
		public static void ShowSelectionWindow(SerializedProperty property)
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.SetType(ListType.Selection);
			wnd.RegisterItemProperty(property);
			wnd.ShowModal();
		}
		
		public static void ShowSelectionWindow(long timestamp, VoiceLibrary targetLibrary)
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.SetType(ListType.Creation);
		
			wnd.RegisterItemTimestamp(timestamp, targetLibrary);
			wnd.ShowModal();
		}

		private void SetType(ListType type)
		{
			_listType = type;
			titleContent = new GUIContent(GetTitle(type));
		}
		
		private void RegisterItemTimestamp(long timestamp, VoiceLibrary targetLibrary)
		{
			_voiceItemTimestamp = timestamp;
			_targetLibrary = targetLibrary;
		}

		private void RegisterItemProperty(SerializedProperty property)
		{
			_voiceItemId = property.FindPropertyRelative("voiceItemId").intValue;
			_targetLibrary = property.serializedObject.targetObject as VoiceLibrary;
		}
		
		private static string GetTitle(ListType listType) => listType switch
		{
			ListType.None => "Available voices",
			ListType.Change => "Click 'Change' to change current voice",
			ListType.Creation => "Click 'Select' to set new voice",
			ListType.Selection => "Click 'Select' to select new voice",
			_ => string.Empty
		};
		
		private void CreateGUI()
		{
			// Each editor window contains a root VisualElement object
			visualTreeAsset.CloneTree(rootVisualElement);
			_listView = rootVisualElement.Q<ListView>();
			UpdateList("All");
			CreateDropdownSelection();
		}
		
		private void CreateDropdownSelection()
		{
			var voiceType = rootVisualElement.Q<DropdownField>("voiceTypeDropdown");
			
			voiceType.choices = new List<string>()
			{
				"All", "System", "Cloned"
			};
			
			voiceType.SetValueWithoutNotify(voiceType.choices[0]);
			
			voiceType.RegisterValueChangedCallback((e)=>UpdateList(e.newValue));
		}

		private void UpdateList(string newValue)
		{
			CreateList( Enum.Parse<VoiceType>(newValue));
		}

		private void DisplayNoItems()
		{
			EditorUtility.DisplayDialog("No items found",
				"Please use 'Tools -> Gemelo.ai Voice -> Update voice previews'\n" +
				"to fetch voice previews database", "OK");
		}
		
		private List<VoicePreviewSerializedProperty> LoadPreviewItems(VoiceType voiceType)
		{
			_itemsDictionary = new Dictionary<VoicePreview, VoicePreviewSerializedProperty>();
			
			var fields = new List<VoicePreviewSerializedProperty>();
			var database = VoicesDatabase.Load();
			var serializedObject = new SerializedObject(database);
			var voices = serializedObject.FindProperty("voices");
			
			for (int i = 0; i < voices.arraySize; i++)
			{
				var element = database.Voices[i];
				var property = new VoicePreviewSerializedProperty(voices.GetArrayElementAtIndex(i));

				if (voiceType == VoiceType.All)
				{
					_itemsDictionary.Add(element, property);
					fields.Add(property);
					continue;
				}

				if (element.Type == voiceType)
				{
					_itemsDictionary.Add(element, property);
					fields.Add(property);
				}
			}
			
			return fields.OrderByDescending(o => o.ItemData.Rating).ToList();
		}
		
		private void CreateList(VoiceType voiceType)
		{
			if (_listView == null)
				throw new Exception("Can't find list view object");
			
			var items = LoadPreviewItems(voiceType);

			if (items.Count == 0)
				DisplayNoItems();
			
			_listView.Clear();
			_listView.itemsSource = items;
			_listView.makeItem = CreatePreviewElement;
			_listView.bindItem = BindElement;
		}

		private void BindElement(VisualElement element, int index)
		{
			var property = (VoicePreviewSerializedProperty) _listView.itemsSource[index];
			((VoicePreviewElement)element).RegisterProperty(property.Base);
		}
		
		private VoicePreviewElement CreatePreviewElement()
		{
			var element = new VoicePreviewElement(_listType);
			element.RegisterOnSelect(OnSelectedItem);
			return element;
		}

		private void OnSelectedItem(int id)
		{
			VoicePreview preview = null;
			
			try
			{
				var item = _itemsDictionary.First(f => f.Key.Id == id);
				preview = item.Key;
			}
			catch (Exception e)
			{
				Debug.LogError($"Can't find preview in database! Error: {e.Message}");
				return;
			}
		
			Debug.Log(preview.Name);

			var libraryObject = new SerializedObject(_targetLibrary);
			
			switch (_listType)
			{
				case ListType.Creation:
					VoiceItemElement.SetVoicePreviewForItemByTimestamp(libraryObject, _voiceItemTimestamp, preview);
					break;

				case ListType.Selection:
					VoiceItemElement.SetVoicePreviewForItemById(libraryObject, _voiceItemId, preview);
					break;
			}

			libraryObject.ApplyModifiedPropertiesWithoutUndo();
			libraryObject.Dispose();
			Close();
		}
	}
}