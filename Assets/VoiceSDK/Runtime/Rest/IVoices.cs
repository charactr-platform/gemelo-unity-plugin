using System.Threading.Tasks;
using Gemelo.VoiceSDK.Rest.Model;
using UnityEngine;

namespace Gemelo.VoiceSDK.Rest
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