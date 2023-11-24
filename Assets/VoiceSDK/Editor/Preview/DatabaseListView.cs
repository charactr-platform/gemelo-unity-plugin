using System;
using System.Collections.Generic;
using System.Linq;
using Gemelo.Voice.Editor.Preview;
using Gemelo.Voice.Library;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace Gemelo.Voice.Editor.Library
{
	public class DatabaseListView : EditorWindow
	{
		public VisualTreeAsset visualTreeAsset;

		private Button _button;
		private ListView _listView;
		private SerializedProperty _voicePreviewProperty;

		private int _voiceItemId = -1;
		private VoiceLibrary _targetLibrary;
		
		private const string TITLE = "Preview and select voice:";
		private static Dictionary<VoicePreview, SerializedProperty> _itemsDictionary;
		public static void ShowWindow()
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.Show();
		}
		
		public static void ShowChangeWindow(SerializedProperty element)
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.RegisterVoiceItem(element);
			wnd.titleContent = new GUIContent(TITLE);
			wnd.ShowAuxWindow();
		}
		
		public static void ShowChangeWindow(int itemId, VoiceLibrary targetLibrary)
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.RegisterItemId(itemId, targetLibrary);
			wnd.titleContent = new GUIContent(TITLE);
			wnd.ShowAuxWindow();
		}

		private void RegisterVoiceItem(SerializedProperty voiceItem)
		{
			_voicePreviewProperty = voiceItem;
			_targetLibrary = voiceItem.serializedObject.targetObject as VoiceLibrary;
		}

		private void RegisterItemId(int id, VoiceLibrary targetLibrary)
		{
			_voiceItemId = id;
			_targetLibrary = targetLibrary;
		}
		
		private void CreateGUI()
		{
			// Each editor window contains a root VisualElement object
			var root = rootVisualElement;
			var popup = CreatePopup();
			popup.Add(visualTreeAsset.Instantiate());
			root.Add(popup);
			
			_listView = popup.Q<ListView>();
			CreateList(_listView, LoadPreviewItems());
		}
		
		public static List<SerializedProperty> LoadPreviewItems()
		{
			_itemsDictionary = new Dictionary<VoicePreview, SerializedProperty>();
			var fields = new List<SerializedProperty>();
			var database = VoicesDatabase.Load();
			var serializedObject = new SerializedObject(database);
			var voices = serializedObject.FindProperty("voices");
			
			for (int i = 0; i < voices.arraySize; i++)
			{
				var element = database.Voices[i];
				var property = voices.GetArrayElementAtIndex(i);
				
				_itemsDictionary.Add(element, property);
				fields.Add(property);
			}
			return fields;
		}
		
		private void CreateList(ListView listView, List<SerializedProperty> items)
		{
			if (listView == null)
				throw new Exception("Can't find list view object");
			
			listView.itemsSource = items;
			listView.makeItem = CreatePreviewElement;
			listView.bindItem = (element, i) => (element as VoicePreviewElement).RegisterProperty(items[i]);
		}

		private VoicePreviewElement CreatePreviewElement()
		{
			var element = new VoicePreviewElement(true);
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

			if (_voiceItemId > -1)
			{
				_targetLibrary.SetVoicePreviewForItemId(_voiceItemId, preview);
			}
			else
			{
				var itemData = _voicePreviewProperty.FindPropertyRelative("itemData");
				var originalVoiceId = itemData.FindPropertyRelative("Id");
				_targetLibrary.SetVoicePreviewForItemVoiceId(originalVoiceId.intValue, preview);
			}
			
			Close();
		}
		
		private PopupWindow CreatePopup()
		{
			var popup = new PopupWindow
			{
				text = "Click 'Select' to change current voice",
				style =
				{
					position = new StyleEnum<Position>(Position.Relative),
					flexBasis = new StyleLength(StyleKeyword.Auto),
					flexGrow = 1,
					borderTopLeftRadius = 0,
					borderTopRightRadius = 0
				}
			};
			return popup;
		}
	}
}