using System.Globalization;
using Charactr.VoiceSDK.Audio;
using Charactr.VoiceSDK.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using PopupWindow = UnityEngine.UIElements.PopupWindow;
using VoiceItem = Charactr.VoiceSDK.Library.VoiceItem;

namespace Charactr.VoiceSDK.Editor.Library
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
     
        private ItemState _itemState;
        private enum ItemState
        {
            None, 
            Init,
            Play,
            Update,
            Download,
        }
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            _target = property.serializedObject.targetObject as VoiceLibrary;
            
            _lastHash = CalculateCurrentHash(property);
            _lastAudioClipHash = _audioClip.GetHashCode();
            
            // Create a new VisualElement to be the root the property UI
            var container = new VisualElement();
            
            var noneStyle = new StyleColor(StyleKeyword.None);
            
            var popup = new PopupWindow
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
            
            popup.RegisterCallback<ClickEvent>((e)=> EditorGUIUtility.systemCopyBuffer = CalculateCurrentHash(property).ToString());
            
            var textField = new PropertyField(_textField, "Text to voice");
            var voiceField = new PropertyField(_voiceId, "Selected Voice Id");
            var audioField = new PropertyField(_audioClip, "Downloaded AudioClip");
            popup.Add(textField);
            popup.Add(voiceField);
            popup.Add(audioField);
            container.Add(popup);
            
            //Register update after values are set
            textField.RegisterValueChangeCallback((s) => UpdateControlsState(property, popup));
            voiceField.RegisterValueChangeCallback((s) => UpdateControlsState(property, popup));

            _itemState = ItemState.Init;
            UpdateControlsState(property, popup);
            // Return the finished UI
            return container;
        }
        
        private void UpdateControlsState(SerializedProperty property, PopupWindow popupWindow)
        {
            //Ignore initial updates from Create...
            if (_itemState == ItemState.None)
                return;
            
            var controlButton = new Button();
            var buttonLabel = new Label();
            
            var newFieldsHash = CalculateCurrentHash(property);
            var newAudioHash = _audioClip.GetHashCode();
            
            var fieldsUpdateOccured = _lastHash != newFieldsHash;
            var audioClipUpdateOccured = _lastAudioClipHash != newAudioHash;
            
            //TODO: recycle buttons instead of removing them
            var oldControlButton = popupWindow.Q<Button>();
            if (oldControlButton  != null)
                popupWindow.Remove(oldControlButton);
            
            var clip = _audioClip.objectReferenceValue as AudioClip;
            
            if (clip != null && (!fieldsUpdateOccured || _itemState == ItemState.Download) && audioClipUpdateOccured)
            {
                buttonLabel.text = $"Play (duration {clip.length.ToString(CultureInfo.InvariantCulture)}s)";
                controlButton.clicked += () => PlayAudioClip(clip);
                buttonLabel.AddToClassList("playIcon");
                _itemState = ItemState.Play;
                _audioClip.serializedObject.Update();
            }
            else
            {
                controlButton.clicked += async () =>
                {
                    if (clip != null) RemoveOldClip(clip);
                    buttonLabel.text = "Downloading...";
                    buttonLabel.AddToClassList("cloudIcon");
                    await _target.AddAudioClip(newFieldsHash);
                    UpdateControlsState(property, popupWindow);
                };

                _itemState = fieldsUpdateOccured ? ItemState.Update : ItemState.Download;
                buttonLabel.text = fieldsUpdateOccured ? "Update audio clip" : "Download audio clip";
                buttonLabel.AddToClassList("warningIcon");
            }
            
            controlButton.Add(buttonLabel);
            popupWindow.Add(controlButton);
            
            _lastHash = newFieldsHash;
            _lastAudioClipHash = newAudioHash;
            
            popupWindow.text = $"Voice item details [{_lastHash}]";
        }
        
        //TODO: We should use single static call for hash calculations in VoiceItem
        private int CalculateCurrentHash(SerializedProperty property)
        {
            _textField = property.FindPropertyRelative("text");
            _voiceId = property.FindPropertyRelative("voiceId");
            _audioClip = property.FindPropertyRelative("audioClip");
            return Mathf.Abs(_textField.stringValue.GetHashCode() + _voiceId.intValue);
        }
        
        private void PlayAudioClip(AudioClip clip)
        {
            Debug.Log($"Playing:{clip.name}");
            AudioPlayer.PlayClipStatic(clip);
            EditorApplication.RepaintProjectWindow();
        }

        private void RemoveOldClip(AudioClip clip)
        {
            var path = AssetDatabase.GetAssetPath(clip);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Removed old asset : {path}");
            }
        }
        
    }
}