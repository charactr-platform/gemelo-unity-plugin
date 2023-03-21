using System.Collections;
using System.Threading.Tasks;
using Charactr.Api.Rest;
using NUnit.Framework;
using UnityEngine;


namespace Charactr.Tests
{
    public class Voices
    {
        private const string CLIENT_KEY = "87Pgd9jP4BoFeblC8xstxSHGVdnhZl1B";
        private const string API_KEY = "qynCJRdjOc3hNvAAmDZUMFgTqi1hOXTC";
        private const string API_ENDPOINT = "https://api.charactr.com/v1/tts/";
        
        private EditorHttpClient _http;
        
        [SetUp]
        public void Setup()
        {
            _http = new EditorHttpClient(CLIENT_KEY, API_KEY, (error) => Debug.LogError(error.Message));
        }
        
        [Test]
        public async Task GetVoices_Returns_OK()
        {
            var voices = await _http.GetAsync<VoicesResponse>(API_ENDPOINT + "voices");
            Assert.NotNull(voices);
            Assert.IsNotEmpty(voices);
        }
        
    }
}