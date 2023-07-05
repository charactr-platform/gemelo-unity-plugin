using UnityEditor;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using VoiceItem = Charactr.VoiceSDK.Library.VoiceItem;

namespace Charactr.VoiceSDK.Editor.Library
{
	[CustomPropertyDrawer(typeof(VoiceItem))]
	public class VoiceItemProperty : PropertyDrawer
	{
		
		private SerializedProperty GetTextInstance(SerializedProperty property) => property.FindPropertyRelative("text");

		private SerializedProperty GetVoiceIdInstance(SerializedProperty property) =>
			property.FindPropertyRelative("voiceId");

		private SerializedProperty GetAudioClipInstance(SerializedProperty property) =>
			property.FindPropertyRelative("audioClip");

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var instance = new VoiceItemPropertyInstance()
			{
				Button = new Button(),
				Property = property,
				TextField = GetTextInstance(property),
				VoiceField = GetVoiceIdInstance(property),
				AudioClipField = GetAudioClipInstance(property)
			};

			// Create a new VisualElement to be the root the property UI
			instance.CreateWindow();
			instance.RegisterVisualElements();
			instance.PopupWindow.RegisterCallback<ClickEvent>((e) =>
				EditorGUIUtility.systemCopyBuffer = instance.ToString());

			instance.UpdateControlsState();
			// Return the finished UI
			return instance.Container;
		}
	}
}