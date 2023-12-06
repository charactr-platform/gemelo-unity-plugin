using System;
using System.IO;
using System.Threading.Tasks;
using Gemelo.Voice.Editor.Preview;
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
        }
        
        public AudioClip AudioClip
        {
            get => audioClip;
            set => audioClip = value;
        }

        public long Timestamp => timestamp;
        public int Id => id;
        
        [SerializeField] private string text;
        [SerializeField] private int voiceId;
        [SerializeField] private AudioClip audioClip;
        //Used in Editor Inspector
        [SerializeField] private VoicePreview voicePreview;
        [SerializeField] private int id;
        [SerializeField] private long timestamp;
        public bool IsValid() => !string.IsNullOrEmpty(Text) && VoiceId > 0 && VoiceId < 999 && Timestamp > 0;
        public ConvertRequest GetRequest()
        {
            return new ConvertRequest()
            {
                Text = text,
                VoiceId = voiceId
            };
        }

        public void InitializeEmpty()
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            voiceId = Configuration.DEFAULT_VOICE_ID;
            text = Configuration.DEFAULT_TEXT;
        }

        public VoiceItem(int voiceId)
        {
            InitializeEmpty();
            this.voiceId = voiceId;
        }
        
        
        public void SetVoicePreview(VoicePreview voicePreview)
        {
            this.voiceId = voicePreview.Id;
            this.voicePreview = voicePreview;
            voicePreview.VoiceItemId = Id;
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
