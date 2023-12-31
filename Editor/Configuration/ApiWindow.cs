using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Configuration
{
    public class ApiWindow : EditorWindow
    {
        public VisualTreeAsset visualTreeAsset;
        private const string STUDIO_URL = "https://gemelo.ai/studio";
        private const string LOGO_GUID = "2f9da5dd0ddd7470a9ef00889bd59c13";
        private TextField _keyField, _clientField;

        private IMGUIContainer _logoContainer;

        [MenuItem("Tools/Gemelo.ai Voice/Configuration")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<ApiWindow>();
            wnd.titleContent = new GUIContent("Gemelo.ai Voice Configuration");
            wnd.Show();
        }

        [MenuItem("Tools/Gemelo.ai Voice/Update voice previews")]
        private static void UpdatePreviewsLibrary()
        {
            if (Voice.Configuration.Exists())
                Bootstrapper.InitializeLibrary();
            else
                EditorUtility.DisplayDialog("Error!", "Please set API Configuration first!", "OK");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;
            
            root.Add(visualTreeAsset.Instantiate());

            _clientField = root.Q<TextField>("ClientText");
            _keyField = root.Q<TextField>("ApiText");
            _logoContainer = root.Q<IMGUIContainer>("LogoContainer");
            var path = AssetDatabase.GUIDToAssetPath(LOGO_GUID);
            _logoContainer.style.backgroundImage = new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            
            var saveButton = root.Q<Button>("SaveButton");
            saveButton.RegisterCallback<MouseUpEvent>((e)=> SaveConfiguration());
            
            var linkButton = root.Q<Button>("LinkButton");
            linkButton.RegisterCallback<MouseUpEvent>((e)=> Application.OpenURL(STUDIO_URL));

            if (!Voice.Configuration.Exists())
                return;
            
            var config = Voice.Configuration.Load();
            _clientField.value = config.ApiClient;
            _keyField.value = config.ApiKey;
            saveButton.text = "Update";
        }

        private void SaveConfiguration()
        {
            var client = _clientField.value;
            var key = _keyField.value;

            if (string.IsNullOrEmpty(client) || string.IsNullOrEmpty(key))
                EditorUtility.DisplayDialog("Save error!", $"Please provide API details from {STUDIO_URL}", "OK");

            Voice.Configuration.Save(client, key);

            if (!Voice.Configuration.Exists())
            {
                EditorUtility.DisplayDialog("Save error!",
                    $"Can't save configuration, please check save path:\n{Voice.Configuration.SAVE_PATH}", "OK");
                Close();
                return;
            }

            if (EditorUtility.DisplayDialog("Congrats!", "Configuration saved!", "OK"))
            {
                UpdatePreviewsLibrary();
                Close();
            }
        }
    }
}