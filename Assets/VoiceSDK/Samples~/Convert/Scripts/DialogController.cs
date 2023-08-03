using UnityEngine;

namespace Gemelo.VoiceSDK.Samples.Convert
{
	public class DialogController : MonoBehaviour
	{
		[SerializeField] private PlayerDialog playerOne;

		[SerializeField] private PlayerDialog playerTwo;

		// Start is called before the first frame update
		void Start()
		{
			playerOne.OnVoiceItemPlayed += OnVoiceItemPlayed;
			playerTwo.OnVoiceItemPlayed += OnVoiceItemPlayed;
			playerOne.PlayNextSequence();
		}

		private void OnVoiceItemPlayed(int id)
		{
			//Play next dialog item as response to previous player
			if (playerOne.ContainsVoiceItemId(id))
				playerTwo.PlayNextSequence();
			
			if (playerTwo.ContainsVoiceItemId(id))
				playerOne.PlayNextSequence();
		}
	}
}