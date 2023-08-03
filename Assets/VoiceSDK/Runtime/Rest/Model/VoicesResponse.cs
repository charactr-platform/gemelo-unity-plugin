using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gemelo.VoiceSDK.Rest.Model
{
	public class VoicesResponse : List<VoiceDescription>, IAPIResponse { }
	
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