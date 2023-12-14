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

			_lastHash = IdProperty.intValue = CalculateCurrentHash();
			Property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			
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
				style =
				{
					alignSelf = new StyleEnum<Align>(Align.Stretch),
					flexBasis = new StyleLength(StyleKeyword.Auto),
					flexGrow = 1,
					maxHeight = integerField.style.maxHeight
				}
			};
			
			button.AddToClassList("rounded-nobg");
			button.RegisterCallback<ClickEvent>((e) =>
			{
				DatabaseListView.ShowSelectionWindow(TimestampProperty.longValue, TargetLibrary);
			});
			
			integerField.Add(button);
		}
		
		private void SetVoiceFieldPreviewElement(IntegerField integerField, VoicePreview preview)
		{
			DisableVoiceFieldInt(integerField);

			var id = "voicePreviewElement";
			
			VoiceIdProperty.intValue = preview.Id;
			
			SetVoicePreviewForItemByTimestamp(Property.serializedObject, TimestampProperty.longValue, preview);
			
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
			
			UpdateState();
		}
		
		public static void SetVoicePreviewForItemByTimestamp(SerializedObject serializedObject, long timestamp, VoicePreview voicePreview)
		{
			var items = serializedObject.FindProperty("items");

			var index = -1L;

			for (int i = 0; i < items.arraySize; i++)
			{
				var voiceItem = items.GetArrayElementAtIndex(i);

				if (voiceItem.FindPropertyRelative("timestamp").longValue != timestamp)
					continue;
				
				index = i;
				SetPreviewForSerializedItem(voiceItem, voicePreview);
				break;
			}

			if (index < 0)
			{
				Debug.LogError($"Can't find item with timestamp or index = {timestamp}");
			}
			else
			{
				serializedObject.ApplyModifiedProperties();
				Debug.Log("Updated serialized object!");
			}
		}
		
		public static void SetVoicePreviewForItemById(SerializedObject serializedObject, int id, VoicePreview voicePreview)
		{
			var items = serializedObject.FindProperty("items");

			var index = -1;

			for (int i = 0; i < items.arraySize; i++)
			{
				var voiceItem = items.GetArrayElementAtIndex(i);

				if (voiceItem.FindPropertyRelative("id").intValue != id)
					continue;
				
				index = i;
				SetPreviewForSerializedItem(voiceItem, voicePreview);
				break;
			}

			if (index < 0)
			{
				Debug.LogError($"Can't find item with id = {id}");
			}
			else
			{
				serializedObject.ApplyModifiedProperties();
				Debug.Log("Updated serialized object!");
			}
		}
		
		/*
		 * Set public fields required by VoicePreviewElement in SerializedObject mode.
		  public struct PreviewItemData
			{
				public string Name;
				public int Id;
				public string PreviewUrl;
				public string Description;
				public string[] Labels;
				public float Rating;
			}
		*/
		
		private static void SetPreviewForSerializedItem(SerializedProperty property, VoicePreview preview)
		{
			property.FindPropertyRelative("voiceId").intValue = preview.Id;
			
			var p = property.FindPropertyRelative("voicePreview");
			var item = p.FindPropertyRelative("itemData");
			
			item.FindPropertyRelative("Id").intValue = preview.Id;
			item.FindPropertyRelative("Name").stringValue = preview.Name;
			item.FindPropertyRelative("Rating").floatValue = preview.Rating;
		
			FillDetailsLabel(item.FindPropertyRelative("Labels"), preview.Labels);
		}

		private static void FillDetailsLabel(SerializedProperty labelsProperty, string[] labelsList)
		{
			labelsProperty.ClearArray();
			
			for (int i = 0; i < labelsList.Length; i++)
			{
				labelsProperty.InsertArrayElementAtIndex(i);
				labelsProperty.GetArrayElementAtIndex(i).stringValue = labelsList[i];
			}
		}
	}
}