using System;
using System.Globalization;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Editor.Preview;
using Gemelo.Voice.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace Gemelo.Voice.Editor.Library
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
		public SerializedProperty VoicePreview { get; }
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
			VoicePreview = property.FindPropertyRelative("voicePreview");
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

			if (TargetLibrary != null)
			{
				await TargetLibrary.AddAudioClip(CalculateCurrentHash());
				AudioClipField.serializedObject.Update();
				UpdateState();
				field.visible = true;
			}
			else
				throw new Exception("Target object not set, or is not VoiceLibrary!");
		}

		private VoiceLibrary TargetLibrary => (Property.serializedObject.targetObject as VoiceLibrary);
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

		public void RegisterVisualElements(VoicesDatabase database)
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
			
			var id = VoiceField.intValue;

			if (!database.GetVoicePreviewById(id, out var preview))
			{
				Debug.LogError($"Cant add preview item [{id}]");
				OverrideSelectVoicePreview(voiceField);
			}
			else 
				OverrideVoiceIdWithPreview(voiceField, preview);
		}

		private void OverrideWithIntField(IntegerField integerField)
		{
			integerField[0].style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);
			var intField = integerField[1];
			intField.style.flexGrow = 0;
			intField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
			intField.SetEnabled(false);
		}

		private void OverrideSelectVoicePreview(IntegerField integerField)
		{
			OverrideWithIntField(integerField);
			
			var button = new Button
			{
				text = "[Select voice]"
			};
			button.AddToClassList(".round");
			button.RegisterCallback<ClickEvent>((e) =>
			{
				DatabaseListView.ShowChangeWindow(_lastHash, TargetLibrary);
			});
			integerField.Add(button);
		}


		private void OverrideVoiceIdWithPreview(IntegerField integerField, VoicePreview preview)
		{
			OverrideWithIntField(integerField);
			
			var library = Property.serializedObject.targetObject as VoiceLibrary;
			library.GetItemById(_lastHash, out var voiceItem);
			voiceItem.SetVoicePreview(preview);
			
			var voicePreviewField = new PropertyField(VoicePreview)
			{
				style =
				{
					alignSelf = new StyleEnum<Align>(Align.Stretch),
					flexBasis = new StyleLength(StyleKeyword.Auto),
					flexGrow = 1,
					maxHeight =  integerField.style.maxHeight
				}
			};
			
			integerField.Add(voicePreviewField);
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