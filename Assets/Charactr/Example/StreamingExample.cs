using System;
using System.Collections;
using System.Collections.Generic;
using Charactr.VoiceSDK.Streaming;
using UnityEngine;

public class StreamingExample : MonoBehaviour
{
    [SerializeField] AudioStreamingManager streamingManager;
    [SerializeField] private List<string> texts = new List<string>()
    {
        "Hello from Charactr Software Development Kit for Unity",
        "No valid plans for the future can be made by those who have no capacity for living now.",
        "Try not to become a man of success, but rather try to become a man of value."
    };
    // Start is called before the first frame update
    private int index = 0;
    void Start()
    {
        streamingManager.OnAudioEnd += PlayNext;
        PlayNext();
    }

    private void PlayNext()
    {
        var c = index++ % texts.Count;
        StartCoroutine(PlayText(texts[c]));
    }

    private IEnumerator PlayText(string text)
    {
        yield return streamingManager.ConvertAndStartPlaying(text);
    }
}
