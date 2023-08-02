using System;
using System.Globalization;
using gemelo.VoiceSDK.Audio;
using gemelo.VoiceSDK.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace gemelo.VoiceSDK.Editor.Library
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
		public Button UpdateButton { get; set; }
		public Button CopyIdButton { get; set; }
		public SerializedProperty Property { get; }
		public SerializedProperty TextField { get; }
		public SerializedProperty VoiceField { get; }
		public SerializedProperty AudioClipField { get; }
		public IntegerField IdField { get; private set; }
		public Label Label { get; private set; }
		public ItemState State { get; set; }
		public int Hash { get; private set; }
		
		private int _lastHash;

		public VoiceItemPropertyInstance(SerializedProperty property)
		{
			Property = property;
			TextField = property.FindPropertyRelative("text");
			VoiceField = property.FindPropertyRelative("voiceId");
			AudioClipField = property.FindPropertyRelative("audioClip");
			Hash = property.GetHashCode();
		}
		public override string ToString() => _lastHash.ToString();

		private int CalculateCurrentHash()
		{
			var hash = Mathf.Abs(TextField.stringValue.GetHashCode() + VoiceField.intValue);
			IdField.value = hash;
			return hash;
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
			if (GetAudioClipInstance(out _))
			{
				State = ItemState.NeedsUpdate;
				UpdateState();
			};

			var field = PopupWindow.Q<PropertyField>();
		
			field.visible = false;
			
			State = ItemState.Download;
			SetButtonFunctionFromState();

			if (Property.serializedObject.targetObject is VoiceLibrary library)
			{
				await library.AddAudioClip(CalculateCurrentHash());
				AudioClipField.serializedObject.Update();
				UpdateState();
				field.visible = true;
			}
			else
				throw new Exception("Target object not set, or is not VoiceLibrary!");
		}
		
		private void SetButtonFunctionFromState()
		{
			var buttonLabel = UpdateButton.Q<Label>();
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
			UpdateButton.clickable.clicked -= DownloadAudioClip;
			UpdateButton.clickable.clicked -= PlayAudioClip;
			UpdateButton.clickable.clicked += onClick;
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
		

		public void UpdateState()
		{
			//Ignore initial updates from Create...
			if (State == ItemState.None)
				return;

			var newFieldsHash = CalculateCurrentHash();

			State = CheckForState();
			
			SetButtonFunctionFromState();

			_lastHash = newFieldsHash;

			PopupWindow.text = $"Voice item state: {State}";
		}

		public void RegisterVisualElements()
		{
			Label = new Label();
			IdField = new IntegerField("ID");
			IdField.isReadOnly = true;
			
			CopyIdButton.Add(new Label("Copy ID"));
			CopyIdButton.clicked += ()=> EditorGUIUtility.systemCopyBuffer = ToString();
			IdField.Add(CopyIdButton);
			Label.Add(IdField);
			
			PopupWindow.Add(Label);
			
			var textField = new TextField("Text to voice", 500, true, false, ' ');
			textField.BindProperty(TextField);

			PopupWindow.Add(textField);

			var voiceField = new IntegerField("Selected Voice Id");
			voiceField.BindProperty(VoiceField);
			PopupWindow.Add(voiceField);
			
			var audioField = new PropertyField(AudioClipField, "AudioClip");
			PopupWindow.Add(audioField);
			
			UpdateButton.Add(new Label("Control button"));
			PopupWindow.Add(UpdateButton);
			
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
				style =
				{
					backgroundColor = noneStyle,
					borderBottomColor = noneStyle,
					borderTopColor = noneStyle,
					borderLeftColor = noneStyle,
					borderRightColor = noneStyle,
				}
			};

			UpdateButton = new Button();
			CopyIdButton = new Button();
		}
	}
}