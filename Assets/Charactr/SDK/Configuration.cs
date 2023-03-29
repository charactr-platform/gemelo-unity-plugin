using UnityEditor;
using UnityEngine;

namespace Charactr.VoiceSDK.SDK
{
	public class Configuration : ScriptableObject
	{
		public const string API = "https://api.charactr.com/v1/tts/";
		private const string FILENAME = "Configuration";
		public const string SAVE_PATH = "Assets/Charactr/Resources/"+FILENAME+".asset";
		
		public string ApiClient
		{
			get => apiClient;
			set => apiClient = value;
		}

		public string ApiKey
		{
			get => apiKey;
			set => apiKey = value;
		}

		public string AudioSavePath
		{
			get => audioSavePath;
			set => audioSavePath = value;
		}
		
		[SerializeField] private string apiClient;
		[SerializeField] private string apiKey;
		[SerializeField] private string audioSavePath;

#if UNITY_EDITOR
		public static void Save(string apiClient, string apiKey, string audioSavePath = "Assets/Charactr/Audio/")
		{
			var instance = CreateInstance<Configuration>();
			instance.Create(apiClient, apiKey, audioSavePath);
			AssetDatabase.CreateAsset(instance, SAVE_PATH);
		}
		
#endif

		public void Create(string apiClientString, string apiKeyString, string savePathString)
		{
			ApiClient = apiClientString;
			ApiKey = apiKeyString;
			AudioSavePath = savePathString;
		}
		
		public static Configuration Load()
		{
			return Resources.Load<Configuration>(FILENAME);
		} 
	}
}