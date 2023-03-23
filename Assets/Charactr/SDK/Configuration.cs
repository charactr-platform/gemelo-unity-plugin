using UnityEditor;
using UnityEngine;

namespace Charactr.VoiceSDK.SDK
{
	public class Configuration : ScriptableObject
	{
		public const string API = "https://api.charactr.com/v1/tts/";
		private const string FILENAME = "Configuration";
		private const string SAVE_PATH = "Assets/Charactr/SDK/Resources/"+FILENAME+".asset";
		
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
		
		[SerializeField] private string apiClient;
		[SerializeField] private string apiKey;

#if UNITY_EDITOR
		public static void Save(string apiClient, string apiKey)
		{
			if (Load() != null && !EditorUtility.DisplayDialog("Overwrite", "Found previous configuration, overwrite?", "YES", "CANCEL"))
				return;
			
			var instance = CreateInstance<Configuration>();
			instance.Create(apiClient, apiKey);
			AssetDatabase.CreateAsset(instance, SAVE_PATH);
		}
		
#endif

		public void Create(string apiClientString, string apiKeyString)
		{
			ApiClient = apiClientString;
			ApiKey = apiKeyString;
		}
		
		public static Configuration Load()
		{
			return Resources.Load<Configuration>(FILENAME);
		} 
	}
}