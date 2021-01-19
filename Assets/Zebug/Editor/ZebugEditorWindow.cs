//  --- Zebug v0.3 ---------------------------------------------------------------------------------
//  Copyright (c) 2020 Dan Kyles
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software
//  and associated documentation files (the "Software"), to deal in the Software without
//  restriction, including without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
//  Software is furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all copies or
//  substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
//  BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//  DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ZebugProject;

public class ZebugEditorWindow : EditorWindow {

    [SerializeField] private ZebugEditorWindow m_Test;

    private const string kEditorLocation = "Assets/Zebug/Editor";
    // private const string kEditorLocation = "Assets/Plugins/Zebug/Editor";

    private const string kWindowElementName = "ZebugEditorWindow";
    private const string kWindowTreePath = kEditorLocation + "/" + kWindowElementName;
    private const string kWindowTreeLayout = kWindowTreePath + ".uxml";
    private const string kWindowTreeStyle = kWindowTreePath + ".uss";

    [MenuItem("Window/Zebug")]
    public static void ShowExample() {
        ZebugEditorWindow wnd = GetWindow<ZebugEditorWindow>();
        wnd.titleContent = new GUIContent("Zebug");
    }

    public void OnEnable() {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        root.style.marginTop = 4;
        root.style.marginLeft = 4;
        root.style.marginRight = 4;
        root.style.marginBottom = 4;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        VisualElement label = new Label("Zebug Editor");
        //  --- TODO(dan): Up to figuring out why this won't pick up the class name from the uss file
        label.AddToClassList("fancy-label");
        label.EnableInClassList("fancy-label", true);
        root.Add(label);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(kWindowTreeStyle);
        root.styleSheets.Add(styleSheet);

        // Import UXML
        var loadedEditorWindowTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(kWindowTreeLayout);
        VisualElement editorWindowLayout = loadedEditorWindowTree.CloneTree();
        root.Add(editorWindowLayout);

        if (Zebug.s_Channels == null || Zebug.s_Channels.Count == 0) {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            HashSet<Type> types = new HashSet<Type>();
            for (int i = 0; i < loadedAssemblies.Length; i++) {
                Assembly a = loadedAssemblies[i];
                foreach (Type type in a.GetTypes()) {
                    types.Add(type);
                }
            }

            foreach (Type type in types) {
                if (typeof(IChannel).IsAssignableFrom(type)
                    && !typeof(Channel<>).IsAssignableFrom(type)
                    && !type.IsInterface) {
                    //  --- Pre-populate the channels list
                    //      default constructor adds instance to the base ZebugInstance
                    Activator.CreateInstance(type);
                }
            }
        }

        Channel<Zebug> zebugBase = Zebug.Instance;
        List<IChannel> items = Zebug.s_Channels;

        ZebugChannelFoldout channelFoldout = new ZebugChannelFoldout((IChannel)zebugBase);
        var scrollPanel = root.Q<ScrollView>(null, "zebug-scroll-view");
        if (scrollPanel != null) {
            scrollPanel.Add(channelFoldout);
        } else {
            root.Add(channelFoldout);
        }

        var refreshButton = editorWindowLayout.Q<Button>(null, "zebug-refresh-window-button");
        refreshButton.clicked += () => {
            rootVisualElement.Clear();
            OnEnable();
        };
    }
}