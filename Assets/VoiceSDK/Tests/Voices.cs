using System.Threading.Tasks;
using Gemelo.VoiceSDK.Rest.Model;
using NUnit.Framework;

namespace Gemelo.VoiceSDK.Tests
{
    public class Voices: TestBase
    {
        [Test]
        public async Task GetVoices_Returns_OK()
        {
            var voices = await EditorHttp.GetAsync<VoicesResponse>(Configuration.VOICES_API);
            Assert.NotNull(voices);
            Assert.IsNotEmpty(voices);
        }
    }
}