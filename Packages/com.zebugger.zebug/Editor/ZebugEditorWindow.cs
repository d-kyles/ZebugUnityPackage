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
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace ZebugProject {

    public class ZebugEditorWindow : EditorWindow {

        private static ZebugEditorWindow s_Window;
        public static ZebugEditorWindow Window => s_Window;

        [MenuItem("Tools/Zebug")]
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

        [SerializeField] private ExpandedChannelsSet _channelExpandedSet = new();  
        
        [Serializable]
        private class ExpandedChannelsSet : Dictionary<string, bool>, ISerializationCallbackReceiver
        {
            [SerializeField, HideInInspector] private List<string> _keys = new();
            [SerializeField, HideInInspector] private List<bool> _values = new();

            public void OnBeforeSerialize()
            {
                _keys.Clear();
                _keys.AddRange(Keys);
            
                _values.Clear();
                _values.AddRange(Values);
            }

            public void OnAfterDeserialize()
            {
                Clear();
                int count = _keys.Count;
 
                for(int i = 0; i < count; i++)
                {
                    Add(_keys[i], _values[i]);
                }
            }
        }

        private static HashSet<IChannel> s_TestChannels = new();
        private static Dictionary<IChannel, ChannelCacheData> s_ChannelCacheData = new();
        
        private class ChannelCacheData
        {
            public GUIStyle togglesLineStyle;
            public GUIContent labelContent;
            public GUIContent logLabelContent;
            public GUIContent gizmosLabelContent;
            public GUILayoutOption[] toggleWidthOptions;
            public Rect logLabelRect;
            public Rect gizmoLabelRect;
        }
        
        private static int s_ExpandedCount;
        private GUIStyle _channelRowStyleTop;
        private GUIStyle _channelRowStyleInner;
        private GUIStyle _channelRowStyleBottom;
        
        private Vector2 _scrollPosition;
        
        private const string kShowTestChannelsPref = "ZebugShowTestChannels";
        
        private bool _advOptionsExpanded;
        private bool _graphsExpanded;
        private bool _windowVarsExpanded;
        
        private bool _showTestChannels;
        
        private bool s_StylesLoaded;

        protected void OnEnable() {

            // ZebugEditorUtils.LoadFromZebugRelative Packages/com.zebugger.zebug or Assets/Plugins/Zebug/

            //  --- Make sure preferences are loaded 
            var _ = ZebugPreferences.Instance;

            if (Zebug.s_Channels == null || Zebug.s_Channels.Count == 0) {
                
                PrepopulateChannelsFromTypes(ref _channelExpandedSet);
                
                foreach (KeyValuePair<string,bool> kvp in _channelExpandedSet)
                {
                    s_ExpandedCount += kvp.Value ? 1 : 0;
                }
            }
            
            LoadStyles();
        }
        
        //  ----------------------------------------------------------------------------------------
        
        private static void PrepopulateChannelsFromTypes(ref ExpandedChannelsSet channelExpandedSet)
        {
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<IChannel>();
            foreach (Type type in types)
            {
                if (typeof(Channel<>).IsAssignableFrom(type)
                    || type.IsConstructedGenericType)
                {
                    continue;
                }
                //  --- Pre-populate the channels list
                //      default constructor adds instance to the base ZebugInstance
                        
                var propInfo = type.BaseType.GetProperty
                (
                    name: "Instance",
                    bindingAttr: BindingFlags.Public | BindingFlags.Static
                );
                IChannel channel = (IChannel)propInfo.GetValue(null); 

                string fullName = channel.FullName();
                bool isBase = fullName == "ZebugBase";
                if (isBase)
                {
                    //  --- Activator.CreateInstance bypasses the normal construction, and
                    //      Zebug.Instance may already have been called in the constructors
                    //      of other child channels, when they link to the hierarchy.
                    //      the channel we just made won't cause issues just lying around.
                    //      As it's editor window only.
                    channel = Zebug.Instance;
                }
                        
                if (!channelExpandedSet.ContainsKey(fullName))
                { 
                    //  --- Default to expanding to show new channels
                    channelExpandedSet.Add(fullName, true);
                }  
                        
                if (type.AssemblyQualifiedName.Contains("EditorTests"))
                {
                    s_TestChannels.Add(channel); 
                }
            }
        }

        //  ----------------------------------------------------------------------------------------
        
        private void LoadStyles()
        {
            try
            {
                _channelRowStyleTop = new GUIStyle(EditorStyles.helpBox);
                _channelRowStyleTop.margin = new RectOffset(-1, -1, -1, -1);
                Texture2D backgroundTextureOuter = Resources.Load<Texture2D>("ZebugBackgroundBox_Top");
                _channelRowStyleTop.normal.background = backgroundTextureOuter;

                _channelRowStyleInner = new GUIStyle(_channelRowStyleTop);
                Texture2D backgroundTextureInner = Resources.Load<Texture2D>("ZebugBackgroundBox_Inner");
                _channelRowStyleInner.normal.background = backgroundTextureInner;

                _channelRowStyleBottom = new GUIStyle(_channelRowStyleTop);
                Texture2D backgroundTextureBottom = Resources.Load<Texture2D>("ZebugBackgroundBox_Bottom");
                _channelRowStyleBottom.normal.background = backgroundTextureBottom;

                
                s_StylesLoaded = _channelRowStyleTop != null;
            } 
            catch (NullReferenceException)
            {
                //  --- NOTE(dan): Shortly after recompiling, EditorStyles.helpBox doesn't exist.
                //                 Not sure how to avoid this.
            }
        }

        private void CheckInit()
        {
            Zebug.EditorNeedsRepaint = OnEditorNeedsRepaint;
            
            if (!PlayerPrefs.HasKey(kShowTestChannelsPref))
            {
                PlayerPrefs.SetInt(kShowTestChannelsPref, 0);
            }
        }
        
        private void OnEditorNeedsRepaint()
        {
            Repaint();
        }
        
        //  ----------------------------------------------------------------------------------------
        
        #region OnGUI
        
        private void OnGUI() 
        {
            Profiler.BeginSample("Zebug IMGUI - Perf Tip: Fold away all the things.");
            
            CheckInit();
            
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            int currentChannel = 0;
            int visibleChannelCount = s_ExpandedCount;
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.grey;
            GUI.backgroundColor = Color.white;
            
            //  --- Channel Toggle List ---------------------------------------
            
            GUILayout.Label("Channels", EditorStyles.largeLabel);
            
            DrawChannel(Zebug.Instance, visibleChannelCount, ref currentChannel);
            GUI.backgroundColor = oldColor;

            s_ExpandedCount = currentChannel;
            
            ZebugGUIStyles.Line();
            
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("New channels enabled by default?");
                    
                bool oldValue = ZebugPreferences.Instance.ChannelsEnabledByDefault; 
                bool newValue = EditorGUILayout.Toggle("", oldValue);
                if (newValue != oldValue)
                {
                    ZebugPreferences.Instance.ChannelsEnabledByDefault = newValue;
                }
            }
            
            //  --- Graphs ----------------------------------------------------
            
            ZebugGUIStyles.Line();

            _graphsExpanded = EditorGUILayout.Foldout( _graphsExpanded
                                                     , "Graphs"
                                                     , toggleOnLabelClick: true);
            if (_graphsExpanded)
            {
                DrawGraphs();
            }
            
            //  --- Adv Options -----------------------------------------------
            
            
            ZebugGUIStyles.Line();
                        
            _advOptionsExpanded = EditorGUILayout.Foldout(_advOptionsExpanded
                                                         , "Advanced Options"
                                                         , toggleOnLabelClick: true);
            if (_advOptionsExpanded)
            {
                DrawAdvancedOptions();
            }
            
            _windowVarsExpanded = EditorGUILayout.Foldout(_windowVarsExpanded
                                                         , "Variables"
                                                         , toggleOnLabelClick: true);
            if (_windowVarsExpanded) {
                DrawWindowVars();
            }
            
            GUILayout.EndScrollView();
            
            Profiler.EndSample();
        }

        #endregion OnGUI
        
        //   ---------------------------------------------------------------------------------------
        
        #region Draw Channel
        
        void DrawChannel(IChannel channel, int visibleChannelCount, ref int currentChannel) {
            
            string channelName = channel.Name();
            string channelPath = channel.FullName();
            IList<IChannel> children = channel.Children();
            bool isFoldoutLine = children.Count > 0;
            
            const float togglesWidth = 150f;
            const float channelLineHeight = 23f;
            const int indentPaddingSize = 16;
            
            Color color = channel.GetColor();
            
            if (!s_StylesLoaded)
            {
                LoadStyles();
            }
            
            var style = _channelRowStyleInner;
            if (currentChannel == 0)
            {
                style = _channelRowStyleTop;
            } 
            else if (currentChannel == visibleChannelCount-1)
            {
                style = _channelRowStyleBottom;
            }
            
            bool channelExpanded = false;
            
            // creating a horizontal scope with a style is allocating 900 bytes each time :(
            
            using (new GUILayout.HorizontalScope(style)) {

                if (!s_ChannelCacheData.TryGetValue(channel, out ChannelCacheData cache))
                {
                    cache = new ChannelCacheData();
                    
                    if (isFoldoutLine) {
                        cache.togglesLineStyle = new GUIStyle(EditorStyles.foldout);
                    } else {
                        cache.togglesLineStyle = new GUIStyle();
                        cache.togglesLineStyle.padding = new RectOffset(indentPaddingSize * EditorGUI.indentLevel, 0, 0, 0);
                        cache.togglesLineStyle.fontSize = 12;
                        cache.togglesLineStyle.alignment = TextAnchor.MiddleLeft;
                        cache.togglesLineStyle.fixedHeight = channelLineHeight - 5;
                    }
                    cache.togglesLineStyle.normal.textColor = color;
                    cache.togglesLineStyle.onNormal.textColor = color;
                    cache.togglesLineStyle.focused.textColor = color;
                    cache.togglesLineStyle.onFocused.textColor = color;
                    
                    cache.labelContent = new GUIContent(channelName);
                    
                    cache.logLabelContent = new GUIContent("Log");
                    cache.gizmosLabelContent = new GUIContent("Gizmos");
                    
                    cache.toggleWidthOptions = new GUILayoutOption[1];
                    cache.toggleWidthOptions[0] = GUILayout.Width(togglesWidth/2);
                    
                    s_ChannelCacheData.Add(channel, cache);
                }
                
                if (isFoldoutLine) {
                   
                    _channelExpandedSet.TryGetValue(channelPath, out bool expanded);

                    channelExpanded = EditorGUILayout.Foldout(expanded,
                                                              channelName,
                                                              true,
                                                              cache.togglesLineStyle);
                    _channelExpandedSet[channelPath] = channelExpanded;
                    
                } else {
                    GUILayout.Label(cache.labelContent, cache.togglesLineStyle);
                }
                
                if (currentChannel == 0)
                {
                    GUILayout.FlexibleSpace();
                }
                
                using (new GUILayout.HorizontalScope()) 
                {
                    using (new EditorGUI.DisabledScope(!channel.ParentLogEnabled())) {
                        bool logEnabled = channel.LocalLogEnabled();
                        bool newLogEnabled = false;
                        
                        //  --- NOTE(dan): We only use auto layout for the first channel's line,
                        //                 after that we claw back a little perf by explicitly
                        //                 passing the rect position that we want.
                        if (currentChannel == 0)
                        {
                            newLogEnabled = EditorGUILayout.ToggleLeft(cache.logLabelContent,
                                                                        logEnabled,
                                                                        cache.toggleWidthOptions);
                            cache.logLabelRect = GUILayoutUtility.GetLastRect();
                        }
                        else
                        {
                            var rect = s_ChannelCacheData[Zebug.Instance].logLabelRect;
                            rect.y += channelLineHeight * currentChannel;
                            newLogEnabled = GUI.Toggle(rect, logEnabled, cache.logLabelContent);
                        }
                        
                        if (newLogEnabled != logEnabled) {
                            channel.SetLogEnabled(newLogEnabled);
                        }
                    }

                    using (new EditorGUI.DisabledScope(!channel.ParentGizmosEnabled())) {
                        bool gizmosEnabled = channel.LocalGizmosEnabled();
                        
                        bool newGizmosEnabled = false;
                        if (currentChannel == 0)
                        {
                            newGizmosEnabled = EditorGUILayout.ToggleLeft(cache.gizmosLabelContent,
                                                                          gizmosEnabled,
                                                                          cache.toggleWidthOptions);
                            cache.gizmoLabelRect = GUILayoutUtility.GetLastRect();
                        }
                        else
                        {
                            var rect = s_ChannelCacheData[Zebug.Instance].gizmoLabelRect;
                            rect.y += channelLineHeight * currentChannel;
                            newGizmosEnabled = GUI.Toggle(rect, gizmosEnabled, cache.gizmosLabelContent);
                        }
                        
                        if (newGizmosEnabled != gizmosEnabled) {
                            channel.SetGizmosEnabled(newGizmosEnabled);
                        }
                    }
                }
            }

            currentChannel++;
            
            if (channelExpanded) {

                using (new EditorGUI.IndentLevelScope(1))
                {
                    foreach (IChannel child in channel.Children())
                    {
                        if (_showTestChannels || !s_TestChannels.Contains(child))
                        {
                            DrawChannel(child, visibleChannelCount, ref currentChannel);
                        } 
                    }
                }
            }
        }
        
        #endregion Draw Channel
        
        //  ----------------------------------------------------------------------------------------
        
        #region Draw Graphs
        
        //  ----------------------------------------------------------------------------------------
        
        private void DrawGraphs()
        {
            //  --- At some point will need to deal with points being added but never removed.
            //      Choose a mechanism for dealing with bloat.
            //  * Channel specific settings
            //  * Does Zebug have a guaranteed update? SceneDrawer seems like the most likely...
            //      EditorNeedsRepaint is a bit of a hack around this issue.
            //      but this would need to function regardless of the Zebug editor being open.
            //  * Display graphs in runtime window too.

            DrawGraph(Zebug.Instance);
        }

        private void DrawGraph(IChannel channel)
        {
            if (!channel.GizmosEnabled())
            {
                return;
            }

            if (Zebug.s_ChannelGraphData.TryGetValue(channel, out GraphData graphData))
            {
                GUILayout.Label(channel.Name());

                GUILayout.Box("", EditorStyles.helpBox,
                    GUILayout.Height(100));

                if (Event.current.type == EventType.Repaint)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    var color = channel.GetColor();

                    DrawGraphPointsIntoRect(rect, graphData, color);
                }
            }

            foreach (IChannel child in channel.Children())
            {
                DrawGraph(child);
            }
        }

        private void DrawGraphPointsIntoRect(Rect rect, GraphData graphData, Color color)
        {
            List<GraphData.Sample> points = graphData.points;
            
            int pointCount = points.Count;
            if (pointCount < 2)
            {
                return;
            }

            Handles.color = color;
            var firstSample = graphData.First();
            var lastSample = graphData.Last();

            float startTime = firstSample.time;
            float startFrame = firstSample.frame;

            float endTime = lastSample.time;
            float endFrame = lastSample.frame;

            const float sixtyFpsFrameTimeThousandth = 0.001f / 60f;

            float invTimeScale = 1f / Math.Max(endTime - startTime, sixtyFpsFrameTimeThousandth);

            float xMin = rect.x;
            float xRange = rect.width;

            float valueMin = graphData.minValue;
            float valueMax = graphData.maxValue;

            float yMin = rect.y;
            float yRange = rect.height;

            float invValueScale = 1f / Math.Max(valueMax - valueMin, sixtyFpsFrameTimeThousandth);

            var pointArray = new Vector3[pointCount];
            for (var i = 0; i < pointCount; i++)
            {
                var idx = (graphData.nextIdx + i) % graphData.maxPoints;
                GraphData.Sample sample = points[idx];

                float xT = (sample.time - startTime) * invTimeScale;
                float xVal = xT * xRange + xMin;

                var yT = (sample.value - valueMin) * invValueScale;
                var yVal = (1f - yT)*yRange + yMin;

                pointArray[i] = new Vector3(
                    xVal,
                    yVal,
                    0);
            }

            Handles.DrawAAPolyLine(
                Texture2D.whiteTexture,
                1f,
                pointArray
            );
        }
        
        #endregion  Draw Graphs

        //  ----------------------------------------------------------------------------------------

        #region Advanced Options
        
        private void DrawAdvancedOptions()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                //  --- Clear unused ------------------------------------------
                
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Clear unused channel data");
                    if (GUILayout.Button("Clear"))
                    {

                        ClearRedundantChannelData();
                    }
                }
                
                //  --- Show Test-only channels -------------------------------

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Show test channels");
                    
                    _showTestChannels = PlayerPrefs.GetInt(kShowTestChannelsPref) > 0;
                    bool newShowTestValue = GUILayout.Toggle(_showTestChannels, ""); 
                    if (_showTestChannels != newShowTestValue)
                    {
                        _showTestChannels = newShowTestValue;
                        PlayerPrefs.SetInt(kShowTestChannelsPref, newShowTestValue ? 1 : 0);
                    }
                }

                //  --- iOS logging prefix ------------------------------------

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Add an additional prefix on iOS?");
                    bool oldValue = ZebugPreferences.Instance.UseAdditionalPrefixOnIos; 
                    bool newValue = EditorGUILayout.Toggle("", oldValue);
                    if (newValue != oldValue)
                    {
                        ZebugPreferences.Instance.UseAdditionalPrefixOnIos = newValue;
                    }
                }
                
                ///
                /// iOS devices logging back into XCode have no formatting to facilitate
                /// syntax highlighting, which makes the logs much harder to read. This
                /// enables a dev to spoof a format like ADB logs (for example) in the case
                /// that they want to look at the logs of an android device next to ones
                /// that have been captured on an iOS device.   
                /// 
                
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("iOS additional prefix:");
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = ZebugPreferences.Instance.UseAdditionalPrefixOnIos;
                    string oldPrefix = ZebugPreferences.Instance.AdditionalIosPrefix;
                    string newPrefix = EditorGUILayout.DelayedTextField("",oldPrefix);
                    if (newPrefix != oldPrefix)
                    {
                        ZebugPreferences.Instance.AdditionalIosPrefix = newPrefix;
                    }
                    GUI.enabled = wasEnabled;
                }
            }
        }

        // -----------------------------------------------------------------------------------------
        
        #endregion Advanced Options

        // -----------------------------------------------------------------------------------------
        
        #region Window Vars
        
        private void DrawWindowVars()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Variables");

                foreach (var (channel, variables) in Zebug.s_ChannelWindowVariables)
                {
                    GUILayout.Label(channel.Name());
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        foreach (var (key, value) in variables)
                        {
                            GUILayout.Label($"{key}: {value}");
                        }
                    }
                }
            }
        }

        #endregion Window Vars
        
        //  ----------------------------------------------------------------------------------------
        
        private static void ClearRedundantChannelData()
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
    
    //Class to hold custom gui styles
    public static class ZebugGUIStyles
    {
        private static readonly GUILayoutOption[] _lineDefaultOptions;
        private static readonly GUIStyle _boxStyle;
        private static readonly Color _lineColor;
        private static readonly Texture2D _separatorTex;
 
        //constructor
        static ZebugGUIStyles()
        {
            _boxStyle = new GUIStyle("box");
            _boxStyle.margin.top = _boxStyle.margin.bottom = 5;
            _boxStyle.border.left = _boxStyle.border.right = 0;
            _boxStyle.margin.left = _boxStyle.margin.right = 0;
            _boxStyle.padding.left = _boxStyle.padding.right = 0;
            _boxStyle.normal.background = EditorGUIUtility.whiteTexture;
            
            _lineColor = new Color(0.34f, 0.34f, 0.34f);
            
            _lineDefaultOptions = new GUILayoutOption[2];
            _lineDefaultOptions[0] = GUILayout.ExpandWidth(true);
            _lineDefaultOptions[1] = GUILayout.Height(2f);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Line()
        {
            Line(_lineColor);
        }
     
        public static void Line(Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUILayout.Box( GUIContent.none, _boxStyle, _lineDefaultOptions);
            GUI.color = oldColor;
        }
        
    }
    
    
}