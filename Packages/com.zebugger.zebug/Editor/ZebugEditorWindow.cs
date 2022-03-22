//  --- Zebug --------------------------------------------------------------------------------------
//  Copyright (c) 2022 Dan Kyles
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

namespace ZebugProject {

    using static ZebugEditorUtils;

    public class ZebugEditorWindow : EditorWindow {

        //[SerializeField] private ZebugEditorWindow m_Test;

        //private const string kEditorUIFolder = "Editor/UI/";

        // private const string kWindowElementName = "ZebugEditorWindow";
        // private const string kWindowTreePath = kWindowElementName;
        // private const string kWindowTreeLayout = kWindowTreePath + ".uxml";
        // private const string kWindowTreeStyle = kWindowTreePath + ".uss";

        private static ZebugEditorWindow s_Window;

        [MenuItem("Window/Zebug")]
        public static void ShowExample() {
            ZebugEditorWindow wnd = GetWindow<ZebugEditorWindow>();
            wnd.titleContent = new GUIContent("Zebug");
            s_Window = wnd;
        }

        [InitializeOnLoadMethod]
        protected static void InitializeOnLoad() {
            if (s_Window != null) {
                s_Window.rootVisualElement.Clear();
                s_Window.OnEnable();
            }
        }

        protected void OnEnable() {
            // Each editor window contains a root VisualElement object
            // VisualElement root = rootVisualElement;
            // root.style.marginTop = 4;
            // root.style.marginLeft = 4;
            // root.style.marginRight = 4;
            // root.style.marginBottom = 4;

            // var styleSheet = LoadFromZebugRelative<StyleSheet>(kEditorUIFolder + kWindowTreeStyle);
            // root.styleSheets.Add(styleSheet);
            //
            // // Import UXML
            // var loadedEditorWindowTree = LoadFromZebugRelative<VisualTreeAsset>(kEditorUIFolder + kWindowTreeLayout);
            // VisualElement editorWindowLayout = loadedEditorWindowTree.CloneTree();
            // root.Add(editorWindowLayout);

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

            // ZebugChannelFoldout channelFoldout = new ZebugChannelFoldout((IChannel)zebugBase);
            // var scrollPanel = root.Q<ScrollView>(null, "zebug-scroll-view");
            // if (scrollPanel != null) {
            //     scrollPanel.Add(channelFoldout);
            // } else {
            //     root.Add(channelFoldout);
            // }

            // var imgui = root.Q<IMGUIContainer>();
            // imgui.onGUIHandler += OnIMGUI;
        }

        private bool settingsFolded;

        protected void OnIMGUI() {
            // if (GUILayout.Button("Settings")) {
            //     settingsFolded = !settingsFolded;
            // }
            //
            // if (!settingsFolded) {
            //     GUILayout.Label("Thing");
            // }
        }

        private static Dictionary<IChannel, bool> s_ChannelExpanded = new Dictionary<IChannel, bool>();

        private void OnGUI() {
            Channel<Zebug> zebugBase = Zebug.Instance;

            EditorGUILayout.LabelField("Converted to IMGUI. Disappointing about UIElements being half-baked :(");

            DrawChannel(zebugBase);

            // maybe?
                    //EditorGUILayout.BeginFoldoutHeaderGroup()
                    // EditorGUIUtility.hierarchyMode

            void DrawChannel(IChannel channel) {
                using (new GUILayout.VerticalScope()) {
                    bool channelExpanded = false;
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox)) {

                        IList<IChannel> children = channel.Children();
                        if (children.Count > 0) {
                            GUIStyle foldoutTextStyle = new GUIStyle(EditorStyles.foldout);
                            {
                                foldoutTextStyle.normal.textColor =
                                foldoutTextStyle.onNormal.textColor = channel.GetColor();
                            }

                            if (!s_ChannelExpanded.TryGetValue(channel, out bool expanded)) {
                                s_ChannelExpanded.Add(channel, false);
                            }

                            channelExpanded = EditorGUILayout.Foldout(expanded, channel.Name(), true, foldoutTextStyle);
                            if (channelExpanded != expanded) {
                                s_ChannelExpanded[channel] = channelExpanded;
                            }
                        } else {
                            var foldoutTextStyle = new GUIStyle();
                            foldoutTextStyle.normal.textColor = channel.GetColor();
                            foldoutTextStyle.onNormal.textColor = channel.GetColor();
                            foldoutTextStyle.contentOffset = new Vector2(30 * EditorGUI.indentLevel,0);
                            GUILayout.Label(channel.Name(), foldoutTextStyle);
                        }
                        GUILayout.FlexibleSpace();

                        const float disabledColorVal = 1f/255f;
                        Color disabledTextColor = new Color(disabledColorVal, disabledColorVal,disabledColorVal);

                        const float togglesWidth = 150f;
                        using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
                        using (new GUILayout.HorizontalScope()) {

                            var toggleTextStyle = new GUIStyle();
                            var normalTextColor = toggleTextStyle.normal.textColor;
                            if (!channel.ParentLogEnabled()) {
                                toggleTextStyle.normal.textColor =
                                    toggleTextStyle.onNormal.textColor = disabledTextColor;
                            }

                            //using (new EditorGUI.DisabledScope(!channel.ParentLogEnabled())) {
                            bool logEnabled = channel.LocalLogEnabled();
                            bool newLogEnabled = EditorGUILayout.ToggleLeft("Log", logEnabled, GUILayout.Width(togglesWidth/2));
                            if (newLogEnabled != logEnabled) {
                                channel.SetLogEnabled(newLogEnabled);
                            }
                            //}

                            if (!channel.ParentGizmosEnabled()) {
                                toggleTextStyle.normal.textColor =
                                    toggleTextStyle.onNormal.textColor = disabledTextColor;
                            } else {
                                toggleTextStyle.normal.textColor
                                    = toggleTextStyle.onNormal.textColor
                                    = normalTextColor;
                            }

                            using (new EditorGUI.DisabledScope(!channel.ParentGizmosEnabled())) {
                                bool gizmosEnabled = channel.LocalGizmosEnabled();
                                bool newGizmosEnabled = EditorGUILayout.ToggleLeft("Gizmos", gizmosEnabled, GUILayout.Width(togglesWidth/2));
                                if (newGizmosEnabled != gizmosEnabled) {
                                    channel.SetGizmosEnabled(newGizmosEnabled);
                                }
                            }
                        }
                    }

                    if (channelExpanded) {
                        using (new EditorGUI.IndentLevelScope(1))
                        using (new GUILayout.VerticalScope()) {
                            foreach (IChannel child in channel.Children()) {
                                DrawChannel(child);
                            }
                        }
                    }
                }
            }
        }
    }
}