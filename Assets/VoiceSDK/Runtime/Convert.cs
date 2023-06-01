using System;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Audio;
using Charactr.VoiceSDK.Rest;
using Charactr.VoiceSDK.Rest.Client;
using Charactr.VoiceSDK.Rest.Model;
using UnityEngine;

namespace Charactr.VoiceSDK
{
	/// <summary>
	/// Base class to utilize Charactr API
	/// </summary>
	public class Convert: IConvert, IDisposable
	{
		public Configuration Configuration { get; }
		public byte[] Data { get; private set; }
		
		private readonly EditorRestClient _client;

		public Convert()
		{
			var configuration = Configuration.Load();
			
			if (configuration == null)
			{
				//ApiConfigurationWindow.ShowWindow();
				return;
			}

			Configuration = configuration;
			
			_client = new EditorRestClient(Configuration, OnRestError);
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
			if (_client == null)
				throw new Exception("Can't connect to API, please provide configuration details first!");
			
			ValidateRequest(convertRequest);
				
			var wavData = await _client.PostAsync(Configuration.API + "convert", convertRequest.ToJson());
			
			if (wavData.Length == 0)
				throw new Exception("Can't download requested WAV data");
			
			Data = wavData;
			
			return new WavBuilder(wavData).CreateAudioClip();
		}

		public UnityAudioConvertRequest ConvertToAudioClipRuntime(ConvertRequest convertRequest)
		{
			if (_client == null)
				throw new Exception("Can't connect to API, please provide configuration details first!");
			
			ValidateRequest(convertRequest);
			
			var runtimeRest = new RuntimeRestClient(Configuration);

			return runtimeRest.GetAudioClipRequest(Configuration.API + "convert", convertRequest);
		}

		private void ValidateRequest(ConvertRequest request)
		{
			if (string.IsNullOrEmpty( request.Text))
				throw new Exception("Text can't be empty");

			if ( request.VoiceId <= 0)
				throw new Exception("Please set proper voice Id");
		}
		public void Dispose()
		{
			_client?.Dispose();
		}
	}
}