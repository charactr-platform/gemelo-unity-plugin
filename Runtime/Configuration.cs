﻿using System;
using UnityEditor;
using UnityEngine;

namespace Gemelo.Voice
{
	public class Configuration : ScriptableObject
	{
#if DEVELOPMENT 
		public const string HOST = "api.slowpoke.charactr.dev";
#else
		public const string HOST = "api.gemelo.ai";
#endif
		public const string CLONED_API = "https://" + HOST + "/v1/cloned-voices";
		public const string CONVERT_API = "https://" + HOST + "/v1/tts/convert";
		public const string VOICES_API = "https://" + HOST + "/v1/tts/voices";
		public const string STREAMING_API = "wss://" + HOST + "/v1/tts/stream/simplex/ws";
		
		public const string USER_AGENT = "sdk-unity";
		public const string API_CLIENT = "X-Client-Key";
		public const string API_KEY = "X-Api-Key";
		public const string SAVE_PATH = GLOBAL_SAVE_PATH + FILENAME + ".asset";
		public const string GLOBAL_SAVE_PATH = "Assets/Resources/";
		
		public const string DEFAULT_TEXT = "Hello from Gemelo.AI Unity Plugin!";
		public static string CachePath => Application.persistentDataPath + "/PreviewCache";

		private const string FILENAME = "Configuration";

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
		public static void Save(string apiClient, string apiKey, string audioSavePath = "Assets/Audio/")
		{
			var instance = CreateInstance<Configuration>();
			instance.Create(apiClient, apiKey, audioSavePath);
			CheckForResourcesDir();
			AssetDatabase.CreateAsset(instance, SAVE_PATH);
		}
		
		public static void CheckForResourcesDir()
		{
			if (!AssetDatabase.IsValidFolder($"Assets/Resources"))
			{
				AssetDatabase.CreateFolder("Assets", "Resources");
				Debug.Log("Created default Resources folder in Assets/Resources");
			}
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
			var configuration = Resources.Load<Configuration>(FILENAME);
			if (configuration == null)
				throw new Exception("Create configuration settings first in top menu: Tools->Gemelo.ai Voice->Configuration");
			
			return configuration;
		}

		public static bool Exists() =>
			Resources.Load<Configuration>(FILENAME) != null;
	}
}