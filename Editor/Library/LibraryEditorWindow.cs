using System.Collections.Generic;
using Gemelo.Voice.Library;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gemelo.Voice.Editor.Library
{
    public class LibraryEditorWindow : EditorWindow
    {
        public VisualTreeAsset visualTreeAsset;
        
        private VisualElement _root;
        private List<ObjectField> _libraryObjects;
        private Button _deleteButton, _openButton;
        
        [MenuItem("Tools/VoiceSDK/LibraryEditor")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<LibraryEditorWindow>();
            wnd.titleContent = new GUIContent("LibraryEditor");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            _root = rootVisualElement;
         
            // Import UXML
            var labelFromUxml = visualTreeAsset.Instantiate();
            _root.Add(labelFromUxml);

            _root.Q<Button>("createButton").RegisterCallback<ClickEvent>((e) => OnCreateButton());

            LoadItems();
        }

        private void OnCreateButton()
        {
            var instance = CreateInstance<VoiceLibrary>();
            Voice.Configuration.CheckForResourcesDir();
            var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{VoiceLibrary.SAVE_PATH}/VoiceLibrary.asset");
            AssetDatabase.CreateAsset(instance, path);
            LoadItems();
        }
        
        private void LoadItems()
        {
            var listOfLibraryObjects = Resources.LoadAll<VoiceLibrary>("");
            var count = 0;
            
            if (_libraryObjects?.Count > 0)
            {
                foreach (var libraryObject in _libraryObjects)
                {
                    _root.Remove(libraryObject);
                }
                
                RemoveOpenButton();
                RemoveDeleteButton();
            }

            _libraryObjects = new List<ObjectField>();
            
            foreach (var library in listOfLibraryObjects)
            {
                var of = new ObjectField($" {count++} - Added items: {library.Items.Count}")
                {
                    objectType = typeof(VoiceLibrary),
                    value = library,
                    focusable = false,
                    style = { marginTop = new StyleLength(5)}
                };
                
                of.RegisterCallback<ClickEvent>((e) => AddContextButtons(library));
                _libraryObjects.Add(of);
                _root.Add(of);
            }
        }

        private void AddContextButtons(VoiceLibrary library)
        {
            Debug.Log("Clicked on "+ library.name);

            RemoveDeleteButton();
            RemoveOpenButton();
            
            AddOpenButton(library);
            AddDeleteButton(library);
        }
        
        private void AddDeleteButton(VoiceLibrary library)
        {
            _deleteButton = new Button(() =>
            {
                var path = AssetDatabase.GetAssetPath(library);
                if (AssetDatabase.DeleteAsset(path))
                {
                    LoadItems();
                }
            })
            {
                text = $"Delete: {library.name}",
                style = { marginTop = new StyleLength(5),}
            };
            
            _root.Add(_deleteButton);
        }
        
        private void AddOpenButton(VoiceLibrary library)
        {
            _openButton = new Button(() =>
            {
                Selection.objects = new Object[] {library};
            })
            {
                text = $"Open in inspector: {library.name}",
                style = { marginTop = new StyleLength(5)}
            };
            
            _root.Add(_openButton );
        }

        private void RemoveDeleteButton()
        {
            if (_deleteButton != null)
            {
                _root.Remove(_deleteButton);
                _deleteButton = null;
            }
        }
        
        private void RemoveOpenButton()
        {
            if (_openButton != null)
            {
                _root.Remove(_openButton);
                _openButton = null;
            }
        }
    }
}