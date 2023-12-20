using System.Collections.Generic;

namespace Gemelo.Voice.Rest.Model
{
	public interface IVoicesResponse
	{
		public IEnumerable<IVoicePreview> Items { get; }
	}
}