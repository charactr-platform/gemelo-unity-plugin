using Charactr.VoiceSDK.Library;
using UnityEditor;
using UnityEngine.UIElements;

namespace Charactr.VoiceSDK.Editor.Library
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
			var updateButton = _inspector.Q<Button>("updateButton");
			updateButton.RegisterCallback<ClickEvent>((e) => OnUpdateButton());
			// Return the finished inspector UI
			return _inspector;
		}
		
		private void OnUpdateButton()
		{
			var library = target as VoiceLibrary;
			
			if (library.IsEmpty)
			{
				EditorUtility.DisplayDialog("Nothing to do...",
					"Please add new items to library (Text, and VoiceId) to downlaod audio clips", "OK");
				return;
			}
			
			if (EditorUtility.DisplayDialog("Start update", "Start update operation on all items ?", "YES", "CANCEL"))
			{
				library.ConvertTextsToAudioClips();
			}
		}
	}
}