
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gemelo.Voice.Editor.Library;
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
			_listView = _inspector.Q<TemplateContainer>("databaseListView").Q<ListView>();
			_updateButton.RegisterCallback<ClickEvent>((e) => OnUpdateButton());
			_purgeButton.RegisterCallback<ClickEvent>(e=> OnPurgeButton());

			
			//DatabaseListView.CreateList(_listView, LoadPreviewItems());
			return _inspector;
		}
		
		private void OnPurgeButton()
		{
			if (EditorUtility.DisplayDialog($"Are you sure?", $"Do You really want to purge cache of voice previews",
				    "YES", "NO"))
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
				Debug.Log("Added element "+ i + " "+ element.displayName);
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
			
			if (!EditorUtility.DisplayDialog("Start update",message , "YES", "CANCEL"))
				return;
			
			_updateProgress = new ProgressUpdater(ShowProgress);
			EditorApplication.update += OnUpdate;
			await library.UpdatePreviewsDatabase(_updateProgress);
			EditorApplication.update -= OnUpdate;
			_updateProgress = null;
			
			Selection.SetActiveObjectWithContext(target, library);
			EditorUtility.ClearProgressBar();
		}

		private void ShowProgress(float p)
		{
			var i = Mathf.RoundToInt(p * 100);
			EditorUtility.DisplayProgressBar("Processing...",$"Downloading voice previews...{i}%", p);
		}
		
	}
}