using System;
using System.Collections;
using System.Collections.Generic;
using Charactr.VoiceSDK.Streaming;
using UnityEngine;

public class StreamingExample : MonoBehaviour
{
    [Serializable]
    public struct VoiceDb
    {
        public string Text;
        public int VoiceId;
    }
    
    [SerializeField] AudioStreamingManager streamingManager;
    [SerializeField] private List<VoiceDb> texts = new List<VoiceDb>()
    {
        new VoiceDb()
        {
            Text = "Hello from Charactr Software Development Kit for Unity.",
            VoiceId = 181,
        },
    };
    
    IEnumerator Start()
    {
        for (int i = 0; i < texts.Count; i++)
        {
            Debug.Log("Play next: "+i);
            yield return PlayNext(i);
        }
    }

    private IEnumerator PlayNext(int i)
    {
        var current = texts[i];
        yield return PlayText(current.Text, current.VoiceId);
    }

    private IEnumerator PlayText(string text, int voiceID)
    {
        streamingManager.SetVoiceId(voiceID);
        yield return streamingManager.ConvertAndStartPlaying(text);
    }
}
