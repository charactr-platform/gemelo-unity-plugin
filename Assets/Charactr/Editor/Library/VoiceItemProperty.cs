using System.Collections;
using System.Collections.Generic;
using Charactr.SDK.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VoiceItem = Charactr.SDK.Library.VoiceItem;

namespace Charactr.Editor.Library
{
    [CustomPropertyDrawer(typeof(VoiceItem))]
    public class VoiceItemProperty : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a new VisualElement to be the root the property UI
            var container = new VisualElement();

            var noneStyle = new StyleColor(StyleKeyword.None);
            
            var audioClip = property.FindPropertyRelative("audioClip");
            
            var popup = new UnityEngine.UIElements.PopupWindow
            {
                text = $"Voice item details ",
                style =
                {
                    backgroundColor = noneStyle,
                    borderBottomColor = noneStyle,
                    borderTopColor = noneStyle,
                    borderLeftColor = noneStyle,
                    borderRightColor = noneStyle,
                }
            };

            var textField = new PropertyField(property.FindPropertyRelative("text"), "Text to voice");
            var voiceField = new PropertyField(property.FindPropertyRelative("voiceId"), "Selected Voice");
            var audioField = new PropertyField(audioClip, "Audio");
            
            var playButton = new Button(() => PlayAudioClip(audioClip.objectReferenceValue))
            {
                text = "Play",
            };
            
            popup.Add(textField);
            popup.Add(voiceField);
            popup.Add(audioField);
            popup.Add(playButton);
            
            container.Add(popup);

            // Return the finished UI
            return container;
        }

        private void PlayAudioClip(Object objectReferenceValue)
        {
            if (objectReferenceValue is AudioClip audioclip)
            {
                Debug.Log($"Playing : {audioclip.name}");
            }
        }
    }
}