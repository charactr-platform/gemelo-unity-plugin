using System.Collections;
using Charactr.VoiceSDK.Streaming;
using UnityEngine;

public class StreamingExample : MonoBehaviour
{
    [SerializeField] AudioStreamingManager streamingManager;
    [SerializeField] private string text = "Hello from Charactr Software Development Kit for Unity";
    // Start is called before the first frame update

    IEnumerator Start()
    {
        yield return streamingManager.ConvertAndStartPlaying(text);
    }

}
