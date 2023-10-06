using Gemelo.Voice.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Preview
{
	public class VoicePreviewElement: VisualElement
	{
		public override VisualElement contentContainer => _container;
		private readonly VisualElement _container;
		private static VoicesDatabase _database;
		private Button _playButton;
		public VoicePreviewElement()
		{
			_container = LoadTreeAsset().Instantiate();
			hierarchy.Add(_container);
			_database = GetDatabase();
		}
		public static VoicePreviewElement Create(SerializedProperty property)
		{
			var element = new VoicePreviewElement();
			element.RegisterProperty(property);
			return element;
		}
		
		public void RegisterProperty(SerializedProperty property)
		{
			var item = property.FindPropertyRelative("itemData");
			CreateLabel(item);
			CreateDetailsLabel(item);
			_playButton = CreatePlayButton(item);
		}

		private Label CreateLabel(SerializedProperty previewItem)
		{
			var label = this.Q<Label>("nameLabel");
			
			var name = previewItem.FindPropertyRelative("Name");
			var id = previewItem.FindPropertyRelative("Id");
			
			label.text = $"{name.stringValue} ID:({id.intValue})";
			
			return label;
		}
		
		private Label CreateDetailsLabel(SerializedProperty previewItem)
		{
			var label = this.Q<Label>("ratingLabel");
			var rating = previewItem.FindPropertyRelative("Rating");
			label.text = $"Rating: {rating.floatValue}";
			return label;
		}
		private Button CreatePlayButton(SerializedProperty previewItem)
		{
			var button = this.Q<Button>("playButton");
			var id = previewItem.FindPropertyRelative("Id");
			button.RegisterCallback<ClickEvent>(e=> OnPlayEvent(id.intValue));
			return button;
		}

		private async void OnPlayEvent(int id)
		{
			var instance = _database.GetVoicePreviewById(id);
			Debug.Log($"Playing voice: {instance}");
			
			_playButton.SetEnabled(false);
			await AudioPlayer.PlayClipStatic(instance.GenerateAudioClip());
			_playButton.SetEnabled(true);
		}
		
		private static VisualTreeAsset LoadTreeAsset()
		{
			var guid = "a37506bebdde24a87a150e49634741c5";
			var assetPath = AssetDatabase.GUIDToAssetPath(guid);
			return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
		}

		private static VoicesDatabase GetDatabase()
		{
			return _database == null ? VoicesDatabase.Load() : _database;
		}
	}
}