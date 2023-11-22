using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Gemelo.Voice.Editor.Preview
{
	[CustomEditor(typeof(VoicesDatabase))]
	public class DatabaseInspector : UnityEditor.Editor
	{
		public VisualTreeAsset visualTreeAsset;
		private Button _updateButton, _purgeButton;
		private VisualElement _inspector;
		private Label _statusLabel;
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
			_statusLabel = _inspector.Q<Label>("statusLabel");
			
			_updateButton.RegisterCallback<ClickEvent>((e) => OnUpdateButton());
			_purgeButton.RegisterCallback<ClickEvent>(e=> OnPurgeButton());
			
			UpdateStatusLabel();
			
			return _inspector;
		}

		private void UpdateStatusLabel()
		{
			var items = LoadPreviewItems();
			var sum = items.Sum(s => s.FindPropertyRelative("previewDataSize").intValue);
			var names = items.Select(s => s.FindPropertyRelative("itemData").FindPropertyRelative("Name").stringValue);
			_statusLabel.text = $"Added previews: {items.Count}, Cache size: {sum / 1024f / 1024:F2}MB";
			_statusLabel.text += $"\n\n {string.Join(" | ",names)} \n";
		}
		
		private void OnPurgeButton()
		{
			if (EditorUtility.DisplayDialog($"Are you sure?",
				    $"Do You really want to purge cache of voice previews", "YES", "NO"))
			{
				VoicesDatabase.Clean();
				Selection.objects = null;
			}
		}
		
		private List<SerializedProperty> LoadPreviewItems()
		{
			var fields = new List<SerializedProperty>();
			
			for (int i = 0; i < _voices.arraySize; i++)
			{
				var element = _voices.GetArrayElementAtIndex(i);
				fields.Add(element);
			}
			
			return fields;
		}

		private ProgressUpdater _updateProgress;

		private void OnUpdate()
		{
			if (_updateProgress != null)
			{
				ShowProgress(_updateProgress.Value);
			}
		}
		
		private async void OnUpdateButton()
		{
			var library = target as VoicesDatabase;

			var message = "Start update operation on all items ?";
			
			if (!EditorUtility.DisplayDialog("Start update", message , "YES", "CANCEL")) return;
			
			_updateProgress = new ProgressUpdater(ShowProgress);
			EditorApplication.update += OnUpdate;
			await library.UpdatePreviewsDatabase(_updateProgress);
			EditorApplication.update -= OnUpdate;
			_updateProgress = null;
			EditorUtility.ClearProgressBar();

			if (serializedObject.UpdateIfRequiredOrScript())
			{
				EditorUtility.SetDirty(library);
				AssetDatabase.SaveAssetIfDirty(library);
				Debug.Log("Saved database changes"); 
				Selection.SetActiveObjectWithContext(target, library);
			}
			else
			{
				Debug.LogError("Error, changes not saved!");
			}
		}

		private void ShowProgress(float p)
		{
			var i = Mathf.RoundToInt(p * 100);
			EditorUtility.DisplayProgressBar("Processing...",$"Downloading voice previews...{i}%", p);
		}
	}
}