using System.Threading.Tasks;
using Charactr.VoiceSDK.Model;
using UnityEngine;

namespace Charactr.VoiceSDK.Rest
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