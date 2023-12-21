using System.Collections.Generic;
using System.Linq;
using Gemelo.Voice.Rest.Model;
using NUnit.Framework;
using Task = System.Threading.Tasks.Task;

namespace Gemelo.Voice.Tests
{
	public class VoicesResponseEndpoint : TestBase
	{
		[Test]
		public async Task GetSystemVoices_Data_NotEmpty()
		{
			var response = await EditorHttp.GetDataAsync(Configuration.VOICES_API);
			Assert.IsNotEmpty(response);

			var stringData = System.Text.Encoding.UTF8.GetString(response);
			Assert.IsNotEmpty(stringData);
		}

		[Test]
		public async Task GetSystemVoices_Items_NotEmpty()
		{
			var response = await EditorHttp.GetAsync<SystemVoicesResponse>(Configuration.VOICES_API);
			Assert.NotNull(response);
			Assert.IsNotEmpty(response.Items);
			var firstItem = response.Items.First();
			Assert.NotNull(firstItem);
			Assert.IsInstanceOf<SystemVoicePreviewItem>(firstItem);
			Assert.AreEqual(VoiceType.System, firstItem.Type);
		}
		
		[Test]
		public async Task GetClonedVoices_Data_NotEmpty()
		{
			var response = await EditorHttp.GetDataAsync(Configuration.CLONED_API);
			Assert.IsNotEmpty(response);

			var stringData = System.Text.Encoding.UTF8.GetString(response);
			Assert.IsNotEmpty(stringData);
		}

		[Test]
		public async Task GetClonedVoices_Items_NotEmpty()
		{
			var response = await EditorHttp.GetAsync<ClonedVoicesResponse>(Configuration.CLONED_API);
			
			Assert.IsNotNull(response);
			Assert.IsNotEmpty(response.Items);
			
			var firstItem = response.Items.First();
			Assert.NotNull(firstItem);
			Assert.IsInstanceOf<ClonedVoicePreviewItemItem>(firstItem);
			Assert.AreEqual(VoiceType.Clone, firstItem.Type);
		}

		[Test]
		public async Task GetCommonVoices_Response_NotEmpty()
		{
			var system = await EditorHttp.GetAsync<SystemVoicesResponse>(Configuration.VOICES_API);
			var cloned = await EditorHttp.GetAsync<ClonedVoicesResponse>(Configuration.CLONED_API);
			
			var items = new List<IVoicePreviewItem>();
			items.AddRange(system.Items);
			items.AddRange(cloned.Items);
			var response = new VoicesResponse(items);
			
			Assert.NotNull(response);
			Assert.IsNotEmpty(response.Items);
		
			Assert.NotZero(response.Items.Count(c => c.Type.Equals(VoiceType.Clone)));
			Assert.NotZero(response.Items.Count(c => c.Type.Equals(VoiceType.System)));
		}
	}
}