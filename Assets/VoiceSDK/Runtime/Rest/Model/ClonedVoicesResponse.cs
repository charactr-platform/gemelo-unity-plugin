using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gemelo.Voice.Rest.Model
{
	public class CommonVoicesResponse : IVoicesResponse
	{
		public IEnumerable<IVoicePreview> Items { get; }

		public CommonVoicesResponse(IEnumerable<IVoicePreview> items)
		{
			Items = items;
		}
	}
	
	public class ClonedVoicesResponse : IVoicesResponse
	{
		[JsonProperty("items")] 
		private List<ClonedVoicePreviewItem> items;

		public IEnumerable<IVoicePreview> Items { get => items; }
	}
	
	[Serializable]
	public class ClonedVoicePreviewItem : BaseVoicePreviewItem, IVoicePreview
	{
		public VoiceType Type => VoiceType.Clone;
		
		[JsonProperty("createdAt")]
		public DateTime CreatedAt { get; set; }
	}
}