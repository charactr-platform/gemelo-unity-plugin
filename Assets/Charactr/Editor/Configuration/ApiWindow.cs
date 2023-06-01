using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Charactr.VoiceSDK.Editor.Configuration
{
    public class ApiWindow : EditorWindow
    {
        public VisualTreeAsset visualTreeAsset;
        private const string API_URL = "https://api.charactr.com";
        private TextField _keyField, _clientField;

        [MenuItem("Charactr/Configuration")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<ApiWindow>();
            wnd.titleContent = new GUIContent("Charactr Voice SDK API Configuration");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;
            
            root.Add(visualTreeAsset.Instantiate());

            _clientField = root.Q<TextField>("ClientText");
            _keyField = root.Q<TextField>("ApiText");

            var saveButton = root.Q<Button>("SaveButton");
            saveButton.RegisterCallback<MouseUpEvent>((e)=> SaveConfiguration());
            
            var linkButton = root.Q<Button>("LinkButton");
            linkButton.RegisterCallback<MouseUpEvent>((e)=> Application.OpenURL(API_URL));
            
            var config = VoiceSDK.Configuration.Load();
            
            if (config != null)
            {
                _clientField.value = config.ApiClient;
                _keyField.value = config.ApiKey;
                saveButton.text = "Update";
            }
        }

        private void SaveConfiguration()
        {
            var client = _clientField.value;
            var key = _keyField.value;

            if (string.IsNullOrEmpty(client) || string.IsNullOrEmpty(key))
                EditorUtility.DisplayDialog("Save error!", $"Please provide api details from {API_URL}", "OK");
            
            VoiceSDK.Configuration.Save(client, key);

            if (VoiceSDK.Configuration.Load() != null)
                EditorUtility.DisplayDialog("Congrats!", "Configuration saved!", "OK");
            else
                EditorUtility.DisplayDialog("Save error!",
                    $"Can't save configuration, please check save path:\n{VoiceSDK.Configuration.SAVE_PATH}", "OK");
        }
    }
}