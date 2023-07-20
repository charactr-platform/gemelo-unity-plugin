Charactr Voice SDK for Unity
===========
Please visit [Character Voice Studio](https://charactr.com/studio) for account creation and API keys.

This is the UPM package for the Charactr Voice SDK API.

Install it via menu `Window/Package Manager->Add package from Git url` in Unity:
```
https://github.com/charactr-platform/charactr-api-sdk-unity.git#v0.3.0
```
Please restart Unity Editor after package installation.

## Unity version support

- Developed and tested on Unity 2021 LTS (2021.3.10)
- Tested on Unity 2022 LTS (2022.3.1)
- Tested on Unity 2023 (2023.1.5)

## Platform support (tested)

- Mac/Windows
- Android
- iOS
- WebGL

## HowTo samples

* Play VoiceLibrary item:
```
using Charactr.VoiceSDK.Audio;
using Charactr.VoiceSDK.Library;
using UnityEngine;

public class PlayVoiceLibrary : MonoBehaviour
{
    //SerializedObject containing voice items list
    [SerializeField] private VoiceLibrary voiceLibrary;

    //ID of single voice item from VoiceLibrary
    [SerializeField] private int itemId;
    
    void Start()
    {
        PlayVoiceClipById(itemId);
    }

    private void PlayAudioClipById(int id)
    {
        if (voiceLibrary.GetAudioClipById(id, out var audioClip))
            AudioPlayer.PlayClipStatic(audioClip);
    }
}
```
* Create runtime instance of VoiceLibrary:
```
using Charactr.VoiceSDK.Library;
using UnityEngine;

public class CreateVoiceLibraryInstance : MonoBehaviour
{
    private VoiceLibrary _voiceLibrary;
    
    void Start()
    {
        _voiceLibrary = ScriptableObject.CreateInstance<VoiceLibrary>();
        var id = _voiceLibrary.AddNewItem("Hello world", 151);
        DownloadClipForItem(id);
    }

    //Note: While working in editor mode, clips are saved to Project/Assets/Audio folder
    private async void DownloadClipForItem(int id)
    {
        await _voiceLibrary.AddAudioClip(id);
        _voiceLibrary.GetItemById(id, out var item);
        Debug.Log($"Clip downloaded: {item.AudioClip.length} seconds");
    }
}
```

## Sample code

This package contains two example scenes with both Convert API and Steaming API:

Use Charactr/Configuration tool menu to provide API access keys first!</i>

* Samples/[Streaming](https://github.com/charactr-platform/charactr-api-sdk-unity/tree/main/Samples~/Streaming) - TTS realtime streaming example with different voices switching
* Samples/[Convert](https://github.com/charactr-platform/charactr-api-sdk-unity/tree/main/Samples~/Convert) - Convert API tools for offline audio clips and dialogue creation

## Source code

The source code for this project is located at:
https://github.com/charactr-platform/charactr-api-sdk-unity

## Resources

* [Character Voice Studio](https://charactr.com/studio)
* [Documentation](https://docs.api.charactr.com/reference/about)
