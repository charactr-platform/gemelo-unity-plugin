using Newtonsoft.Json;

namespace Charactr.VoiceSDK.Model
{
	public class RequestBase
	{
		public string ToJson() => 
			JsonConvert.SerializeObject(this);

		public override string ToString() => ToJson();
	}
}