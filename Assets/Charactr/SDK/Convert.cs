using System;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Model;
using Charactr.VoiceSDK.Rest;
using UnityEngine;

namespace Charactr.VoiceSDK.SDK
{
	/// <summary>
	/// Base class to utilize Charactr API
	/// </summary>
	public class Convert: IConvert
	{
		private RestHttpClient _client;
		private Configuration _configuration;
		public Convert()
		{
			_configuration = Configuration.Load();
			
			if (_configuration == null)
			{
				//TODO: Show new configuration wizard
				Debug.LogError("Can't find configuration");
				return;
			}

			_client = new RestHttpClient(_configuration.ApiClient, _configuration.ApiKey, OnRestError);
		}

		private void OnRestError(FrameworkErrorMessage errorMessage)
		{
			//TODO: Show nice editor dialog with error message
			Debug.LogError(errorMessage.Message);
		}

		/// <summary>
		/// This method is core utility to use in our API. It allows to download converted audio from text input
		/// in async manner.
		/// </summary>
		/// <param name="convertRequest">Request object, filled with text data and selected VoiceID integer</param>
		/// <returns>Returns AudioClip object that can be used in AudioSource.clip property.</returns>
		/// <exception cref="Exception">Throws exception when data can't be downloaded, ie. network error</exception>
		public async Task<AudioClip> ConvertToAudioClip(ConvertRequest convertRequest)
		{
			var wavData = await _client.PostAsync(Configuration.API + "convert", convertRequest.ToJson());
			
			if (wavData.Length == 0)
				throw new Exception("Can't download requested WAV data");
			
			return WavUtility.ToAudioClip(wavData);
		}
	}
}