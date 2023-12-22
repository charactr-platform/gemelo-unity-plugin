using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gemelo.Voice.Rest.Model
{
	public class ClonedVoicesResponse : IVoicesResponse
	{
		[JsonProperty("items")] 
		private List<ClonedVoicePreviewItemItem> items;

		public IEnumerable<IVoicePreviewItem> Items { get => items; }
	}
	
	[Serializable]
	public class ClonedVoicePreviewItemItem : BaseVoicePreviewItem, IVoicePreviewItem
	{
		public VoiceType Type => VoiceType.Cloned;
		
		[JsonProperty("createdAt")]
		public DateTime CreatedAt { get; set; }
	}
}