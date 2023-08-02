using System.Threading.Tasks;
using gemelo.VoiceSDK.Rest.Model;
using UnityEngine;

namespace gemelo.VoiceSDK.Rest
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