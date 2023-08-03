using System.Collections;
using System.Text;
using Gemelo.VoiceSDK.Rest.Model;
using UnityEngine;
using UnityEngine.Networking;

namespace Gemelo.VoiceSDK.Rest.Client
{
	public class UnityAudioConvertRequest
	{
		public UnityWebRequest.Result Result { get; private set; }
		public AudioClip AudioClip { get; private set; }
		
		private readonly UnityWebRequest _request;
		private readonly DownloadHandlerAudioClip _handler;
		public IEnumerator SendRequest()
		{
			yield return _request.SendWebRequest();

			Result = _request.result;
			
			if (Result != UnityWebRequest.Result.Success)
			{
				Debug.LogError(_request.error);
				yield break;
			}

			AudioClip = _handler.audioClip;
		}
		
		public UnityAudioConvertRequest(string url, ConvertRequest covertRequest, bool stream = false)
		{
			_handler = new DownloadHandlerAudioClip(url, AudioType.WAV);
			_handler.streamAudio = stream;
			_handler.compressed = false;
			
			_request = new UnityWebRequest(url, "POST");
			_request.downloadHandler = _handler;
			
			SetupPost(_request, covertRequest.ToJson(), "application/json");
		}

		public void AddHeader(string key, string value)
		{
			_request.SetRequestHeader(key, value);
		}
		
		private static void SetupPost(UnityWebRequest request, string postData, string contentType)
		{
			if (string.IsNullOrEmpty(postData))
			{
				request.SetRequestHeader("Content-Type", contentType);
			}
			else
			{
				request.uploadHandler = (UploadHandler) new UploadHandlerRaw(Encoding.UTF8.GetBytes(postData));
				request.uploadHandler.contentType = contentType;
			}
		}
	}
}