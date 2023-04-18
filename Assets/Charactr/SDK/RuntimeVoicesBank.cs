using System;
using Charactr.Editor.Library;
using Charactr.SDK.Library;
using UnityEngine;

namespace Charactr.SDK
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
            
        }

        public void PlayLibraryVoice(int id)
        {
            if (voiceLibrary.GetAudioClipById(id, out var clip))
            {
                EditorAudioPlayer.PlayClip(clip);
            }
        }
    }
}
