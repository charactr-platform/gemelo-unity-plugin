using System.Threading.Tasks;
using Gemelo.Voice.Rest.Model;
using NUnit.Framework;

namespace Gemelo.Voice.Tests
{
	public class ClonedVoices : TestBase
	{
		[Test]
		public async Task GetClonedVoices_Data_NotEmpty()
		{
			var bytes = await EditorHttp.GetDataAsync(Configuration.CLONED_API);
			Assert.IsNotEmpty(bytes);

			var stringData = System.Text.Encoding.UTF8.GetString(bytes);
			Assert.IsNotEmpty(stringData);
		}

		[Test]
		public async Task GetClonedVoices_Items_NotEmpty()
		{
			var response = await EditorHttp.GetAsync<ClonedVoicesResponse>(Configuration.CLONED_API);
			Assert.IsNotEmpty(response.Items);
		}
	}
}