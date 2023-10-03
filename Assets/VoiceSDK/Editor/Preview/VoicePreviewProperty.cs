using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Preview
{
   [CustomPropertyDrawer(typeof(VoicePreview))]
   public class VoicePreviewProperty : PropertyDrawer
   {
      public override VisualElement CreatePropertyGUI(SerializedProperty property)
      {
         var instance = new VoicePreviewInstance(property);
         return instance.Container;
      }
   }
}