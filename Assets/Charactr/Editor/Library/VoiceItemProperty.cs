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
        private SerializedProperty _textField;
        private SerializedProperty _voiceId;
        private SerializedProperty _audioClip;
        private VoiceLibrary _target;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            _target = property.serializedObject.targetObject as VoiceLibrary;
            
            _textField = property.FindPropertyRelative("text");
            _voiceId = property.FindPropertyRelative("voiceId");
            _audioClip = property.FindPropertyRelative("audioClip");
            
            _lastHash = CalculateCurrentHash(property);
            _lastAudioClipHash = _audioClip.GetHashCode();
            
            // Create a new VisualElement to be the root the property UI
            var container = new VisualElement();
            
            var noneStyle = new StyleColor(StyleKeyword.None);
            
            var popup = new UnityEngine.UIElements.PopupWindow
            {
                text = $"Voice item details [{_lastHash}]",
                tooltip = "Click to copy item ID into clipboard...",
                style =
                {
                    backgroundColor = noneStyle,
                    borderBottomColor = noneStyle,
                    borderTopColor = noneStyle,
                    borderLeftColor = noneStyle,
                    borderRightColor = noneStyle,
                }
            };
            
            popup.RegisterCallback<ClickEvent>((e)=> EditorGUIUtility.systemCopyBuffer = _lastHash.ToString());
            
            var textField = new PropertyField(_textField, "Text to voice");
            var voiceField = new PropertyField(_voiceId, "Selected Voice Id");
            var audioField = new PropertyField(_audioClip, "Downloaded AudioClip");
            
        
            popup.Add(textField);
            popup.Add(voiceField);
            popup.Add(audioField);
            container.Add(popup);

            property.serializedObject.ApplyModifiedProperties();
            //Register update after values are set
            audioField.RegisterValueChangeCallback((s) => UpdatePlayButtons(property, popup));
            textField.RegisterValueChangeCallback((s)=> UpdatePlayButtons(property, popup));
            voiceField.RegisterValueChangeCallback((s)=> UpdatePlayButtons(property, popup));

            // Return the finished UI
            return container;
        }
        
        private void UpdatePlayButtons(SerializedProperty property, VisualElement container)
        {
            var play = "playButton";
            var get = "getButton";
            
            var newFieldsHash = CalculateCurrentHash(property);
            var newAudioHash = _audioClip.GetHashCode();
       
            var fieldsUpdateOccured = _lastHash != newFieldsHash;
            var audioClipUpdateOccured = _lastAudioClipHash != newAudioHash;

            //TODO: recycle buttons instead of removing them
            var getButton = container.Q<Button>(get);
            if (getButton != null)
                container.Remove(getButton);

            var playButton = container.Q<Button>(play);
            if (playButton != null)
                container.Remove(playButton);
            
            _audioClip = property.FindPropertyRelative("audioClip");
            
            if (_audioClip.objectReferenceValue is AudioClip clip && !fieldsUpdateOccured && audioClipUpdateOccured)
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
            await _target.AddAudioClip(CalculateCurrentHash(property));
            onClipReady.Invoke();
        }
        
        //TODO: We should use single static call for hash calculations in VoiceItem
        private int CalculateCurrentHash(SerializedProperty property)
        {
            _textField = property.FindPropertyRelative("text");
            _voiceId = property.FindPropertyRelative("voiceId");
            return Mathf.Abs(_textField.stringValue.GetHashCode() + _voiceId.intValue);
        }
        
        private void PlayAudioClip(AudioClip clip)
        {
            Debug.Log($"Playing:{clip.name}");
            EditorAudioPlayer.PlayClip(clip);
            EditorApplication.RepaintProjectWindow();
        }
        
    }
}