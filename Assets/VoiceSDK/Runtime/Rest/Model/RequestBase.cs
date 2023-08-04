using Newtonsoft.Json;

namespace Gemelo.Voice.Rest.Model
{
	public class RequestBase
	{
		public string ToJson() => 
			JsonConvert.SerializeObject(this);

		public override string ToString() => ToJson();
	}
}