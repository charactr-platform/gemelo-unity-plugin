using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Gemelo.Voice.Rest.Model
{
	public class SystemVoicesResponse : List<SystemVoicePreviewItem>, IVoicesResponse
	{
		public IEnumerable<IVoicePreviewItem> Items => this.AsEnumerable();
	}
	
	[Serializable]
	public class VoiceLabel
	{
		[JsonProperty("category")]
		public string Category { get; set; }

		[JsonProperty("label")]
		public string Label { get; set; }
	}
	
	public interface IVoicePreviewItem
	{
		int Id { get; }
		string Url { get; }
		string Name { get; }
		public VoiceType Type { get; }
	}

	public enum VoiceType
	{
		None = -1,
		All = 0,
		System = 1,
		Cloned = 2,
	}
	
	public class BaseVoicePreviewItem
	{
		public string Url => PreviewUrls?.Count > 0 ? PreviewUrls[0] : string.Empty;

		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("previewUrls")]
		public List<string> PreviewUrls { get; set; }
	}

	[Serializable]
	public class SystemVoicePreviewItem : BaseVoicePreviewItem, IVoicePreviewItem
	{
		public VoiceType Type => VoiceType.System;
		
		[JsonProperty("description")]
		public string Description { get; set; }
		
		[JsonProperty("rating")]
		public float Rating { get; set; }
		
		[JsonProperty("disabled")]
		public bool Disabled { get; set; }

		[JsonProperty("labels")]
		public List<VoiceLabel> Labels { get; set; }

		[JsonProperty("new", NullValueHandling = NullValueHandling.Ignore)]
		public bool New { get; set; }
	}
}