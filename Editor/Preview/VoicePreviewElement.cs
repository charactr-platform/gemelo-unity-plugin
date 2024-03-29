using System;
using System.Collections.Generic;
using System.Linq;
using Gemelo.Voice.Audio;
using Gemelo.Voice.Editor.Library;
using Gemelo.Voice.Rest.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace Gemelo.Voice.Editor.Preview
{
	public enum ListType
	{
		None,
		Change,
		Creation,
		Selection,
	}
	
	public class VoicePreviewElement: VisualElement
	{
		public override VisualElement contentContainer => _viewRoot;
	
		private readonly VisualElement _viewRoot;
		private Button _playButton, _changeButton, _detailsButton, _selectButton;
		private SerializedProperty _property;
		private PopupWindow _detailsPopup;
		private int _id;
		private VoiceType _voiceType;
		private readonly ListType _listType;
		private Action<int> _onSelect;
		
		public VoicePreviewElement(ListType listType): this()
		{
			_listType = listType;
		}

		private VoicePreviewElement()
		{
			_viewRoot = LoadTreeAsset().Instantiate();
			hierarchy.Add(_viewRoot);
		}
		
		public static VoicePreviewElement Create(SerializedProperty property)
		{
			var element = new VoicePreviewElement(ListType.Change);
			element.RegisterProperty(property);
			return element;
		}

		public void RegisterOnSelect(Action<int> onSelect)
		{
			_onSelect = onSelect;
		}
		
		public void RegisterProperty(SerializedProperty property)
		{
			var item = property.FindPropertyRelative("itemData");
			var id = item.FindPropertyRelative("Id").intValue;
			var voiceType = (VoiceType) item.FindPropertyRelative("Type").enumValueIndex;
			//Recycling view 
			if (_property != null)
			{
				_playButton.UnregisterCallback<ClickEvent>(OnPlayEvent);
				_changeButton.UnregisterCallback<ClickEvent>(OnChangeEvent);
				_detailsButton.UnregisterCallback<ClickEvent>(OnDetailsEvent);
				_selectButton.UnregisterCallback<ClickEvent>(OnSelectEvent);
			}
			
			_id = id;
			_property = property;
			_voiceType = voiceType;
			
			CreateNameLabel(item);
			
			switch (_voiceType)
			{
				case VoiceType.System:
					CreateRatingLabel(item);
					CreateSystemLabels(item);
					break;
				
				case VoiceType.Cloned:
					CreateClonedLabels();
					break;
			}
			
			_selectButton = RegisterSelectButton();
			_detailsButton = RegisterDetailsButton();
			_changeButton = RegisterChangeButton();
			_playButton = RegisterPlayButton();
			_detailsPopup = CreateDetailsPopup(item, _voiceType == VoiceType.System);
		}

		private void CreateNameLabel(SerializedProperty previewItem)
		{
			var label = this.Q<Label>("nameLabel");
			var itemName = previewItem.FindPropertyRelative("Name");
			label.text = $"{itemName.stringValue}";
		}
		
		private void CreateRatingLabel(SerializedProperty previewItem)
		{
			var rate = this.Q<Label>("rateLabel");
			rate.text = $"{previewItem.FindPropertyRelative("Rating").floatValue:F1}";
		}

		private void CreateSystemLabels(SerializedProperty previewItem)
		{
			var labelsValue = string.Empty;
			
			var labels = previewItem.FindPropertyRelative("Labels");
			
			if (labels.isArray && labels.arraySize > 0)
				labelsValue = $"({labels.GetArrayElementAtIndex(0).stringValue})";

			this.Q<Label>("nameLabel").text += $"{labelsValue}";
		}
		
		private void CreateClonedLabels()
		{
			var rate = this.Q<Label>("rateLabel");
			var rateImage = this.Q<Label>("rateImage");
			rate.text = "Cloned";
			rateImage.RemoveFromClassList("starIcon");
			rateImage.AddToClassList("clonedIcon");
		}
		
		private PopupWindow CreateDetailsPopup(SerializedProperty previewItem, bool isSystemPreview)
		{
			var labelString = $"ID: {_id}\nType: {_voiceType}\n";

			if (isSystemPreview)
			{
				labelString += $"Labels: {FillDetailsLabel(previewItem)}";
			}
			
			var label = new Label
			{
				style = 
				{
					flexBasis = new StyleLength(StyleKeyword.Auto),
					flexGrow = 1,
					flexWrap = new StyleEnum<Wrap>(Wrap.Wrap)
				},
				text = labelString
			};
			
			_detailsPopup = CreatePopup();
			_detailsPopup.Add(label);
			return _detailsPopup;
		}

		private string FillDetailsLabel(SerializedProperty previewItem)
		{
			var labels = previewItem.FindPropertyRelative("Labels");
			
			var labelsList = new List<string>();

			if (labels.arraySize <= 1)
				return string.Empty;
			
			for (int i = 1; i < labels.arraySize; i++)
			{
				var value = labels.GetArrayElementAtIndex(i).stringValue;
				if (i % 6 == 0)
					value = value.Insert(0,"\n");
				
				labelsList.Add(value);
			}

			return string.Join(", ", labelsList);
		}

		private PopupWindow CreatePopup()
		{
			var popup = new PopupWindow
			{
				text = $"Details:",
				style =
				{
					position = new StyleEnum<Position>(Position.Relative),
					flexBasis = new StyleLength(StyleKeyword.Auto),
					flexGrow = 1,
					borderTopLeftRadius = 0,
					borderTopRightRadius = 0
				}
			};
			popup.AddToClassList("rounded-nobg");
			return popup;
		}

		private Button RegisterPlayButton()
		{
			var button = this.Q<Button>("playButton");
			button.RegisterCallback<ClickEvent>(OnPlayEvent);
			return button;
		}

		private Button RegisterSelectButton()
		{
			var button = this.Q<Button>("selectButton");
			button.RegisterCallback<ClickEvent>(OnSelectEvent);
			
			if (_listType != ListType.Selection && _listType != ListType.Creation)
			{
				button.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
			}
			
			return button;
		}

		private Button RegisterDetailsButton()
		{
			var button = this.Q<Button>("detailsButton");
			button.RegisterCallback<ClickEvent>(OnDetailsEvent);
			return button;
		}

		private Button RegisterChangeButton()
		{
			var button = this.Q<Button>("changeButton");
			button.RegisterCallback<ClickEvent>(OnChangeEvent);
			
			if (_listType != ListType.Change)
			{
				button.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
			}
			
			return button;
		}

		private void OnSelectEvent(ClickEvent evt) => _onSelect?.Invoke(_id);
		
		private void OnPlayEvent(ClickEvent evt) => OnPlayEvent(_id);

		private void OnChangeEvent(ClickEvent evt) => DatabaseListView.ShowSelectionWindow(_property);
		
		private void OnDetailsEvent(ClickEvent evt)
		{
			if (!_viewRoot.hierarchy.Children().Contains(_detailsPopup))
				_viewRoot.hierarchy.Add(_detailsPopup);
			else
				_viewRoot.hierarchy.Remove(_detailsPopup);
		}
		
		private async void OnPlayEvent(int id)
		{
			if (!GetDatabase().GetVoicePreviewById(id, out var instance))
			{
				Debug.LogError($"Can't Get Voice Preview with Id = [{id}]");
				return;
			}
			
			Debug.Log($"Playing voice [{id}]: {instance}");
			
			_playButton.SetEnabled(false);
			await AudioPlayerInstance.PlayClipStatic(instance.GenerateAudioClip());
			_playButton.SetEnabled(true);
		}
		
		private static VisualTreeAsset LoadTreeAsset()
		{
			var assetPath = AssetDatabase.GUIDToAssetPath("a37506bebdde24a87a150e49634741c5");
			return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
		}

		private static VoicesDatabase GetDatabase() => VoicesDatabase.Load();
	}
}