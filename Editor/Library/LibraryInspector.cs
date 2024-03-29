using System.Collections.Generic;
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
			
			_listView = LoadSerializedDataToList();
			
			_listView.onSelectedIndicesChange += SetSelectionIndex;
			
			return _inspector;
		}

		private void SetSelectionIndex(IEnumerable<int> ints)
		{
			var enumerable = ints as int[] ?? ints.ToArray();
			var selectionFound = enumerable.Any();
			_removeButton.SetEnabled(selectionFound);

			if (!selectionFound)
				return;

			var index = enumerable.FirstOrDefault();
			_selectedIndex = index;
		}

		private ListView LoadSerializedDataToList()
		{
			var list = _inspector.Q<ListView>();

			serializedObject.UpdateIfRequiredOrScript();
			
			_items = serializedObject.FindProperty("items");
			
			list.BindProperty(_items);

			_initialHash = CalculateListHashFromItems(_items);
			Debug.Log($"Initial hash: {_initialHash}");
			return list;
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
			if (_selectedIndex < 0)
				return;
			
			Debug.Log($"Deleting item = [{_selectedIndex}]");
			_items.GetArrayElementAtIndex(_selectedIndex).DeleteCommand();
			_items.serializedObject.ApplyModifiedProperties();
			_listView.ClearSelection();
			_selectedIndex = -1;
			_listView = LoadSerializedDataToList();
			_listView.RefreshItems();
		}

		private void OnSaveButton()
		{
			_saveButton.SetEnabled(false);
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
			SaveLibrary(target as VoiceLibrary);
		}

		public static void SaveLibrary(VoiceLibrary library)
		{
			EditorUtility.SetDirty(library);
			AssetDatabase.SaveAssetIfDirty(library);
		}
		
		private void OnAddButton()
		{
			var library = target as VoiceLibrary;
			library.AddNewItem("Hello world from Gemelo.AI SDK", 0);
			
			Repaint();
			
			if (CalculateListHashFromItems(_items) != _initialHash)
			{
				_saveButton.SetEnabled(true);
			}
		}
	}
}