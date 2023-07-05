using System;
using System.Globalization;
using Charactr.VoiceSDK.Audio;
using Charactr.VoiceSDK.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace Charactr.VoiceSDK.Editor.Library
{
	internal class VoiceItemPropertyInstance
	{
		public enum ItemState
		{
			None,
			Initialized,
			UpToDate,
			NeedsUpdate,
			Updating,
		}
		
		public VisualElement Container { get; set; }
		public PopupWindow PopupWindow { get; private set; }
		public Button Button { get; set; }
		public SerializedProperty Property { get; set; }
		public SerializedProperty TextField { get; set; }
		public SerializedProperty VoiceField { get; set; }
		public SerializedProperty AudioClipField { get; set; }
		public ItemState State { get; set; }

		private int _lastHash;
		public override string ToString() => _lastHash.ToString();

		private int CalculateCurrentHash()
		{
			return Mathf.Abs(TextField.stringValue.GetHashCode() + VoiceField.intValue);
		}

		private ItemState CheckForState()
		{
			var clip = AudioClipField.objectReferenceValue as AudioClip;
			var currentHash = CalculateCurrentHash();

			if (clip == null)
				return ItemState.NeedsUpdate;

			if (currentHash != _lastHash)
				return ItemState.NeedsUpdate;

			return ItemState.UpToDate;
		}

		private void UpdateControlButtonStateToUpToDate()
		{
			var buttonLabel = Button.Q<Label>();

			var clip = AudioClipField.objectReferenceValue as AudioClip;

			buttonLabel.text = $"Play (duration {clip.length.ToString(CultureInfo.InvariantCulture)}s)";
			AssignButtonOnClick(PlayAudioClip);

			buttonLabel.AddToClassList("playIcon");
			State = ItemState.UpToDate;
		}

		private async void DownloadAudioClip()
		{
			var clip = AudioClipField.objectReferenceValue as AudioClip;
			if (clip != null) RemoveOldClip(clip);
			
			State = ItemState.Updating;
			SetButtonLabelFromState();
			
			var voiceLibrary = Property.serializedObject.targetObject as VoiceLibrary;
			await voiceLibrary.AddAudioClip(CalculateCurrentHash());
			
			UpdateControlsState();
		}

		private void UpdateControlButtonStateToNeedsUpdate()
		{
			var newFieldsHash = CalculateCurrentHash();

			AssignButtonOnClick(DownloadAudioClip);

			var fieldsUpdateOccured = _lastHash != newFieldsHash;
			State = fieldsUpdateOccured ? ItemState.NeedsUpdate : ItemState.Updating;
		}

		private void SetButtonLabelFromState()
		{
			var buttonLabel = Button.Q<Label>();
			buttonLabel.RemoveFromClassList("warningIcon");
			buttonLabel.RemoveFromClassList("cloudIcon");
			buttonLabel.RemoveFromClassList("playIcon");
			
			switch (State)
			{
				case ItemState.UpToDate:
					buttonLabel.text = "Play audio";
					buttonLabel.AddToClassList("playIcon");
					break;
				case ItemState.NeedsUpdate:
					buttonLabel.text = "Update audio clip";
					buttonLabel.AddToClassList("warningIcon");
					break;
				case ItemState.Updating:
					buttonLabel.text = "Downloading";
					buttonLabel.AddToClassList("cloudIcon");
					break;
			}
		}

		private void AssignButtonOnClick(Action onClick)
		{
			Button.clickable.clicked -= DownloadAudioClip;
			Button.clickable.clicked -= PlayAudioClip;
			Button.clickable.clicked += onClick;
		}

		private void PlayAudioClip()
		{
			var clip = AudioClipField.objectReferenceValue as AudioClip;
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
				State = ItemState.NeedsUpdate;
				UpdateControlsState();
			}
		}

		public void UpdateControlsState()
		{
			//Ignore initial updates from Create...
			if (State == ItemState.None)
				return;

			var newFieldsHash = CalculateCurrentHash();

			State = CheckForState();

			switch (State)
			{
				case ItemState.NeedsUpdate:
					UpdateControlButtonStateToNeedsUpdate();
					break;
				case ItemState.UpToDate:
					UpdateControlButtonStateToUpToDate();
					break;
			}
			
			SetButtonLabelFromState();

			_lastHash = newFieldsHash;

			PopupWindow.text = $"Voice item details [{_lastHash}] [{State}]";
		}

		public void RegisterVisualElements()
		{
			var textField = new TextField("Text to voice", 500, true, false, ' ');
			textField.BindProperty(TextField);
			PopupWindow.Add(textField);

			var voiceField = new IntegerField("Selected Voice Id");
			voiceField.BindProperty(VoiceField);

			PopupWindow.Add(voiceField);

			var audioField = new PropertyField(AudioClipField, "Downloaded AudioClip");
			PopupWindow.Add(audioField);

			Button.Add(new Label("Control button"));
			PopupWindow.Add(Button);

			Container.Add(PopupWindow);

			_lastHash = CalculateCurrentHash();
			textField.RegisterValueChangedCallback((s) => UpdateControlsState());
			voiceField.RegisterValueChangedCallback((s) => UpdateControlsState());

			State = ItemState.Initialized;
		}

		public void CreateWindow()
		{
			Container = new VisualElement();

			var noneStyle = new StyleColor(StyleKeyword.None);

			PopupWindow = new PopupWindow
			{
				text = $"Voice item details [hash][state]",
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
		}
	}
}