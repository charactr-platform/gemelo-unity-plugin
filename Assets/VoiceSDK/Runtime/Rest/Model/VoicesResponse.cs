using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Gemelo.Voice.Rest.Model
{
	public class VoicesResponse : List<VoicePreviewItem>, IAPIResponse
	{
		public List<VoicePreviewItem> Data => this.ToList();
	}
	
	[Serializable]
	public class VoiceLabel
	{
		[JsonProperty("category")]
		public string Category { get; set; }

		[JsonProperty("label")]
		public string Label { get; set; }
	}
    
	[Serializable]
	public class VoicePreviewItem
	{
		public string Url
		{
			get => PreviewUrls?.Count > 0 ? PreviewUrls[0] : string.Empty;
		}
		
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("previewUrls")]
		public List<string> PreviewUrls { get; set; }

		[JsonProperty("rating")]
		public float Rating { get; set; }

		[JsonProperty("labels")]
		public List<VoiceLabel> Labels { get; set; }

		[JsonProperty("new",NullValueHandling = NullValueHandling.Ignore)]
		public bool New { get; set; }
	}

}