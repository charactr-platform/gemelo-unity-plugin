using Charactr.VoiceSDK.SDK;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Charactr.SDK.Editor
{
    public class ApiConfigurationWindow : EditorWindow
    {
        private const string API_URL = "https://api.charactr.com";
        private TextField _keyField, _clientField;
        
        [MenuItem("Charactr/ApiConfigurationWindow")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<ApiConfigurationWindow>();
            wnd.titleContent = new GUIContent("ApiConfigurationWindow"); ;
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Charactr/Editor/Configuration/ApiConfigurationWindow.uxml");
            VisualElement labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);

            _clientField = root.Q<TextField>("ClientText");
            _keyField = root.Q<TextField>("ApiText");
            
            var linkButton = root.Q<Button>("LinkButton");
            linkButton.RegisterCallback<MouseUpEvent>((e)=> Application.OpenURL(API_URL));

            var saveButton = root.Q<Button>("SaveButton");
            saveButton.RegisterCallback<MouseUpEvent>((e)=> SaveConfiguration());
        }

        private void SaveConfiguration()
        {
            var client = _clientField.value;
            var key = _keyField.value;

            if (string.IsNullOrEmpty(client) || string.IsNullOrEmpty(key))
                EditorUtility.DisplayDialog("Save error!", $"Please provide api details from {API_URL}", "OK");
            
            Configuration.Save(client, key);

            if (Configuration.Load() != null)
                EditorUtility.DisplayDialog("Congrats!", "Configuration saved!", "OK");
            else
                EditorUtility.DisplayDialog("Save error!",
                    $"Can't save configuration, please check save path:\n{Configuration.SAVE_PATH}", "OK");
        }
    }
}