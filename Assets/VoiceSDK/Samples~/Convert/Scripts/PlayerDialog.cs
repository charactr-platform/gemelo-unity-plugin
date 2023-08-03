using System;
using System.Collections;
using System.Collections.Generic;
using Gemelo.VoiceSDK.Audio;
using Gemelo.VoiceSDK.Library;
using UnityEngine;
using UnityEngine.UI;

namespace Gemelo.VoiceSDK.Samples.Convert
{
    [RequireComponent(typeof(AudioPlayer))]
    public class PlayerDialog : MonoBehaviour
    {
        public event Action<int> OnVoiceItemPlayed;
        
        [SerializeField] private Text voiceDetails;
        [SerializeField] private RuntimeVoicesBank runtimeBank;
      
        private List<VoiceItem> _items;
        private AudioPlayer _player;
        private int index = 0;
        
        // Cache player and items to be played from VoiceLibrary bank in Awake()
        void Awake()
        {
            _player = GetComponent<AudioPlayer>();
            _player.Initialize(false);
            _items = runtimeBank.GetItems();
        }
        
        //This method invokes next voice clip from VoiceLibrary
        public bool PlayNextSequence()
        {
            var canPlay = index < _items.Count;
            
            if (canPlay)
            {
                StartCoroutine(PlayNext());
                index++;
            }

            return canPlay;
        }

        public bool ContainsVoiceItemId(int id)
        {
            return _items.Exists(e => e.Id.Equals(id));
        }
        private IEnumerator PlayNext()
        {
            var item = _items[index];
            
            voiceDetails.text = $"Item Id:{item.Id}\n";
            
            yield return _player.PlayClipRoutine(item.AudioClip);
            
            voiceDetails.text = string.Empty;
            
            OnVoiceItemPlayed?.Invoke(item.Id);
        }
    }
}
