
using Gemelo.Voice.Editor.Preview;
using Gemelo.Voice.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Charactr.VoiceSDK.Editor.Preview
{
	[CustomEditor(typeof(VoicesDatabase))]
	public class DatabaseInspector : UnityEditor.Editor
	{
		public VisualTreeAsset visualTreeAsset;
		private Button _updateButton;
		private VisualElement _inspector;
		private ListView _listView;
	
		public override VisualElement CreateInspectorGUI()
		{
			// Create a new VisualElement to be the root of our inspector UI
			_inspector = new VisualElement();
		
			// Load from default reference
			visualTreeAsset.CloneTree(_inspector);
			_updateButton = _inspector.Q<Button>("updateButton");
			_listView = _inspector.Q<ListView>();
			_updateButton.RegisterCallback<ClickEvent>((e) => OnUpdateButton());
			// Return the finished inspector UI
			UpdateView();
			
			return _inspector;
		}

		private void UpdateView()
		{
			var database = target as VoicesDatabase;
			var count = 0;
			var so = new SerializedObject(database);
			var property = so.FindProperty("voices");

			for (int i = 0; i < property.arraySize; i++)
			{
				var prop = property.GetArrayElementAtIndex(i);
				var element = new PropertyField(prop, "Test");

				_listView.hierarchy.Add(element);
				Debug.Log("Added element "+ i + " "+ element.bindingPath);
			}
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