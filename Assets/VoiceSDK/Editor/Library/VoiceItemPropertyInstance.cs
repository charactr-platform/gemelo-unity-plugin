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
			Download,
		}
		
		public VisualElement Container { get; private set; }
		public PopupWindow PopupWindow { get; private set; }
		public Button Button { get; set; }
		public SerializedProperty Property { get; }
		public SerializedProperty TextField { get; }
		public SerializedProperty VoiceField { get; }
		public SerializedProperty AudioClipField { get; }
		public ItemState State { get; set; }
		public int Hash { get; private set; }
		public SerializedProperty Id { get; }

		private int _lastHash;

		public VoiceItemPropertyInstance(SerializedProperty property)
		{
			Property = property;
			TextField = property.FindPropertyRelative("text");
			VoiceField = property.FindPropertyRelative("voiceId");
			AudioClipField = property.FindPropertyRelative("audioClip");
			Id = property.FindPropertyRelative("id");
			Hash = property.GetHashCode();
		}
		public override string ToString() => _lastHash.ToString();

		private int CalculateCurrentHash()
		{
			return Mathf.Abs(TextField.stringValue.GetHashCode() + VoiceField.intValue);
		}

		private ItemState CheckForState()
		{
			AudioClip audioClip = null;

			GetAudioClipInstance(out audioClip);
			
			var currentHash = CalculateCurrentHash();

			if (audioClip == null)
				return ItemState.NeedsUpdate;

			if (currentHash.ToString() != audioClip.name)
				return ItemState.NeedsUpdate;

			return ItemState.UpToDate;
		}

		private bool GetAudioClipInstance(out AudioClip audioClip)
		{
			if (AudioClipField.objectReferenceValue is AudioClip clip)
			{
				audioClip = clip;
				return true;
			}

			audioClip = null;
			return false;
		}
		
		private async void DownloadAudioClip()
		{
			if (GetAudioClipInstance(out var audioClip))
				RemoveOldClip(audioClip);

			var field = PopupWindow.Q<PropertyField>();
			field.visible = false;
			
			State = ItemState.Download;
			SetButtonFunctionFromState();

			if (Property.serializedObject.targetObject is VoiceLibrary library)
			{
				await library.AddAudioClip(CalculateCurrentHash());
				
				GetAudioClipInstance(out var newClip);
				SetClipHashData(newClip);
				AudioClipField.serializedObject.Update();
				UpdateState();
				field.visible = true;
			}
			else
				throw new Exception("Target object not set, or is not VoiceLibrary!");
		}
		
		private void SetButtonFunctionFromState()
		{
			var buttonLabel = Button.Q<Label>();
			buttonLabel.RemoveFromClassList("warningIcon");
			buttonLabel.RemoveFromClassList("cloudIcon");
			buttonLabel.RemoveFromClassList("playIcon");
			
			switch (State)
			{
				case ItemState.UpToDate:
					if (GetAudioClipInstance(out var clip))
					{
						buttonLabel.text = $"Play (duration {clip.length.ToString(CultureInfo.InvariantCulture)}s)";
						buttonLabel.AddToClassList("playIcon");
						AssignButtonOnClick(PlayAudioClip);
					}
					else
						Debug.LogError("Can't find audio clip!");
					
					break;
				
				case ItemState.NeedsUpdate:
					buttonLabel.text = "Update audio clip";
					buttonLabel.AddToClassList("warningIcon");
					AssignButtonOnClick(DownloadAudioClip);
					break;
				
				case ItemState.Download:
					buttonLabel.text = "Downloading...";
					buttonLabel.AddToClassList("cloudIcon");
					AssignButtonOnClick(() => { Debug.Log("Download in progress"); });
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
			if (!GetAudioClipInstance(out var clip))
			{
				Debug.LogError("Can't play missing audio clip!");
				return;
			}

			Debug.Log($"Playing:{clip.name}");
			AudioPlayer.PlayClipStatic(clip);
			EditorApplication.RepaintProjectWindow();
		}

		private void SetClipHashData(AudioClip clip)
		{
			var path = AssetDatabase.GetAssetPath(clip);

			if (string.IsNullOrEmpty(path))
				return;
			
			var importer = AssetImporter.GetAtPath(path);
			importer.userData = Hash.ToString();
			importer.SaveAndReimport();
		}
		private void RemoveOldClip(AudioClip clip)
		{
			var path = AssetDatabase.GetAssetPath(clip);

			if (string.IsNullOrEmpty(path))
				return;
			
			var importer = AssetImporter.GetAtPath(path);
			if (importer.userData.Equals(Hash.ToString()))
			{
				AssetDatabase.DeleteAsset(path);
				Debug.Log($"Removed old asset : {path}");
			}
			
			State = ItemState.NeedsUpdate;
			UpdateState();
		}

		public void UpdateState()
		{
			//Ignore initial updates from Create...
			if (State == ItemState.None)
				return;

			var newFieldsHash = CalculateCurrentHash();

			State = CheckForState();
			
			SetButtonFunctionFromState();

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
			textField.RegisterValueChangedCallback((s) => UpdateState());
			voiceField.RegisterValueChangedCallback((s) => UpdateState());

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

			Button = new Button();
		}
	}
}