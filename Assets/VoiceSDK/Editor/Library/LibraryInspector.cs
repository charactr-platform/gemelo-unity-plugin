using System.Collections.Generic;
using Gemelo.Voice.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Library
{
	
	[CustomEditor(typeof(VoiceLibrary))]
	public class LibraryInspector : UnityEditor.Editor
	{
		public VisualTreeAsset inspectorXmlAsset;

		private Button _updateButton, _addButton, _removeButton;
		private VisualElement _inspector;
		private VisualElement _selected;
		private ListView _listView;
		
		public override VisualElement CreateInspectorGUI()
		{
			// Create a new VisualElement to be the root of our inspector UI
			_inspector = new VisualElement();
		
			// Load from default reference
			inspectorXmlAsset.CloneTree(_inspector);
			_updateButton = _inspector.Q<Button>("updateButton");
			_updateButton.RegisterCallback<ClickEvent>((e) => OnUpdateButton());

			_addButton = _inspector.Q<Button>("addButton");
			_addButton.RegisterCallback<ClickEvent>((e) => OnAddButton());
			
			_removeButton = _inspector.Q<Button>("removeButton");
			_removeButton.RegisterCallback<ClickEvent>((e) => OnRemoveButton());
			
			_listView = _inspector.Q<ListView>();
			
			var items = serializedObject.FindProperty("items");

			var itemsList = new List<SerializedProperty>();
			
			for (int i = 0; i < items.arraySize; i++)
			{
				var property = items.GetArrayElementAtIndex(i);
				
				itemsList.Add(property);
			}

			//_listView.itemsSource = itemsList;
			_listView.BindProperty(items);

			// Return the finished inspector UI
			return _inspector;
		}

		private void OnRemoveButton()
		{
			throw new System.NotImplementedException();
		}

		private void OnAddButton()
		{
			var library = target as VoiceLibrary;
			library.AddNewItem("Hello world from Gemelo.AI SDK", Gemelo.Voice.Configuration.DEFAULT_VOICE_ID);
		}

		private async void OnUpdateButton()
		{
			var library = target as VoiceLibrary;
			
			if (library.IsEmpty)
			{
				EditorUtility.DisplayDialog("Nothing to do...",
					"Please add new items to library (Text, and VoiceId) to download audio clips", "OK");
				return;
			}
			
			if (EditorUtility.DisplayDialog("Start update", "Start update operation on all items ?", "YES", "CANCEL"))
			{
				Selection.objects = null;
				var count = library.Items.Count;
				EditorUtility.DisplayProgressBar($"Downloading...", $"Downloading items [{0}/{count}]", 0f);
				await library.ConvertTextsToAudioClips((i) =>
				{
					EditorUtility.DisplayProgressBar($"Downloading...", $"Downloading items [{i}/{count}]", (float)count/i);
				});
				Selection.SetActiveObjectWithContext(target, library);
				EditorUtility.ClearProgressBar();
			}
		}
		
	}
}