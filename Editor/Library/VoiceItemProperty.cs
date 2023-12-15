using Gemelo.Voice.Library;
using UnityEditor;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Library
{
	[CustomPropertyDrawer(typeof(VoiceItem))]
	public class VoiceItemProperty : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var instance = new VoiceItemElement();
			instance.RegisterElement(property);
			// Return the finished UI
			return instance;
		}
	}
}