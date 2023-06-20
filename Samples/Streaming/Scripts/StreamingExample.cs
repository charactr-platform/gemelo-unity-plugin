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
        yield break;
        
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

    private void OnGUI()
    {
        var x = Screen.width / 2;
        var y = Screen.height / 2;
        
        if (GUI.Button(new Rect(x-100, y-100, 200, 35), "Vits: 40"))
            StartCoroutine(PlayText(texts[0].Text, 40));
        
        if (GUI.Button(new Rect(x-100, y-55, 200, 35), "Wtv: 64"))
            StartCoroutine(PlayText(texts[0].Text, 64));
        
        if (GUI.Button(new Rect(x-100, y-10, 200, 35), "Wtv_sid: 150"))
            StartCoroutine(PlayText(texts[0].Text, 150));
    }
}
