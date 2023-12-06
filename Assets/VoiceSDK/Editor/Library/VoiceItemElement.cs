using System;
using System.Globalization;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Editor.Configuration;
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
		private SerializedProperty TextField { get; set; }
		private SerializedProperty VoiceField { get; set; }
		private SerializedProperty AudioClipField { get; set; }
		private SerializedProperty VoicePreview { get; set; }
		
		private SerializedProperty Timestamp { get; set; }
		private IntegerField IdField { get; set; }
		private Label Label { get; set; }
		private ItemState State { get; set; }

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
			TextField = property.FindPropertyRelative("text");
			VoiceField = property.FindPropertyRelative("voiceId");
			AudioClipField = property.FindPropertyRelative("audioClip");
			VoicePreview = property.FindPropertyRelative("voicePreview");
			Timestamp = property.FindPropertyRelative("timestamp");
			// Create a new VisualElement to be the root the property UI
			CreateInspector();
			RegisterVisualElements();
			IdField.value = CalculateCurrentHash();
			UpdateState();
		}
		
		public override string ToString() => _lastHash.ToString();

		private int CalculateCurrentHash()
		{
			var listHash = Timestamp.longValue.GetHashCode() + 
									TextField.stringValue.GetHashCode() +
									VoiceField.intValue.GetHashCode();

			int hash = CrcHelper.CRC16(listHash);
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

			if (!_database.PreviewExists(VoiceField.intValue, out _))
				return ItemState.NotSet;
			
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

			var field = this.Q<PropertyField>();
		
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

			this.Q<Label>().text = $"Voice item state: {State}";
			
			if (State != ItemState.UpToDate)
				RefreshVoicePreview();
		}
		
		private void RegisterVisualElements()
		{
			IdField = this.Q<IntegerField>("idField");
			var voiceField = this.Q<IntegerField>();
			var textField = this.Q<TextField>();
			
			var idProperty = Property.FindPropertyRelative("id");
			IdField.BindProperty(idProperty);
			
			IdField.isReadOnly = true;
			CopyIdButton = this.Q<Button>("copyButton");
			CopyIdButton.Add(new Label("Copy ID"));
			CopyIdButton.clicked += ()=> EditorGUIUtility.systemCopyBuffer = ToString();
			
			textField.BindProperty(TextField);
			voiceField.BindProperty(VoiceField);
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
			var id = VoiceField.intValue;
			var voiceField = this.Q<IntegerField>("voiceField");
			
			var buttonId = "selectVoiceButton";
			
			if (!_database.GetVoicePreviewById(id, out var preview))
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
			button.AddToClassList(".round");
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
			
			if (library.GetItemByTimestamp(Timestamp.longValue, out var voiceItem))
				voiceItem.SetVoicePreview(preview);
			else
				Debug.LogError($"Can't find item with hash: {_lastHash}");

			var element = integerField.Q<PropertyField>(id);

			if (element != null)
			{
				element.BindProperty(VoicePreview);
				return;
			}
		
			var voicePreviewField = new PropertyField(VoicePreview)
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
			
			voicePreviewField.BindProperty(VoicePreview);
			
			integerField.Add(voicePreviewField);
		}
	}
}