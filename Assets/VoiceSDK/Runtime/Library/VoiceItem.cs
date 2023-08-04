using System;
using System.IO;
using System.Threading.Tasks;
using Gemelo.Voice.Rest.Model;
using UnityEditor;
using UnityEngine;

namespace Gemelo.Voice.Library
{
    [System.Serializable]
    public class VoiceItem
    {
        public string Text
        {
            get => text;
            set => text = value;
        }

        public int VoiceId
        {
            get => voiceId;
            set => voiceId = value;
        }

        public AudioClip AudioClip
        {
            get => audioClip;
            set => audioClip = value;
        }

        public int Id
        {
            get { return Mathf.Abs(text.GetHashCode() + voiceId); }
        }

        [SerializeField] private string text;
        [SerializeField] private int voiceId;
        [SerializeField] private AudioClip audioClip;
        
        public bool IsValid() => !string.IsNullOrEmpty(Text) && VoiceId > 0 && voiceId < 999;
        public ConvertRequest GetRequest()
        {
            return new ConvertRequest()
            {
                Text = text,
                VoiceId = voiceId
            };
        }
        public async Task<AudioClip> GetAudioClip()
        {
            if (!IsValid())
            {
                Debug.LogError($"Can't convert voiceItem {Id}");
                return null;
            }

            using (var convert = new Convert())
            {
                audioClip = await convert.ConvertToAudioClip(GetRequest());
    #if UNITY_EDITOR
                TryRemoveClip();
                SaveInProject(convert);
    #endif
            }

            Debug.Log($"Updated audio clip for voiceItem = {Id}");
            return AudioClip;
        }

#region Editor helpers

    #if UNITY_EDITOR
        public void TryRemoveClip()
        {

            if (audioClip == null)
                return;

            var path = AssetDatabase.GetAssetPath(audioClip);

            if (string.IsNullOrEmpty(path))
                return;
            
            var hash = GetHashCode().ToString();
            var importer = AssetImporter.GetAtPath(path);

            if (importer.userData.Equals(hash) && AssetDatabase.DeleteAsset(path))
                Debug.Log($"Removed old asset : {path}");

        }
        public void SaveInProject(Convert convert)
        {
            if (audioClip == null)
                throw new Exception($"VoiceItem ({Id}) don't contains generated AudioClip");

            if (convert.Data == null || convert.Data.Length == 0)
                throw new Exception("Can't save file, data is empty");
            
            var configuration = convert.Configuration;
            var data = convert.Data;
            
            var di = new DirectoryInfo(configuration.AudioSavePath);
			
            if (!di.Exists)
                di.Create();

            var filePath = $"{configuration.AudioSavePath}{Id}.wav";
            File.WriteAllBytes(filePath, data);
            
            AssetDatabase.ImportAsset(filePath);
            Debug.Log($"Saved asset at: {filePath}");
            audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(filePath);
            SetClipHashData(audioClip);
            Debug.Assert(AudioClip != null);

        }
        private void SetClipHashData(AudioClip clip)
		{
			var path = AssetDatabase.GetAssetPath(clip);
            
			var importer = AssetImporter.GetAtPath(path);
            
			importer.userData = GetHashCode().ToString();
			importer.SaveAndReimport();
		}
    #endif
#endregion
    }
}
