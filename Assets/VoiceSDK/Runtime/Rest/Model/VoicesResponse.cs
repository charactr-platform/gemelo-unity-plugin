using System.Collections.Generic;

namespace Gemelo.Voice.Rest.Model
{
	public class VoicesResponse : IVoicesResponse
	{
		public IEnumerable<IVoicePreviewItem> Items { get; }

		public VoicesResponse(IEnumerable<IVoicePreviewItem> items)
		{
			Items = items;
		}
	}
}