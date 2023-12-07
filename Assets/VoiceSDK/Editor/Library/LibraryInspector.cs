using System.Linq;
using Gemelo.Voice.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Library
{
	
	[CustomEditor(typeof(VoiceLibrary))]
	public class LibraryInspector : UnityEditor.Editor
	{
		public VisualTreeAsset inspectorXmlAsset;

		private Button _addButton, _removeButton, _saveButton;
		private VisualElement _inspector;
		private VisualElement _selected;
		private ListView _listView;
		private int _initialHash;
		private int _selectedIndex = -1;
		private SerializedProperty _items;
		public override VisualElement CreateInspectorGUI()
		{
			// Create a new VisualElement to be the root of our inspector UI
			_inspector = new VisualElement();
		
			// Load from default reference
			inspectorXmlAsset.CloneTree(_inspector);
			
			_addButton = _inspector.Q<Button>("addButton");
			_addButton.RegisterCallback<ClickEvent>((e) => OnAddButton());
			
			_removeButton = _inspector.Q<Button>("removeButton");
			_removeButton.RegisterCallback<ClickEvent>((e) => OnRemoveButton());
			_removeButton.SetEnabled(false);
			
			_saveButton = _inspector.Q<Button>("saveButton");
			_saveButton.RegisterCallback<ClickEvent>((e) => OnSaveButton());
			_saveButton.SetEnabled(false);
			
			_listView = _inspector.Q<ListView>();
			
			_items = serializedObject.FindProperty("items");
			
			_listView.BindProperty(_items);

			_listView.onSelectedIndicesChange += ints =>
			{
				_removeButton.SetEnabled(ints.Any());
				var index = ints.FirstOrDefault();
				Debug.Log($"Selected = [{index}]");
				_selectedIndex = index;
			};
			
			_initialHash = CalculateListHashFromItems(_items);
			Debug.Log($"Initial hash: {_initialHash}");
			return _inspector;
		}

		private int CalculateListHashFromItems(SerializedProperty items)
		{
			var j = 0;
			items.serializedObject.Update();
			
			for (int i = 0; i < items.arraySize; i++)
			{
				j += items.GetArrayElementAtIndex(i).FindPropertyRelative("id").intValue;
			}

			return j;
		}
		
		private void OnRemoveButton()
		{
			//_listView.RemoveFromSelection(_selectedIndex);
			_items.GetArrayElementAtIndex(_selectedIndex).DeleteCommand();
			_items.serializedObject.ApplyModifiedProperties();
			_listView.Rebuild();
		}

		private void OnSaveButton()
		{
			
			_saveButton.SetEnabled(false);
		}
		private void OnAddButton()
		{
			var library = target as VoiceLibrary;
			library.AddNewItem("Hello world from Gemelo.AI SDK", Gemelo.Voice.Configuration.DEFAULT_VOICE_ID);
			_items.serializedObject.Update();
			_saveButton.SetEnabled(true);
			_listView.Rebuild();
		}
	}
}