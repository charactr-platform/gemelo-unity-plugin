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

namespace Gemelo.Voice.Editor.Library
{
	internal class VoiceItemElement: VisualElement
	{
		public enum ItemState
		{
			None,
			Initialized,
			UpToDate,
			NeedsUpdate,
			Download,
			NotSet
		}
		
		private Button UpdateButton { get; set; }
		private Button CopyIdButton { get; set; }
		private SerializedProperty Property { get; set; }
		private SerializedProperty TextProperty { get; set; }
		private SerializedProperty VoiceIdProperty { get; set; }
		private SerializedProperty AudioClipProperty { get; set; }
		private SerializedProperty VoicePreviewProperty { get; set; }
		private SerializedProperty IdProperty { get; set; }
		private SerializedProperty TimestampProperty { get; set; }
		private ItemState State { get; set; }

		private IntegerField _idField;
		private int _lastHash;
		private readonly VoicesDatabase _database;
		
		public VoiceItemElement()
		{
			_database = VoicesDatabase.Load();
		}

		private void CreateInspector()
		{
			var assetPath = AssetDatabase.GUIDToAssetPath("591c5c1ab96e74cfd9c11b6f1d26aec0");
			var treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
			treeAsset.CloneTree(this);
		}
		
		public void RegisterElement(SerializedProperty property)
		{
			Property = property;
			IdProperty = property.FindPropertyRelative("id");
			TextProperty = property.FindPropertyRelative("text");
			VoiceIdProperty = property.FindPropertyRelative("voiceId");
			AudioClipProperty = property.FindPropertyRelative("audioClip");
			VoicePreviewProperty = property.FindPropertyRelative("voicePreview");
			TimestampProperty = property.FindPropertyRelative("timestamp");
			
			// Create a new VisualElement to be the root the property UI
			CreateInspector();
			RegisterVisualElements();
			UpdateState();
			IdProperty.intValue = _lastHash;
			Property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}
		
		public override string ToString() => _lastHash.ToString();

		private int CalculateCurrentHash()
		{
			var listHash = TimestampProperty.longValue.GetHashCode() + 
									TextProperty.stringValue.GetHashCode() +
									VoiceIdProperty.intValue.GetHashCode();

			int hash = CrcHelper.CRC16(listHash);
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

			if (!_database.PreviewExists(VoiceIdProperty.intValue, out _))
				return ItemState.NotSet;
			
			return ItemState.UpToDate;
		}

