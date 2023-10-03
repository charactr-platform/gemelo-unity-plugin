using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace Gemelo.Voice.Editor.Preview
{
	public class VoicePreviewInstance
	{
		public VisualElement Container { get; private set; }
		private PopupWindow _popup;
		private Button _playButton;
		public VoicePreviewInstance(SerializedProperty property)
		{
			_popup = CreateWindow();
			RegisterElements(property);
		}

		private void RegisterElements(SerializedProperty property)
		{
			_playButton = new Button
			{
				text = "Play",
				style = { width = 200, height = 30f,}
			};
         _playButton.Add(new Label("Play preview: "));
			_popup.Add(_playButton);
		}
		
		private PopupWindow CreateWindow()
		{
			Container = new VisualElement();
			
			var noneStyle = new StyleColor(StyleKeyword.None);

			var popupWindow = new PopupWindow
			{
				text = $"Preview for voice",
				style =
				{
					backgroundColor = noneStyle,
					borderBottomColor = noneStyle,
					borderTopColor = noneStyle,
					borderLeftColor = noneStyle,
					borderRightColor = noneStyle,
				}
			};
			
			Container.Add(popupWindow);
		
			return popupWindow;
		}
	}
}