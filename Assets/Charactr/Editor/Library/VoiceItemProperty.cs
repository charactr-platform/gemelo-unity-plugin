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
        private int _lastHash;
        private int _lastAudioClipHash;
        private VisualElement _popup;
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
            
            audioField.RegisterValueChangeCallback((s)=> UpdatePlayButtons(property, popup));
            textField.RegisterValueChangeCallback((s)=> UpdatePlayButtons(property, popup));
            voiceField.RegisterValueChangeCallback((s)=> UpdatePlayButtons(property, popup));
            
            popup.Add(textField);
            popup.Add(voiceField);
            popup.Add(audioField);
            container.Add(popup);
            
            _lastHash = CalculateCurrentHash(property);
            _lastAudioClipHash = audioClip.GetHashCode();
            _popup = popup;
            // Return the finished UI
            return container;
        }

        private void UpdatePlayButtons(SerializedProperty property, VisualElement container)
        {
            var play = "playButton";
            var get = "getButton";
            
            var audioClip = property.FindPropertyRelative("audioClip");
            
            var newFieldsHash = CalculateCurrentHash(property);
            var newAudioHash = audioClip.GetHashCode();
       
            var fieldsUpdateOccured = _lastHash != newFieldsHash;
            var audioClipUpdateOccured = _lastAudioClipHash != newAudioHash;

            //TODO: recycle buttons instead of removing them
            var getButton = container.Q<Button>(get);
            if (getButton != null)
                container.Remove(getButton);

            var playButton = container.Q<Button>(play);
            if (playButton != null)
                container.Remove(playButton);

            if (audioClip.objectReferenceValue is AudioClip clip && !fieldsUpdateOccured && audioClipUpdateOccured)
            {
                playButton = new Button(() => PlayAudioClip(clip))
                {
                    text = $"Play (duration {clip.length.ToString(CultureInfo.InvariantCulture)}s)",
                    name = play
                };
                container.Add(playButton);
            }
            else
            {
                getButton = new Button(() =>
                {
                    //TODO: Remove old audio clip as we get a new one
                    getButton.text = "Downloading...";
                    getButton.clickable = null;
                    
                    DownloadClip(property, () => UpdatePlayButtons(property, container));
                })
                {
                    text = fieldsUpdateOccured ? "Update audio clip":"Download audio clip", 
                    name = get 
                };
                
                container.Add(getButton);
            }

            _lastHash = newFieldsHash;
            _lastAudioClipHash = newAudioHash;
        }
        private async void DownloadClip(SerializedProperty property, Action onClipReady)
        {
            if (Selection.activeObject is VoiceLibrary voiceLibrary)
            {
                var hashId = CalculateCurrentHash(property);
                await voiceLibrary.AddAudioClip(hashId);
                onClipReady.Invoke();
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