		private bool GetAudioClipInstance(out AudioClip audioClip)
		{
			if (AudioClipProperty.objectReferenceValue is AudioClip clip)
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

			var field = this.Q<PropertyField>();
		
			field.visible = false;
			
			State = ItemState.Download;
			SetButtonFunctionFromState();

			if (TargetLibrary != null)
			{
				await TargetLibrary.AddAudioClip(CalculateCurrentHash());
				AudioClipProperty.serializedObject.Update();
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
				case ItemState.NotSet:
						UpdateButton.SetEnabled(false);
					break;
				
				case ItemState.UpToDate:
					
					if (GetAudioClipInstance(out var clip))
					{
						UpdateButton.SetEnabled(true);
						buttonLabel.text = $"Play (duration {clip.length.ToString(CultureInfo.InvariantCulture)}s)";
						buttonLabel.AddToClassList("playIcon");
						AssignButtonOnClick(PlayAudioClip);
					}
					else
						Debug.LogError("Can't find audio clip!");
					
					break;
				
				case ItemState.NeedsUpdate:
					UpdateButton.SetEnabled(true);
					buttonLabel.text = "Update audio clip";
					buttonLabel.AddToClassList("warningIcon");
					AssignButtonOnClick(DownloadAudioClip);
					break;
				
				case ItemState.Download:
					UpdateButton.SetEnabled(false);
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

		private void UpdateState()
		{
			//Ignore initial updates from Create...
			if (State == ItemState.None)
				return;

			var newFieldsHash = CalculateCurrentHash();

			State = CheckForState();
			
			SetButtonFunctionFromState();

			_lastHash = newFieldsHash;

			//_idField.value = _lastHash;
			IdProperty.intValue = _lastHash;
			this.Q<Label>().text = $"Voice item state: {State}";

			if (State != ItemState.UpToDate)
			{
				RefreshVoicePreview();
			}
		}
		
		private void RegisterVisualElements()
		{
			var voiceField = this.Q<IntegerField>("voiceIdField");
			var textField = this.Q<TextField>("inputTextField");
			var audioField = this.Q<ObjectField>("audioClipField");
			_idField = this.Q<IntegerField>("idField");

			audioField.BindProperty(AudioClipProperty);
			textField.BindProperty(TextProperty);
			voiceField.BindProperty(VoiceIdProperty);
			_idField.BindProperty(IdProperty);

			CopyIdButton = this.Q<Button>("copyButton");
			CopyIdButton.clicked += ()=> EditorGUIUtility.systemCopyBuffer = ToString();
			
			UpdateButton = this.Q<Button>("updateButton");
			UpdateButton.Add(new Label("Control button"));

			_lastHash = CalculateCurrentHash();
			textField.RegisterValueChangedCallback((s) => UpdateState());
			voiceField.RegisterValueChangedCallback((s) => UpdateState());
			
			RefreshVoicePreview();
			State = ItemState.Initialized;
		}
		
		private void RefreshVoicePreview()
		{
			var voiceId = VoiceIdProperty.intValue;
			var voiceField = this.Q<IntegerField>("voiceIdField");
			
			var buttonId = "selectVoiceButton";
			
			if (!_database.GetVoicePreviewById(voiceId, out var preview))
			{
				//No item found, default to "Select"
				SetVoiceFieldSelectButton(voiceField, buttonId);
			}
			else
			{
				var buttonElement = voiceField.Q<Button>(buttonId);
				if (buttonElement != null)
					voiceField.Remove(buttonElement);
				
				SetVoiceFieldPreviewElement(voiceField, preview);
			}
		}

		private void DisableVoiceFieldInt(IntegerField integerField)
		{
			integerField[0].style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);
			var intField = integerField[1];
			intField.style.flexGrow = 0;
			intField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
			intField.SetEnabled(false);
		}

		private void SetVoiceFieldSelectButton(IntegerField integerField, string id)
		{
			DisableVoiceFieldInt(integerField);
			
			var buttonElement = integerField.Q<Button>(id);
			if (buttonElement != null)
				return;
			
			var button = new Button
			{
				text = "[Select voice]",
				name = id,
			};
			
			button.AddToClassList(".rounded");
			button.RegisterCallback<ClickEvent>((e) =>
			{
				DatabaseListView.ShowSelectionWindow(_lastHash, TargetLibrary);
			});
			integerField.Add(button);
		}
		
		private void SetVoiceFieldPreviewElement(IntegerField integerField, VoicePreview preview)
		{
			DisableVoiceFieldInt(integerField);

			var id = "voicePreviewElement";
			
			var library = Property.serializedObject.targetObject as VoiceLibrary;
			
			if (library.GetItemByTimestamp(TimestampProperty.longValue, out var voiceItem))
				voiceItem.SetVoicePreview(preview);
			else
				Debug.LogError($"Can't find item with hash: {_lastHash}");

			var element = integerField.Q<PropertyField>(id);

			if (element != null)
			{
				element.BindProperty(VoicePreviewProperty);
				return;
			}
		
			var voicePreviewField = new PropertyField(VoicePreviewProperty)
			{
				name = id,
				style =
				{
					alignSelf = new StyleEnum<Align>(Align.Stretch),
					flexBasis = new StyleLength(StyleKeyword.Auto),
					flexGrow = 1,
					maxHeight =  integerField.style.maxHeight
				}
			};
			
			voicePreviewField.BindProperty(VoicePreviewProperty);
			
			integerField.Add(voicePreviewField);
		}
	}
}