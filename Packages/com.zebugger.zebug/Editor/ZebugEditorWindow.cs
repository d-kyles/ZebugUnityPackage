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
using System.Linq;
using System.Reflection;
using Codice.CM.Common;
using UnityEditor;
using UnityEngine;

namespace ZebugProject {

    public class ZebugEditorWindow : EditorWindow {

        private static ZebugEditorWindow s_Window;

        [MenuItem("Window/Zebug")]
        public static void ShowWindow() {
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

        private static Dictionary<IChannel, bool> s_ChannelExpanded = new Dictionary<IChannel, bool>();
        private static int s_ExpandedCount = 0; 
        private GUIStyle _channelRowStyleTop;
        private GUIStyle _channelRowStyleInner;
        private GUIStyle _channelRowStyleBottom;

        private const string kAllOnPreprocessor = "ZEBUG_ALL_ON"; 
        private bool _preprocessorAllOnSet;
        private float _lastFetchedPreprocessorTime;
        private string[] _symbols;


        protected void OnEnable() {

            _lastFetchedPreprocessorTime = 0;
            
            // ZebugEditorUtils.LoadFromZebugRelative Packages/com.zebugger.zebug or Assets/Plugins/Zebug/
    
            ZebugPreferences thing =  ZebugPreferences.Instance;
            
            if (Zebug.s_Channels == null || Zebug.s_Channels.Count == 0) {
                
                Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                HashSet<Type> types = new HashSet<Type>();
                for (int i = 0; i < loadedAssemblies.Length; i++) {
                    Assembly a = loadedAssemblies[i];
                    foreach (Type type in a.GetTypes()) {
                        types.Add(type);
                    }
                }
                
                //    = TypeCache.GetTypesDerivedFrom<IChannel>()
                //        .Where(x => !x.IsConstructedGenericType)?

                foreach (Type type in types) {
                    if (typeof(IChannel).IsAssignableFrom(type)
                        && !typeof(Channel<>).IsAssignableFrom(type)
                        && !type.IsInterface) {
                        //  --- Pre-populate the channels list
                        //      default constructor adds instance to the base ZebugInstance
                        
                        IChannel channel = (IChannel)Activator.CreateInstance(type);
                        
                        if (!s_ChannelExpanded.ContainsKey(channel))
                        {
                            s_ChannelExpanded.Add(channel, true);
                        } 
                        
                        if (channel.FullName() == "ZebugBase")
                        {
                            s_ChannelExpanded[channel] = true;
                            s_ExpandedCount = 1;
                        }
                    }
                }
            }
            
            try
            {
                _channelRowStyleTop = new GUIStyle(EditorStyles.helpBox);    
                _channelRowStyleTop.margin = new RectOffset(-1,-1,-1,-1);
                Texture2D backgroundTextureOuter = Resources.Load<Texture2D>("ZebugBackgroundBox_Top");
                _channelRowStyleTop.normal.background = backgroundTextureOuter;
            
                _channelRowStyleInner = new GUIStyle(_channelRowStyleTop);
                Texture2D backgroundTextureInner = Resources.Load<Texture2D>("ZebugBackgroundBox_Inner");
                _channelRowStyleInner.normal.background = backgroundTextureInner;
            
                _channelRowStyleBottom = new GUIStyle(_channelRowStyleTop);
                Texture2D backgroundTextureBottom = Resources.Load<Texture2D>("ZebugBackgroundBox_Bottom");
                _channelRowStyleBottom.normal.background = backgroundTextureBottom;
            } 
            catch (NullReferenceException)
            {
                //  --- NOTE(dan): Shortly after recompiling, EditorStyles.helpBox doesn't exist.
                //                 Not sure how to avoid this.
            }
        }

        private void OnGUI() {
            Channel<Zebug> zebugBase = Zebug.Instance;

            var lineColor = new Color(0.34f, 0.34f, 0.34f);
            
            if (GUILayout.Button("Refresh Window"))
            {
                OnEnable();
            }
            
            ZebugGUIStyles.Line(lineColor, 2);
            
            int currentChannel = 0;
            int visibleChannelCount = s_ExpandedCount;
            
            s_ExpandedCount = 0;
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.grey;
            GUI.backgroundColor = Color.white;
            GUILayout.Space(5);
            GUILayout.Label("Channels", EditorStyles.largeLabel);
            DrawChannel(zebugBase);
            GUI.backgroundColor = oldColor;

            GUILayout.Space(5);
            ZebugGUIStyles.Line(lineColor, 2);
            
            GUILayout.Space(5);
            GUILayout.Label("Preprocessor Directives", EditorStyles.largeLabel);
            
            bool oldValue = false;
            bool newValue = false;
            
            using (new GUILayout.HorizontalScope())
            {
                if (_symbols == null || Time.time - _lastFetchedPreprocessorTime > 2f)
                {
                    UpdatePreprocessorStuff();
                }                    
                
                using (new GUILayout.VerticalScope())
                {
                    oldValue = _preprocessorAllOnSet; 
                    newValue = GUILayout.Toggle(_preprocessorAllOnSet, "Force All On");
                    if (newValue != oldValue)
                    {
                        SetPreprocessorString(kAllOnPreprocessor, newValue);
                        UpdatePreprocessorStuff();
                    }
                    
                    // oldValue = _preprocessorForceToDefault; 
                    // newValue = GUILayout.Toggle(_preprocessorForceToDefault, "Force to default");
                    // if (newValue != oldValue)
                    // {
                    //     SetPreprocessorString(kForceToDefault, newValue);
                    //     UpdatePreprocessorStuff();
                    // }
                }
                
                
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Scripting Define Symbols");
                    using (new GUILayout.VerticalScope(EditorStyles.textArea))
                    {
                        for (int sIdx = 0; sIdx < _symbols.Length; sIdx++)
                        {
                            string symbol = _symbols[sIdx];
                            GUILayout.Label(symbol);
                        }
                    }
                }
                
                void UpdatePreprocessorStuff()
                {
                    BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
                    BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
                    string symbolString = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                    string[] symbolArray = symbolString.Split(';');
                    _symbols = symbolArray;
                    _preprocessorAllOnSet = HasPreprocessorString(kAllOnPreprocessor);
                    _lastFetchedPreprocessorTime = Time.time;
                }
                
                bool HasPreprocessorString(string target)
                {
                    bool result = _symbols.Contains(target); 
                    return result;
                }
                
                void SetPreprocessorString(string target, bool targetEnabled)
                {
                    bool existing = HasPreprocessorString(target);
                    if (existing == targetEnabled)
                    {
                        return;
                    }
                    
                    if (targetEnabled)
                    {
                        // adding
                        Array.Resize(ref _symbols, _symbols.Length+1);
                        _symbols[_symbols.Length-1] = target;
                    } 
                    else
                    {
                        // swap with back element
                        int idx = Array.FindIndex(_symbols, x=> x == target);
                        _symbols[idx] = _symbols[_symbols.Length-1];
                        Array.Resize(ref _symbols, _symbols.Length-1);
                    }
                    BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
                    BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, _symbols);
                }
                
            }
            
