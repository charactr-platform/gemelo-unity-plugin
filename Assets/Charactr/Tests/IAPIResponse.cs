using System.Collections.Generic;
using Charactr.Api.Rest;
using Newtonsoft.Json;

namespace Charactr.Api.Rest
{
	public interface IAPIResponse
	{
		
	}

	public class RequestBase
	{
		public string ToJson() => 
			JsonConvert.SerializeObject(this);

		public override string ToString() => ToJson();
	}

	public class ConvertRequest: RequestBase
	{
		[JsonProperty("voiceId")]
		public int VoiceId { get; set; }

		[JsonProperty("text")]
		public string Text { get; set; }
	}
	
	public class VoicesResponse : List<VoiceDescription>, IAPIResponse
	{
		
	}
	
	public class LabelDescription
	{
		[JsonProperty("category")]
		public string Category { get; set; }

		[JsonProperty("label")]
		public string Label { get; set; }
	}

	public class VoiceDescription
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("previewUrl")]
		public string PreviewUrl { get; set; }

		[JsonProperty("labels")]
		public List<LabelDescription> Labels { get; set; }
	}
}