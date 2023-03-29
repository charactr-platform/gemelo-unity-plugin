using System.Collections.Generic;
using Charactr.SDK.Library;
using Charactr.VoiceSDK.SDK;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


namespace Charactr.SDK.Editor.Library
{
    public class LibraryEditorWindow : EditorWindow
    {
        public VisualTreeAsset visualTreeAsset;
        private Button _button;
        private VisualElement _root;
        private List<ObjectField> _libraryObjects;
        
        [MenuItem("Charactr/LibraryEditor")]
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
            VisualElement labelFromUXML = visualTreeAsset.Instantiate();
            _root.Add(labelFromUXML);

            _root.Q<Button>("createButton").RegisterCallback<ClickEvent>((e) => OnCreateButton());

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Charactr/Editor/Library/LibraryEditorWindow.uss");
            VisualElement labelWithStyle = new Label("Hello World! With Style");
            _root.Add(labelWithStyle);
            LoadItems();
        }

        private void OnCreateButton()
        {
            VoiceLibrary.Create();
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
            }

            _libraryObjects = new List<ObjectField>();
            
            foreach (var library in listOfLibraryObjects)
            {
                var of = new ObjectField($" {count++} - Items: {library.Items.Count}")
                {
                    objectType = typeof(VoiceLibrary),
                    value = library,
                    focusable = false,
                };
                of.RegisterCallback<ClickEvent>((e) => {Debug.Log("Clicked on "+ library.name); });
                _libraryObjects.Add(of);
                _root.Add(of);
            }
        }
    }
}