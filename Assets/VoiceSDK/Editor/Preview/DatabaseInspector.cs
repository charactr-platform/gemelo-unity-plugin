
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gemelo.Voice.Editor.Preview;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace Charactr.VoiceSDK.Editor.Preview
{
	
	[CustomEditor(typeof(VoicesDatabase))]
	public partial class DatabaseInspector : UnityEditor.Editor
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
			_listView = _inspector.Q<ListView>();
			_updateButton.RegisterCallback<ClickEvent>((e) => OnUpdateButton());
			_purgeButton.RegisterCallback<ClickEvent>(e=> OnPurgeButton());

			var list = LoadPreviewItems();
			_listView = CreateList(list);
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