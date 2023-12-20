using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gemelo.Voice.Rest.Model
{
	public class ClonedVoicesResponse : IVoicesResponse
	{
		[JsonProperty("items")]
		public IEnumerable<IVoicePreview> Items { get; set; }
	}
	
	[Serializable]
	public class ClonedVoicePreviewItem : BaseVoicePreviewItem, IVoicePreview
	{
		[JsonProperty("createdAt")]
		public DateTime CreatedAt { get; set; }
	}
}