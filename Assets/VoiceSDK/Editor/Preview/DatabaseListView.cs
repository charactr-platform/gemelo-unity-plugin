using System;
using System.Collections.Generic;
using System.Linq;
using Gemelo.Voice.Editor.Preview;
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
		private SerializedProperty _voiceItemProperty;
		public static void ShowWindow()
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.ShowAuxWindow();
		}

		public static void ShowChangeWindow(SerializedProperty element)
		{
			var wnd = CreateInstance<DatabaseListView>();
			wnd.RegisterVoicePreview(element);
			wnd.ShowAuxWindow();
		}
		
		public void RegisterVoicePreview(SerializedProperty voiceItem)
		{
			_voiceItemProperty = voiceItem;
			ChangeVoiceIdPropertyValue(voiceItem);
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
			var serializedObject = new SerializedObject(VoicesDatabase.Load());
			var fields = new List<SerializedProperty>();
			var voices = serializedObject.FindProperty("voices");
			
			for (int i = 0; i < voices.arraySize; i++)
			{
				var element = voices.GetArrayElementAtIndex(i);
				fields.Add(element);
				Debug.Log("Added element "+ i + " "+ element.displayName);
			}
			return fields;
		}
		
		public static void CreateList(ListView listView, List<SerializedProperty> items)
		{
			if (listView == null)
				throw new Exception("Can't find list view object");
			
			listView.itemsSource = items;
			listView.makeItem = () => new VoicePreviewElement(true);
			listView.bindItem = (element, i) => (element as VoicePreviewElement).RegisterProperty(items[i]);
			listView.onItemsChosen += objects =>
			{
				Debug.Log("choosen" + objects.First());
			};
		}

		private void ChangeVoiceIdPropertyValue(SerializedProperty property)
		{
			//var item = property.FindPropertyRelative("voiceId");
			//var id = item.intValue;
			Debug.Log("Property value: "+property);
		}
		private PopupWindow CreatePopup()
		{
			var popup = new PopupWindow
			{
				text = "Title",
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