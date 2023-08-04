using System.Threading.Tasks;
using Gemelo.Voice.Rest.Model;
using UnityEngine;

namespace Gemelo.Voice.Rest
{
	public interface IConvert
	{
		Task<AudioClip> ConvertToAudioClip(ConvertRequest convertRequest);
	}

	public interface IVoices
	{
		Task<VoicesResponse> GetVoices();
		Task<AudioClip> GetVoicePreview(int id);
	}
}