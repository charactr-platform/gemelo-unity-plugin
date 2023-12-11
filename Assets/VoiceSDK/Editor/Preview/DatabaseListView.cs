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

		private const string TITLE = "Preview and select voice:";
		private Button _button;
		private ListView _listView;
	
		private long _voiceItemTimestamp = -1;
		private VoiceLibrary _targetLibrary;
		private ListType _listType;
		
		private static Dictionary<VoicePreview, SerializedProperty> _itemsDictionary;
		
		[MenuItem("Tools/Gemelo.ai Voice/Voices database")]
		public static void ShowWindow()
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.titleContent = new GUIContent(TITLE);
			wnd.SetListType(ListType.None);
			wnd.Show(true);
		}
		
		public static void ShowSelectionWindow(SerializedProperty property)
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.titleContent = new GUIContent(TITLE);
			wnd.RegisterItemProperty(property);
			wnd.SetListType(ListType.Selection);
			wnd.ShowModal();
		}
		
		public static void ShowSelectionWindow(long timestamp, VoiceLibrary targetLibrary)
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.RegisterItemId(timestamp, targetLibrary);
			wnd.SetListType(ListType.Selection);
			wnd.titleContent = new GUIContent(TITLE);
			wnd.ShowModal();
		}

		public void SetListType(ListType type)
		{
			_listType = type;
		}
		
		private void RegisterItemId(long id, VoiceLibrary targetLibrary)
		{
			_voiceItemTimestamp = id;
			_targetLibrary = targetLibrary;
		}

		private void RegisterItemProperty(SerializedProperty property)
		{
			_voiceItemTimestamp = property.FindPropertyRelative("voiceItemId").longValue;
			_targetLibrary = property.serializedObject.targetObject as VoiceLibrary;
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
			
			//TODO: To much string names here :'(
			var orderedList = items.OrderByDescending(o => o.FindPropertyRelative("itemData").FindPropertyRelative("Rating").floatValue).ToList();
			
			listView.itemsSource = orderedList;
			listView.makeItem = CreatePreviewElement;
			listView.bindItem = (element, i) => (element as VoicePreviewElement).RegisterProperty(orderedList[i]);
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

			if (_voiceItemTimestamp > -1)
			{
				_targetLibrary.SetVoicePreviewForItemTimestamp(_voiceItemTimestamp, preview);
			}
			else
			{
				Debug.LogError($"Can't find itemId: {_voiceItemTimestamp}");
			}

			Close();
		}
		
		private PopupWindow CreatePopup()
		{
			var text = _listType switch
			{
				ListType.None => "Available voices: ",
				ListType.Change => "Click 'Change' to change current voice:",
				ListType.Selection => "Click 'Select' to select new voice:",
				_ => string.Empty
			};

			var popup = new PopupWindow
			{
				text = text,
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