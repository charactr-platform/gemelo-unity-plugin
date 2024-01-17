using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gemelo.Voice.Rest.Model;
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
		private static DatabaseInspector _instance;
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
			_instance = this;
			return _inspector;
		}

		private void UpdateStatusLabel()
		{
			var items = LoadPreviewItems();
			var sum = items.Sum(s => s.Size);
			var names = items.GroupBy(s => s.ItemData.Type, e => e.ItemData.Name).ToList();
			_statusLabel.text = $"Cache size [{items.Count}] ({sum / 1024f / 1024:F2}MB)\n";
			var s =  names.FirstOrDefault(f => f.Key == VoiceType.System)?.ToArray();
			var c =  names.FirstOrDefault(f => f.Key == VoiceType.Cloned)?.ToArray();
			var empty = new string[] {"(no items found)"};
			_statusLabel.text += $"\nSystem:\n {string.Join(" | ", s ?? empty)} \n"+
			                     $"\nCloned:\n {string.Join(" | ", c ?? empty)} \n";

			
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

		private List<VoicePreviewSerializedProperty> LoadPreviewItems()
		{
			var fields = new List<VoicePreviewSerializedProperty>();
			
			for (int i = 0; i < _voices.arraySize; i++)
			{
				var element = new VoicePreviewSerializedProperty(_voices.GetArrayElementAtIndex(i));
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
			var message = "Start update operation on all items ?";
			
			if (!EditorUtility.DisplayDialog("Start update", message , "YES", "CANCEL")) return;
			
			await UpdateLibrary(target as VoicesDatabase);
		}

		public static async Task UpdateLibraryInstance(VoicesDatabase database)
		{
			Selection.objects = null;
			Selection.SetActiveObjectWithContext(database,null);

			await Task.Delay(1000);
			
			if (_instance == null)
				throw new Exception("Can't load database inspector instance");
			
			await _instance.UpdateLibrary(database);
		}

		
		private async Task UpdateLibrary(VoicesDatabase database)
		{
			_updateProgress = new ProgressUpdater(ShowProgress);
			EditorApplication.update += OnUpdate;
			await database.UpdatePreviewsDatabase(_updateProgress);
			EditorApplication.update -= OnUpdate;
			_updateProgress = null;
			EditorUtility.ClearProgressBar();

			if (serializedObject.UpdateIfRequiredOrScript())
			{
				EditorUtility.SetDirty(database);
				AssetDatabase.SaveAssetIfDirty(database);
				Debug.Log("Saved database changes"); 
				Selection.SetActiveObjectWithContext(target, database);
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