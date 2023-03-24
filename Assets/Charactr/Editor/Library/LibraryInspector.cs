using System.Collections;
using System.Collections.Generic;
using Charactr.SDK.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Charactr.SDK.Editor.Library
{
	[CustomEditor(typeof(VoiceLibrary))]
	public class LibraryInspector : UnityEditor.Editor
	{
		public VisualTreeAsset inspectorXmlAsset;
		private Button _updateButton;
		
		private VisualElement _inspector;
		public override VisualElement CreateInspectorGUI()
		{
			// Create a new VisualElement to be the root of our inspector UI
			_inspector = new VisualElement();
			// Load from default reference
			inspectorXmlAsset.CloneTree(_inspector);
			
			_inspector.Q<Button>("createButton").RegisterCallback<ClickEvent>((e) => OnCreateButton());
			_inspector.Q<Button>("loadButton").RegisterCallback<ClickEvent>((e) => OnLoadButton());
			_inspector.Q<Button>("updateButton").RegisterCallback<ClickEvent>((e) => OnUpdateButton());
			// Return the finished inspector UI
			return _inspector;
		}
		
		private void OnUpdateButton()
		{
			if (EditorUtility.DisplayDialog("Start update", "Start update operation on all items ?", "YES", "CANCEL"))
			{
				var library = target as VoiceLibrary;
				library.ConvertTextsToAudioClips();
			}
		}

		private void OnLoadButton()
		{
			throw new System.NotImplementedException();
		}

		private void OnCreateButton()
		{
			throw new System.NotImplementedException();
		}
	}
}