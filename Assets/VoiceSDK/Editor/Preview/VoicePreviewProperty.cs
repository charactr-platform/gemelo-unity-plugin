using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Preview
{
   [CustomPropertyDrawer(typeof(VoicePreview))]
   public class VoicePreviewProperty : PropertyDrawer
   {
      private VisualTreeAsset VisualTreeAsset =>
         _visualTreeAsset ??= LoadTreeAsset();

      private static VisualTreeAsset _visualTreeAsset;
      
      public override VisualElement CreatePropertyGUI(SerializedProperty property)
      {
         var instance = new VoicePreviewInstance(property, VisualTreeAsset);
         return instance.Container;
      }

      private VisualTreeAsset LoadTreeAsset()
      {
         var guid = "a37506bebdde24a87a150e49634741c5";
         var assetPath = AssetDatabase.GUIDToAssetPath(guid);
         return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
      }
   }
}