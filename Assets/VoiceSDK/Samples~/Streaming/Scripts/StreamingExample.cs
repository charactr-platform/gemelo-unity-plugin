using System;
using System.Collections;
using System.Collections.Generic;
using Charactr.VoiceSDK.Streaming;
using UnityEngine;
using UnityEngine.UI;

public class StreamingExample : MonoBehaviour
{
    [Serializable]
    public struct VoiceDb
    {
        public string Text;
        public int VoiceId;
    }

    [SerializeField] private Text textToSpeechText;
    [SerializeField] private Text voiceIdText;
    [SerializeField] private Button startButton;
    [SerializeField] private Toggle autoplayToggle;
    
    [SerializeField] AudioStreamingManager streamingManager;
    [SerializeField] private List<VoiceDb> texts = new List<VoiceDb>()
    {
        new VoiceDb()
        {
            Text = "Hello from Charactr Software Development Kit for Unity.",
            VoiceId = 181,
        },
    };

    private void Awake()
    {
        startButton.onClick.AddListener(()=>StartCoroutine(StartVoiceStreaming()));
    }

    IEnumerator StartVoiceStreaming()
    {
        var count = autoplayToggle.isOn ? texts.Count : 1;
        
        for (int i = 0; i < count; i++)
        {
            Debug.Log("Play next: "+i);
            yield return PlayNext(i);
        }
    }

    private IEnumerator PlayNext(int i)
    {
        var current = texts[i];
        
        textToSpeechText.text = $"Text: {current.Text}";
        voiceIdText.text = $"VoiceID: {current.VoiceId}";
        
        yield return StreamTextToSpeech(current.Text, current.VoiceId);
    }

    private IEnumerator StreamTextToSpeech(string text, int voiceID)
    {
        streamingManager.SetVoiceId(voiceID);
        yield return streamingManager.ConvertAndStartPlaying(text);
    }
}
