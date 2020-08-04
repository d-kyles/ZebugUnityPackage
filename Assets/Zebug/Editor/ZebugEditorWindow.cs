using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using ZebugProject;


public class ZebugEditorWindow : EditorWindow
{
    [MenuItem("Window/Zebug")]
    public static void ShowExample()
    {
        ZebugEditorWindow wnd = GetWindow<ZebugEditorWindow>();
        wnd.titleContent = new GUIContent("Zebug");
    }

    public void OnEnable()
    {
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

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Zebug/Editor/ZebugEditorWindow.uxml");
        VisualElement labelFromUXML = visualTree.CloneTree();
        root.Add(labelFromUXML);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Zebug/Editor/ZebugEditorWindow.uss");
        //labelWithStyle.styleSheets.Add(styleSheet);


        // // Mirror value of uxml field into the C# field.
        // csharpField.RegisterCallback<ChangeEvent<bool>>((evt) => {
        //     csharpField.value = evt.newValue;
        // });

        // Create some list of data, here simply numbers in interval [1, 1000]
        // const int itemCount = 1000;
        // var items = new List<string>(itemCount);
        // for (int i = 1; i <= itemCount; i++) {
        //     items.Add(i.ToString());
        // }


        var imTreeViewContainer = new IMGUIContainer();

        imTreeViewContainer.onGUIHandler += () => {

            //TreeView.


        };

        root.Add(imTreeViewContainer);


        var refreshButton = new Button(() => {
            rootVisualElement.Clear();
            this.OnEnable();
        }) {
          text = "Refresh Window"
        };
        root.Add(refreshButton);

        var zebugBase = Zebug.Instance;
        var items = Zebug.s_Channels;


        // The "makeItem" function will be called as needed
        // when the ListView needs more items to render
        Func<VisualElement> makeItem = () => {
            return new ZebugToggleElement();
        };

        // As the user scrolls through the list, the ListView object
        // will recycle elements created by the "makeItem"
        // and invoke the "bindItem" callback to associate
        // the element with the matching data item (specified as an index in the list)
        Action<VisualElement, int> bindItem = (e, i) => {
            if (e is ZebugToggleElement t) {
                IChannel zChannel = items[i];
                t.text = zChannel.Name();
                t.style.color = zChannel.GetColor();
                t.Channel = zChannel;
                t.value = zChannel.LogEnabled();
            }
        };

        var listView = root.Q<ListView>();
        listView.styleSheets.Add(styleSheet);
        var ss = listView.styleSheets;
        var s = listView.style;
        //listView.StretchToParentSize();
        listView.makeItem = makeItem;
        listView.bindItem = bindItem;
        listView.itemsSource = items;
        listView.selectionType = SelectionType.Multiple;

        // Callback invoked when the user double clicks an item
        listView.onItemChosen += obj => {
            Debug.Log(obj);
        };

        // Callback invoked when the user changes the selection inside the ListView
        listView.onSelectionChanged += objects => {
            Debug.Log(objects);
        };


        //root.Add(listView);

    }

    public class ZebugToggleElement : Toggle {
        public IChannel Channel;

        public ZebugToggleElement() {
            RegisterCallback<ChangeEvent<bool>>(OnValueChanged);
        }

        private void OnValueChanged(ChangeEvent<bool> evt) {
            if (Channel.LocalLogEnabled() != evt.newValue) {
                Channel.SetLogEnabled(evt.newValue);
            }
        }
    }
}
