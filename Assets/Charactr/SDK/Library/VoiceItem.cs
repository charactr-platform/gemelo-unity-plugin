using System;
using System.IO;
using System.Threading.Tasks;
using Charactr.VoiceSDK.Model;
using Charactr.VoiceSDK.SDK;
using UnityEditor;
using UnityEngine;
using Convert = Charactr.VoiceSDK.SDK.Convert;

namespace Charactr.SDK.Library
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

        public int Id => Mathf.Abs(text.GetHashCode());
        
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
                SaveInProject(convert.Configuration);
            }

            Debug.Log($"Updated audio clip for voiceItem = {Id}");
            return AudioClip;
        }
        
    
        public void SaveInProject(Configuration configuration)
        {
#if UNITY_EDITOR
            if (audioClip == null)
                throw new Exception($"VoiceItem ({Id}) don't contains generated AudioClip");

            var di = new DirectoryInfo(configuration.AudioSavePath);
			
            if (!di.Exists)
                di.Create();

            var filePath = $"{configuration.AudioSavePath}/{Id}.wav";
            File.WriteAllBytes(filePath, WavUtility.FromAudioClip(AudioClip));
            AssetDatabase.ImportAsset(filePath);
            Debug.Log($"Saved asset at: {filePath}");
            AudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(filePath);
            Debug.Assert(AudioClip != null);
         
#endif
        }

    }
}
