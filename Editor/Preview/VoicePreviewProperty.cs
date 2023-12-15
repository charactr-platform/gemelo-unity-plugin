using UnityEditor;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Preview
{
   [CustomPropertyDrawer(typeof(VoicePreview))]
   public class VoicePreviewProperty : PropertyDrawer
   {
      public override VisualElement CreatePropertyGUI(SerializedProperty property) => 
         VoicePreviewElement.Create(property);
   }
}