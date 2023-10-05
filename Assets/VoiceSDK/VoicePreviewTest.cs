using System.Linq;
using UnityEngine;

namespace Gemelo.Voice.Editor.Preview
{
	public class VoicePreviewTest: MonoBehaviour
	{
		[SerializeField] private VoicePreview preview;

		private async void Awake()
		{
			var response = await VoicesDatabase.GetVoicesResponse();
			preview = new VoicePreview(response.Data.First());
		}
	}
}