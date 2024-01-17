using Gemelo.Voice.Rest.Model;
using UnityEditor;

namespace Gemelo.Voice.Editor.Preview
{
	public class VoicePreviewSerializedProperty
	{
		public int Size => _property.FindPropertyRelative("previewDataSize").intValue;
		public PreviewItemData ItemData => ItemDataFromProperty();
		
		private readonly SerializedProperty _property;
		public VoicePreviewSerializedProperty(SerializedProperty property)
		{
			_property = property;
		}

		private PreviewItemData ItemDataFromProperty()
		{
			var itemData = _property.FindPropertyRelative("itemData");
			
			return new PreviewItemData()
			{
				Id = itemData.FindPropertyRelative(nameof(PreviewItemData.Id)).intValue,
				Type = (VoiceType) itemData.FindPropertyRelative(nameof(PreviewItemData.Type)).enumValueIndex,
				Name = itemData.FindPropertyRelative(nameof(PreviewItemData.Name)).stringValue,
				Rating = itemData.FindPropertyRelative(nameof(PreviewItemData.Rating)).floatValue,
				Description = itemData.FindPropertyRelative(nameof(PreviewItemData.Description)).stringValue,
				PreviewUrl = itemData.FindPropertyRelative(nameof(PreviewItemData.PreviewUrl)).stringValue
			};
		}
	}
}