using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Charactr.SDK.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
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
            
            audioField.RegisterValueChangeCallback((s)=>UpdatePlayButtons(property, popup));
            popup.Add(textField);
            popup.Add(voiceField);
            popup.Add(audioField);
            container.Add(popup);
            
            // Return the finished UI
            return container;
        }

        private void UpdatePlayButtons(SerializedProperty property, VisualElement container)
        {
            var play = "playButton";
            var get = "getButton";
            
            var audioClip = property.FindPropertyRelative("audioClip");
            var audioClipPresent = audioClip.objectReferenceValue != null;

            if (container.Q<Button>(get) != null && audioClipPresent)
                container.Remove(container.Q<Button>(get ));
            
            if (container.Q<Button>(play) != null && audioClipPresent == false)
                container.Remove(container.Q<Button>(play));

            if (audioClip.objectReferenceValue is AudioClip clip)
            {
                var playButton = new Button(() => PlayAudioClip(clip))
                {
                    text = $"Play (duration {clip.length.ToString(CultureInfo.InvariantCulture)}s)",
                    name = play
                };
                container.Add(playButton);
            }
            else
            {
                var getButton = new Button(() => DownloadClip(property))
                {
                    text = "Download audio clip",
                    name = get 
                };
                container.Add(getButton);
            }
        }
        private async void DownloadClip(SerializedProperty property)
        {
            if (Selection.activeObject is VoiceLibrary voiceLibrary)
            {
                var hashId = CalculateCurrentHash(property);
                await voiceLibrary.AddAudioClip(hashId);
            }
        }
        
        private int CalculateCurrentHash(SerializedProperty property)
        {
            var textProperty = property.FindPropertyRelative("text");
            var voiceId = property.FindPropertyRelative("voiceId");
            return Mathf.Abs(textProperty.stringValue.GetHashCode() + voiceId.intValue);
        }
        
        private void PlayAudioClip(AudioClip audioclip)
        {
            Debug.Log($"Playing : {audioclip.name}");
            EditorAudioPlayer.PlayClip(audioclip);
        }
        
    }
}