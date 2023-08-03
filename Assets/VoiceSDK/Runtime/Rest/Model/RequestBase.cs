using Newtonsoft.Json;

namespace Gemelo.VoiceSDK.Rest.Model
{
	public class RequestBase
	{
		public string ToJson() => 
			JsonConvert.SerializeObject(this);

		public override string ToString() => ToJson();
	}
}