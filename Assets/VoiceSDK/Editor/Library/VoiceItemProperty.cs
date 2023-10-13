using Gemelo.Voice.Editor.Preview;
using Gemelo.Voice.Library;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Library
{
	[CustomPropertyDrawer(typeof(VoiceItem))]
	public class VoiceItemProperty : PropertyDrawer
	{

		private static VoicesDatabase _database;
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var instance = new VoiceItemPropertyInstance(property);
			
			// Create a new VisualElement to be the root the property UI
			instance.CreateWindow();
			instance.RegisterVisualElements(VoicesDatabase.Load());
			instance.UpdateState();
			// Return the finished UI
			return instance.Container;
		}
	}
}