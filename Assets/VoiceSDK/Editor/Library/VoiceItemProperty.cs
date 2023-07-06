using UnityEditor;
using UnityEngine.UIElements;
using VoiceItem = Charactr.VoiceSDK.Library.VoiceItem;

namespace Charactr.VoiceSDK.Editor.Library
{
	[CustomPropertyDrawer(typeof(VoiceItem))]
	public class VoiceItemProperty : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var instance = new VoiceItemPropertyInstance(property);

			// Create a new VisualElement to be the root the property UI
			instance.CreateWindow();
			instance.RegisterVisualElements();
			instance.PopupWindow.RegisterCallback<ClickEvent>((e) =>
				EditorGUIUtility.systemCopyBuffer = instance.ToString());

			instance.UpdateState();
			// Return the finished UI
			return instance.Container;
		}
	}
}