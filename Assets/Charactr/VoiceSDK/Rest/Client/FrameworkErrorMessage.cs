namespace Charactr.VoiceSDK.Rest
{
	public class FrameworkErrorMessage
	{
		public int StatusCode { get; set; }
		public string Source { get; set; }
		public string Message { get; set; }
		public override string ToString()
		{
			return Message;
		}
	}
}