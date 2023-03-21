using Charactr.Api.Rest;
using NUnit.Framework;
using UnityEngine;

namespace Charactr.Tests
{
	public class TestBase
	{
		private const string CLIENT_KEY = "87Pgd9jP4BoFeblC8xstxSHGVdnhZl1B";
		private const string API_KEY = "qynCJRdjOc3hNvAAmDZUMFgTqi1hOXTC";
        
		protected const string API = "https://api.charactr.com/v1/tts/";
        
		protected EditorHttpClient Http;
        
		[SetUp]
		public void Setup()
		{
			Http = new EditorHttpClient(CLIENT_KEY, API_KEY, (error) => Debug.LogError(error.Message));
		}
	}
}