            // maybe?
                    //EditorGUILayout.BeginFoldoutHeaderGroup()
                    //EditorGUIUtility.hierarchyMode

            void DrawChannel(IChannel channel) {
                
                s_ExpandedCount++;
                
                var style = _channelRowStyleInner;
                if (currentChannel == 0)
                {
                    style = _channelRowStyleTop;
                } 
                else if (currentChannel == visibleChannelCount-1)
                {
                    style = _channelRowStyleBottom;
                }
                currentChannel++;
                
                using (new GUILayout.VerticalScope()) {
                    bool channelExpanded = false;
                    
                    using (new GUILayout.HorizontalScope(style)) {

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

                            using (new EditorGUI.DisabledScope(!channel.ParentLogEnabled())) {
                                bool logEnabled = channel.LocalLogEnabled();
                                bool newLogEnabled = EditorGUILayout.ToggleLeft("Log", logEnabled, GUILayout.Width(togglesWidth/2));
                                if (newLogEnabled != logEnabled) {
                                    channel.SetLogEnabled(newLogEnabled);
                                }
                            }

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
            
            ZebugGUIStyles.Line(lineColor, 2);
            
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("New channels enabled by default?");
                    
                    oldValue = ZebugPreferences.Instance.ChannelsEnabledByDefault; 
                    newValue = EditorGUILayout.Toggle("", oldValue);
                    if (newValue != oldValue)
                    {
                        ZebugPreferences.Instance.ChannelsEnabledByDefault = newValue;
                    }
                }
                
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Clear unused channel data");
                    if (GUILayout.Button("Clear"))
                    {
                        HashSet<string> channels = new HashSet<string>();
                        AddChannels(Zebug.Instance, channels);
                        
                        static void AddChannels(IChannel channel, HashSet<string> list)
                        {
                            list.Add(channel.FullName());
                            IList<IChannel> children = channel.Children();
                            for (int idx = 0; idx < children.Count; idx++)
                            {
                                AddChannels(children[idx], list);
                            }
                        }
                        
                        List<string> keysToRemove = new List<string>(); 
                        foreach (KeyValuePair<string, ChannelPreference> kvp in ZebugPreferences.Instance.Data)
                        {
                            string channelName = kvp.Key;
                            if (!channels.Contains(channelName))
                            {
                                keysToRemove.Add(channelName);
                            }
                        }
                        
                        //  --- Mustn't modify data while iterating 
                        foreach (string key in keysToRemove)
                        {
                            ZebugPreferences.RemoveChannelData(key);
                        }
                    }
                }
            }
            
        }
    }
    
    //Class to hold custom gui styles
    public static class ZebugGUIStyles
    {
        private static GUIStyle _lineStyle;
 
        //constructor
        static ZebugGUIStyles()
        {
            _lineStyle = new GUIStyle("box");
            _lineStyle.border.top = _lineStyle.border.bottom = 1;
            _lineStyle.margin.top = _lineStyle.margin.bottom = 1;
            _lineStyle.padding.top = _lineStyle.padding.bottom = 1;
            _lineStyle.border.left = _lineStyle.border.right = 0;
            _lineStyle.margin.left = _lineStyle.margin.right = 0;
            _lineStyle.padding.left = _lineStyle.padding.right = 0;
            _lineStyle.normal.background = EditorGUIUtility.whiteTexture;
        }
     
        public static void Line(Color color, float height = 1f)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Space(3);
            GUILayout.Box( GUIContent.none
                         , _lineStyle
                         , GUILayout.ExpandWidth(true)
                         , GUILayout.Height(height));
            GUILayout.Space(3);
            GUI.color = oldColor;
        }
        
    }
    
    
}