using Newtonsoft.Json;

namespace Charactr.VoiceSDK.Model
{
	public class ConvertRequest: RequestBase
	{
		[JsonProperty("voiceId")]
		public int VoiceId { get; set; }

		[JsonProperty("text")]
		public string Text { get; set; }
		
	}
}