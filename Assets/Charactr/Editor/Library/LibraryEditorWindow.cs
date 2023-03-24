using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;


namespace Charactr.SDK.Editor.Library
{
    public class LibraryEditorWindow : EditorWindow
    {
        public VisualTreeAsset visualTreeAsset;
    
        [MenuItem("Charactr/LibraryEditor")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<LibraryEditorWindow>();
            wnd.titleContent = new GUIContent("LibraryEditor");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            VisualElement label = new Label("Hello World! From C#");
            root.Add(label);

            // Import UXML
            VisualElement labelFromUXML = visualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Charactr/Editor/Library/LibraryEditorWindow.uss");
            VisualElement labelWithStyle = new Label("Hello World! With Style");
            labelWithStyle.styleSheets.Add(styleSheet);
            root.Add(labelWithStyle);
        }
    }
}