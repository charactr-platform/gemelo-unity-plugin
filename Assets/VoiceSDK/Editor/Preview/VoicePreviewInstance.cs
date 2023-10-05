using Gemelo.Voice.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Preview
{
	public class VoicePreviewInstance
	{
		public VisualElement Container { get;}
		private Button _playButton;
		public VoicePreviewInstance(SerializedProperty property, VisualTreeAsset visualTreeAsset)
		{
			Container = visualTreeAsset.Instantiate();
			RegisterElements(property);
		}
		
		private void RegisterElements(SerializedProperty property)
		{
			
			var item = property.FindPropertyRelative("itemData");
			var id = item.FindPropertyRelative("Id");
			CreateLabel(item);
			CreateDetailsLabel(item);
			_playButton = CreatePlayButton(item, id.intValue);
		}

		private Label CreateLabel(SerializedProperty previewItem)
		{
			var name = previewItem.FindPropertyRelative("Name");
			var id = previewItem.FindPropertyRelative("Id");
			
			var label = Container.Q<Label>("nameLabel");
			label.text = $"{name.stringValue} ID:({id.intValue})";
			
			return label;
		}
		
		private Label CreateDetailsLabel(SerializedProperty previewItem)
		{
			var rating = previewItem.FindPropertyRelative("Rating");
			var label = Container.Q<Label>("ratingLabel");
			label.text = $"Rating: {rating.floatValue}";
			return label;
		}
		private Button CreatePlayButton(SerializedProperty property, int id)
		{
			var button = Container.Q<Button>("playButton");
			button.text = "play";

			var database = property.serializedObject.targetObject as VoicesDatabase;
			button.RegisterCallback<ClickEvent>(e=> OnPlayEvent(database, id));
			return button;
		}

		private async void OnPlayEvent(VoicesDatabase database, int id)
		{
			var instance = database.GetVoicePreviewById(id);
			await AudioPlayer.PlayClipStatic(instance.GenerateAudioClip());
		}
	}
}