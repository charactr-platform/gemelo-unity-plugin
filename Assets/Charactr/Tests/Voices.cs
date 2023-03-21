using System.Threading.Tasks;
using Charactr.Api.Rest;
using NUnit.Framework;
using UnityEngine.TestTools;


namespace Charactr.Tests
{
    public class Voices: TestBase
    {
        private const string ENDPOINT = "voices";
        
        [Test]
        public async Task GetVoices_Returns_OK()
        {
            var voices = await Http.GetAsync<VoicesResponse>(API + ENDPOINT);
            Assert.NotNull(voices);
            Assert.IsNotEmpty(voices);
        }
    }
}