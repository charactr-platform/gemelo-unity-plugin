
using System;
using System.Collections.Generic;
using System.Linq;
using Gemelo.Voice.Editor.Preview;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace Charactr.VoiceSDK.Editor.Preview
{
	
	[CustomEditor(typeof(VoicesDatabase))]
	public class DatabaseInspector : UnityEditor.Editor
	{
		public VisualTreeAsset visualTreeAsset;
		private Button _updateButton,_purgeButton;
		private VisualElement _inspector;
		private ListView _listView;
		private DropdownField _dropdown;
		private static SerializedProperty _voices;
		
		public override VisualElement CreateInspectorGUI()
		{
			_voices = serializedObject.FindProperty("voices");
			// Create a new VisualElement to be the root of our inspector UI
			_inspector = new VisualElement();
			
			// Load from default reference
			visualTreeAsset.CloneTree(_inspector);
			_updateButton = _inspector.Q<Button>("updateButton");
			_purgeButton = _inspector.Q<Button>("purgeButton");
			_listView = _inspector.Q<ListView>();
			_dropdown = _inspector.Q<DropdownField>();
			_dropdown.RegisterCallback<ClickEvent>(e=> CreateDropdownList());
			_updateButton.RegisterCallback<ClickEvent>((e) => OnUpdateButton());
			_purgeButton.RegisterCallback<ClickEvent>(e=> OnPurgeButton());

			var list = LoadPreviewItems();
			_listView = CreateList(list);
			return _inspector;
		}

		private void CreateDropdownList()
		{
			CreatePreviewPopup(_inspector, _dropdown, (p) =>
			{
				_dropdown.value = p.propertyPath;
			});
		}
		
		private void OnPurgeButton()
		{
			if (EditorUtility.DisplayDialog($"Are you sure?", $"Do You really want to purge cache of voice previews", "YES", "NO"))
				VoicesDatabase.PurgeCache();
		}

		public void CreatePreviewPopup(VisualElement parent, VisualElement button, Action<SerializedProperty> onSelected)
		{
			var popup = new PopupWindow
			{
				text = "Title",
				style =
				{
					width = 300,
					height = 300,
					position = new StyleEnum<Position>(Position.Absolute),
					top = button.contentRect.center.y + 50,
					left = button.contentRect.center.x - 150,
					borderTopLeftRadius = 0,
					borderTopRightRadius = 0
				}
			};
			
			var items = LoadPreviewItems();
			var list = CreateList(items);
			
			popup.RegisterCallback<ClickEvent>(e=>
			{
				parent.Remove(popup);
				onSelected.Invoke(items[list.selectedIndex]);
			});
			
			popup.Add(list);
			parent.Add(popup);
		}
		
		private ListView CreateList(List<SerializedProperty> items)
		{
			_listView.itemsSource = items;
			_listView.makeItem = () => new VoicePreviewElement();
			_listView.bindItem = (element, i) => (element as VoicePreviewElement).RegisterProperty(items[i]);
			return _listView;
		}
	
		private void MakeALikeB(VisualElement A, VisualElement B)
		{
			A.style.position = B.style.position;
			A.style.top = B.style.top;
			A.style.bottom = B.style.bottom;
			A.style.left = B.style.left;
			A.style.right = B.style.right;
 
			A.style.flexGrow = B.style.flexGrow;
			A.style.flexShrink = B.style.flexShrink;
			A.style.width = B.style.width;
			A.style.height = B.style.height;
			A.style.transformOrigin = B.style.transformOrigin;
		}
		private static List<SerializedProperty> LoadPreviewItems()
		{
			var fields = new List<SerializedProperty>();
			
			for (int i = 0; i < _voices.arraySize; i++)
			{
				var element = _voices.GetArrayElementAtIndex(i);
				fields.Add(element);
				Debug.Log("Added element "+ i + " "+ element.displayName);
			}
			return fields;
		}
		
		private async void OnUpdateButton()
		{
			var library = target as VoicesDatabase;
			
			if (EditorUtility.DisplayDialog("Start update", "Start update operation on all items ?", "YES", "CANCEL"))
			{
				Selection.objects = null;
				var items = await VoicesDatabase.GetVoicesResponse();
				var count = items.Data.Count;
				EditorUtility.DisplayProgressBar($"Downloading...", $"Downloading items [{0}/{count}]", 0f);
				await library.UpdatePreviewsDatabase();
				Selection.SetActiveObjectWithContext(target, library);
				EditorUtility.ClearProgressBar();
			}
		}
		
	}
}