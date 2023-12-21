using System.Collections.Generic;

namespace Gemelo.Voice.Rest.Model
{
	public interface IVoicesResponse
	{
		public IEnumerable<IVoicePreviewItem> Items { get; }
	}
}