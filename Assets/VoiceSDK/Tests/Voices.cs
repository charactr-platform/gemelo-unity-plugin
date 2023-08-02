using System.Threading.Tasks;
using Charactr.VoiceSDK.Rest.Model;
using NUnit.Framework;

namespace Charactr.VoiceSDK.Tests
{
    public class Voices: TestBase
    {
        private const string ENDPOINT = "voices";
        
        [Test]
        public async Task GetVoices_Returns_OK()
        {
            var voices = await EditorHttp.GetAsync<VoicesResponse>(Configuration.CONVERT_API + ENDPOINT);
            Assert.NotNull(voices);
            Assert.IsNotEmpty(voices);
        }
    }
}