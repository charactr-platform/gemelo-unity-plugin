using System.Runtime.InteropServices;
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
		private SerializedProperty _property;
		private int _id;
		public VoicePreviewElement()
		{
			_container = LoadTreeAsset().Instantiate();
			hierarchy.Add(_container);
			_database = GetDatabase();
			RegisterCallback<ClickEvent>(e => {Debug.Log("Item click!"); });
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
			var id = item.FindPropertyRelative("Id").intValue;
			
			//Recycling view 
			if (_property != null)
			{
				_playButton.UnregisterCallback<ClickEvent>(OnPlayEvent);
			}

			_id = id;
			_property = property;

			var audio = _property.FindPropertyRelative("audioDetails");
			
			CreateLabel(item);
			CreateDetailsLabel(item, audio);
			_playButton = CreatePlayButton();
		}
		
		private Label CreateLabel(SerializedProperty previewItem)
		{
			var label = this.Q<Label>("nameLabel");
			var labelsValue = string.Empty;
			var name = previewItem.FindPropertyRelative("Name");
			var labels = previewItem.FindPropertyRelative("Labels");
			
			if (labels.isArray && labels.arraySize > 0)
				labelsValue = $"({labels.GetArrayElementAtIndex(0).stringValue})";
			
			label.text = $"{name.stringValue} "+labelsValue;
			
			return label;
		}
		
		private void CreateDetailsLabel(SerializedProperty previewItem, SerializedProperty audioDetails)
		{
			var label = this.Q<Button>("detailsButton");
			var rating = previewItem.FindPropertyRelative("Rating");
			var bits = audioDetails.FindPropertyRelative("BitRate");
			var hz = audioDetails.FindPropertyRelative("SampleRate");
		
			label.text = $"Rating: {rating.floatValue}, Audio: {bits.intValue} bit, {(hz.intValue/1000f):F1} kHz";
		}
		
		private Button CreatePlayButton()
		{
			var button = this.Q<Button>("playButton");
			button.RegisterCallback<ClickEvent>(OnPlayEvent);
			return button;
		}

		private void OnPlayEvent(ClickEvent evt) =>
			OnPlayEvent(_id);

		private async void OnPlayEvent(int id)
		{
			if(!_database.GetVoicePreviewById(id, out var instance))
			{
				Debug.LogError($"Can't Get Voice Preview with Id = [{id}]");
				return;
			}
			
			Debug.Log($"Playing voice [{id}]: {instance}");
			
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