using System;
using System.Collections.Generic;
using gemelo.VoiceSDK.Audio;
using gemelo.VoiceSDK.Library;
using UnityEngine;

namespace gemelo.VoiceSDK
{
    /// <summary>
    /// Allows to use VoiceLibrary object on any GameObject and play selected voices
    /// </summary>
    public class RuntimeVoicesBank : MonoBehaviour
    {
        [Tooltip("Attach VoiceLibrary object from Resources folder")]
        [SerializeField] private VoiceLibrary voiceLibrary;

        private void Awake()
        {
            if (voiceLibrary == null)
                throw new Exception("Please add VoiceLibrary item");
        }

        public void PlayLibraryVoice(int id)
        {
            if (voiceLibrary.GetAudioClipById(id, out var clip))
                AudioPlayer.PlayClipStatic(clip);
        }

        public List<VoiceItem> GetItems() => voiceLibrary.Items;
        public int GetCount() => voiceLibrary.Items.Count;
    }
}
