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

using static ZebugProject.ZebugUtils;

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
        
        private Dictionary<IChannel, ChannelGuiCacheData> _channelGuiCacheData = new();
        
        private class ChannelGuiCacheData
        {
            public GUIStyle togglesLineStyle;
            public GUIContent channelNameContent;
            public GUIContent emptyContent;
            public GUILayoutOption[] toggleWidthOptions;
            public Rect logLabelRect;
            public Rect gizmoLabelRect;
            public GUIStyle infoButtonStyle;
            public GUIContent infoButtonContent;
        }
        
        private static int s_ExpandedCount;
        private bool _realtimeElementVisible;
        
        private GUIStyle _channelRowStyleTop;
        private GUIStyle _channelRowStyleInner;
        private GUIStyle _channelRowStyleBottom;
        
        private Vector2 _mainContentScrollPosition;
        private Vector2 _advOptionsScrollPosition;
        private Vector2 _variablesScrollPosition;
        
        private const string kShowTestChannelsPref = "ZebugShowTestChannels";
        private const float channelLineHeight = 23f;

        private bool _advOptionsExpanded;
        private bool _windowVarsExpanded;
        private static bool s_addingValueBreakpoint;

        private bool _showTestChannels;
        
        private bool s_StylesLoaded;
        private GUIStyle _graphStyleCollapsed;
        private GUIStyle _graphStyleExpanded;
        private GUIStyle _graphFoldoutStyle;
        private GUIStyle _graphNoSamplesTextStyle;
        private GUIStyle _toolbarButtonStyle;
        private GUIContent _graphNoSamplesText;
        private GUIContent _graphSamplesDisabledText;
        private GUIContent _graphSamplesReenableText;
        private GUILayoutOption[] _graphLayoutOptions;
        
        private GUIContent _channelsTxt;
        private GUIContent _channelsDefaultTxtContent;
        private GUIContent _graphsLabelTxtContent;
        private GUIContent _advOptionsLabelTxtContent;
        private GUIContent _windowVarsLabelTxtContent;
        private GUIContent _logLabelTxtContent;
        private GUIContent _gizmosLabelTxtContent;
        private GUIContent _clearUnusedTxtContent;
        private GUIContent _clearTextContent;
        private GUIContent _showTestChannelsTxtContent;
        private GUIContent _iosTogglePrefixLabelTxtContent;
        private GUIContent _iosPrefixLabelTxtContent;
        private GUIContent _addBreakLabelTxtContent;
        private GUIContent[] _toolbarButtonsContent;

        private GUIStyle _graphSettingsIconStyle;
        private GUIContent _graphSettingsIconContent;

        private float _nextRepaintTime;
        
        
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

                _graphStyleCollapsed = new GUIStyle(EditorStyles.helpBox);
                _graphStyleCollapsed.fixedHeight = 100f;
                _graphStyleCollapsed.alignment = TextAnchor.UpperLeft;
                
                _graphStyleExpanded = new GUIStyle(EditorStyles.helpBox);
                _graphStyleExpanded.fixedHeight = 200f;
                _graphStyleExpanded.alignment = TextAnchor.UpperLeft;
                
                _graphFoldoutStyle = new GUIStyle(EditorStyles.foldout);
                //_graphFoldoutStyle.margin = new RectOffset(16, 0, 0, 0);
                _graphNoSamplesText = new GUIContent("No samples found.");
                _graphSamplesDisabledText = new GUIContent("Channel visuals disabled.");
                _graphSamplesReenableText = new GUIContent("Enable?");
                _graphNoSamplesTextStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                _toolbarButtonStyle = new GUIStyle(GUI.skin.button);
                _toolbarButtonStyle.fixedHeight = 28;
                
                _channelsTxt = new GUIContent("Channels");
                _channelsDefaultTxtContent = new GUIContent("New channels enabled by default?");
                _graphsLabelTxtContent = new GUIContent("Graphs");
                _advOptionsLabelTxtContent = new GUIContent("Advanced Options");
                _windowVarsLabelTxtContent = new GUIContent("Variables");
                _logLabelTxtContent = new GUIContent("Log");
                _gizmosLabelTxtContent = new GUIContent("Gizmos");
                _clearUnusedTxtContent = new GUIContent("Clear unused channel data");
                _clearTextContent = new GUIContent("Clear");
                _showTestChannelsTxtContent = new GUIContent("Show test channels");
                _iosTogglePrefixLabelTxtContent = new GUIContent("Extra log prefix for iOS", "May help ADB style syntax highlighting for exported ios Logs");
                _iosPrefixLabelTxtContent = new GUIContent("iOS log prefix:");
                _addBreakLabelTxtContent = new GUIContent("Add value breakpoint");

                _toolbarButtonsContent = new []{_channelsTxt, _graphsLabelTxtContent};
                
                _graphSettingsIconStyle = new GUIStyle(GUI.skin.button);
                _graphSettingsIconStyle.padding = new RectOffset(0, 0, 0, 0);
                _graphSettingsIconContent = EditorGUIUtility.IconContent("ToolSettings");
                
                s_StylesLoaded = true;
            } 
            catch (NullReferenceException)
            {
                //  --- NOTE(dan): Shortly after recompiling, EditorStyles.helpBox doesn't exist.
                //                 Not sure how to avoid this.
            }
        }
 
        private void CheckInit()
        {
            if (!s_StylesLoaded)
            {
                LoadStyles();
            }
            
            Zebug.EditorNeedsRepaint = OnEditorNeedsRepaint;
            
            //  --- Make double sure unit-test channels are set to be off. 
            if (!PlayerPrefs.HasKey(kShowTestChannelsPref))
            {
                PlayerPrefs.SetInt(kShowTestChannelsPref, 0);
            }
        }
        
        protected void Update()
        {
            float time = Time.realtimeSinceStartup;
            
            if (_realtimeElementVisible) 
            {
                _nextRepaintTime = time + 1/60f;
                _realtimeElementVisible = false;
            }
            
            if (time > _nextRepaintTime)
            {
                _nextRepaintTime = time + 1f;
                Repaint();
            }
        }
        
        private void OnEditorNeedsRepaint()
        {
            _nextRepaintTime = Time.realtimeSinceStartup;
        }
        
        //  ----------------------------------------------------------------------------------------
        
        #region OnGUI
        
        private int tabIdx;
        
        private void OnGUI() 
        {
            Profiler.BeginSample("Zebug IMGUI - Perf Tip: Fold away all the things. Each IMGUI component is crazy expensive.");
            
            CheckInit();
            
            tabIdx = GUILayout.Toolbar(tabIdx, _toolbarButtonsContent, _toolbarButtonStyle);
            
            GUILayout.Space(12);
            ZebugGUIStyles.Line();

            _mainContentScrollPosition = GUILayout.BeginScrollView(_mainContentScrollPosition);

            _realtimeElementVisible = false;
            
            switch (tabIdx)
            {
                case 0: 
                    //  --- Channel Toggle List --------------------------------
                    ChannelsGui();
                    break;
                case 1:
                    //  --- Graphs ---------------------------------------------
                    GraphsGui();
                    break;
            }
            
            GUILayout.FlexibleSpace();
            
            GUILayout.EndScrollView();

            //  --- Adv Options -----------------------------------------------
            
            
            ZebugGUIStyles.Line();
                        
            _advOptionsExpanded = EditorGUILayout.Foldout(_advOptionsExpanded
                                                         , _advOptionsLabelTxtContent
                                                         , toggleOnLabelClick: true);
            
            if (_advOptionsExpanded)
            {
                _advOptionsScrollPosition = GUILayout.BeginScrollView(_advOptionsScrollPosition);
                DrawAdvancedOptions();
                GUILayout.EndScrollView();
            }
            
            _windowVarsExpanded = EditorGUILayout.Foldout(_windowVarsExpanded
                                                         , _windowVarsLabelTxtContent
                                                         , toggleOnLabelClick: true);
            if (_windowVarsExpanded) {
                _variablesScrollPosition =  GUILayout.BeginScrollView(_variablesScrollPosition);
                DrawWindowVars();
                GUILayout.EndScrollView();
            }
            
            Profiler.EndSample();
        }

        //   ---------------------------------------------------------------------------------------
        
        private void ChannelsGui()
        {
            int currentChannel = 0;
            int visibleChannelCount = s_ExpandedCount;
            
            var oldColor = GUI.backgroundColor;
            //GUI.backgroundColor = Color.grey;
            GUI.backgroundColor = Color.white;

            //GUILayout.Label(_channelsTxt, EditorStyles.largeLabel);
            
            DrawChannel(Zebug.Instance, visibleChannelCount, ref currentChannel);
            GUI.backgroundColor = oldColor;

            s_ExpandedCount = currentChannel;
            
            ZebugGUIStyles.Line();
            
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(_channelsDefaultTxtContent);
                    
                bool oldValue = ZebugPreferences.Instance.ChannelsEnabledByDefault; 
                bool newValue = EditorGUILayout.Toggle(GUIContent.none, oldValue);
                if (newValue != oldValue)
                {
                    ZebugPreferences.Instance.ChannelsEnabledByDefault = newValue;
                }
            }

        }

        //   ---------------------------------------------------------------------------------------

        #endregion OnGUI
        
        //   ---------------------------------------------------------------------------------------
        
        #region Draw Channel
        
        private void DrawChannel(IChannel channel, int visibleChannelCount, ref int currentChannel) {
            
            string channelName = channel.Name();
            string channelPath = channel.FullName();
            List<IChannel> children = channel.Children();
            bool isFoldoutLine = children.Count > 0;
            
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

                GetCachedChannelData(channel, out ChannelGuiCacheData cache);
                
                if (isFoldoutLine) {
                   
                    _channelExpandedSet.TryGetValue(channelPath, out bool expanded);

                    channelExpanded = EditorGUILayout.Foldout(expanded,
                                                              channelName,
                                                              true,
                                                              cache.togglesLineStyle);
                    _channelExpandedSet[channelPath] = channelExpanded;
                    
                } else {
                    GUILayout.Label(cache.channelNameContent, cache.togglesLineStyle);
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
                            newLogEnabled = EditorGUILayout.ToggleLeft(_logLabelTxtContent,
                                                                        logEnabled,
                                                                        cache.toggleWidthOptions);
                            cache.logLabelRect = GUILayoutUtility.GetLastRect();
                        }
                        else
                        {
                            var rect = _channelGuiCacheData[Zebug.Instance].logLabelRect;
                            rect.y += channelLineHeight * currentChannel;
                            newLogEnabled = GUI.Toggle(rect, logEnabled, _logLabelTxtContent);
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
                            newGizmosEnabled = EditorGUILayout.ToggleLeft(_gizmosLabelTxtContent,
                                                                          gizmosEnabled,
                                                                          cache.toggleWidthOptions);
                            cache.gizmoLabelRect = GUILayoutUtility.GetLastRect();
                        }
                        else
                        {
                            var rect = _channelGuiCacheData[Zebug.Instance].gizmoLabelRect;
                            rect.y += channelLineHeight * currentChannel;
                            newGizmosEnabled = GUI.Toggle(rect, gizmosEnabled, _gizmosLabelTxtContent);
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

        private void GetCachedChannelData(IChannel channel, out ChannelGuiCacheData guiCache)
        {
            const float togglesWidth = 150f;
            const int indentPaddingSize = 16;
            
            _channelGuiCacheData.TryGetValue(channel, out guiCache);
            if (guiCache == null)
            {
                guiCache = new ChannelGuiCacheData();
                
                var color = channel.GetColor();
                var channelName = channel.Name();
                bool isFoldoutLine = channel.Children().Count > 0;
                
                if (isFoldoutLine) {
                    guiCache.togglesLineStyle = new GUIStyle(EditorStyles.foldout);
                } else {
                    guiCache.togglesLineStyle = new GUIStyle();
                    guiCache.togglesLineStyle.padding = new RectOffset(indentPaddingSize * EditorGUI.indentLevel, 0, 0, 0);
                    guiCache.togglesLineStyle.fontSize = 12;
                    guiCache.togglesLineStyle.alignment = TextAnchor.MiddleLeft;
                    guiCache.togglesLineStyle.fixedHeight = channelLineHeight - 5;
                }
                guiCache.togglesLineStyle.normal.textColor = color;
                guiCache.togglesLineStyle.onNormal.textColor = color;
                guiCache.togglesLineStyle.focused.textColor = color;
                guiCache.togglesLineStyle.onFocused.textColor = color;
                
                guiCache.channelNameContent = new GUIContent(channelName);
                guiCache.emptyContent = new GUIContent();
                
                guiCache.toggleWidthOptions = new GUILayoutOption[1];
                guiCache.toggleWidthOptions[0] = GUILayout.Width(togglesWidth/2);
                
                guiCache.infoButtonStyle = new GUIStyle(GUI.skin.button);
                guiCache.infoButtonStyle.padding = new RectOffset(2, 2, 2, 2);
                guiCache.infoButtonStyle.margin = new RectOffset(2, 2, 2, 2);
                
                guiCache.infoButtonContent = new GUIContent("...", "Show graph information");
                
                _channelGuiCacheData[channel] = guiCache;
            }
        }

        #endregion Draw Channel
        
        //  ----------------------------------------------------------------------------------------
        
        #region Draw Graphs
        
        //  ----------------------------------------------------------------------------------------
        
        private void GraphsGui()
        {
            //  * Channel specific settings
            //  * Display graphs in runtime window too.
            //  --- TODO(dan): Data breakpoints are still very WIP.
            //                 * Not sure they add enough value... much more performant than a
            //                   conditional breakpoint though, and if you're graphing a value
            //                   over time you tend to be very interested in outlier cases.
            //                 Might want to be able to add multiple, change whether they
            //                 auto-toggle off etc.
            //                 Also need a way to remove them without hitting the breakpoint.
            // if (GUILayout.Button(_addBreakLabelTxtContent))
            // {
            //     s_addingValueBreakpoint = !s_addingValueBreakpoint;
            // }
            
            DrawGraphGui(Zebug.Instance);
        }

        //  ----------------------------------------------------------------------------------------
        
        [Serializable]
        private class GraphUserConfig
        {
            public bool FoldoutExpanded;
            public bool GraphExpanded;
            public bool SettingsExpanded;
            public int SampleCountOverride = 200;
        }
        
        private const string k_GraphConfigEditorPrefsKeyPrefix = "Zebug.EditorGraphConfig.";
        
        private Dictionary<IChannel, GraphUserConfig> _graphConfigs = new();

        private void GetGraphGuiPrefs(IChannel channel, out GraphUserConfig config)
        {
            if (!_graphConfigs.TryGetValue(channel, out config))
            {
                string key = k_GraphConfigEditorPrefsKeyPrefix + channel.FullName();
                
                string configJson = EditorPrefs.GetString(key, "{}");
                try
                {
                    config = JsonUtility.FromJson<GraphUserConfig>(configJson) 
                             ?? new GraphUserConfig();
                } 
                finally
                {
                    config ??= new GraphUserConfig();
                }
                _graphConfigs[channel] = config;
            }
        }
        
        private void GraphGuiPrefsSaveCheck()
        {
            if (!GUI.changed)
            {
                return;
            }

            foreach (var (channel, config) in _graphConfigs)
            {
                string key = k_GraphConfigEditorPrefsKeyPrefix + channel.FullName();
                EditorPrefs.SetString(key, JsonUtility.ToJson(config));
            }
        }
        
        //  ----------------------------------------------------------------------------------------
        
        private void DrawGraphGui(IChannel channel)
        {
            if (Zebug.s_ChannelGraphData.TryGetValue(channel, out GraphData graphData))
            {
                DrawGraph(channel, graphData);   
            }

            foreach (IChannel child in channel.Children())
            {
                DrawGraphGui(child);
            }
        }

        //  ----------------------------------------------------------------------------------------
        
        private void DrawGraph(IChannel channel, GraphData graphData)
        {
            GetCachedChannelData(channel, out ChannelGuiCacheData cache);
            GetGraphGuiPrefs(channel, out GraphUserConfig config);
            
            config.FoldoutExpanded = EditorGUILayout.Foldout(config.FoldoutExpanded, cache.channelNameContent, true, _graphFoldoutStyle);
            GraphGuiPrefsSaveCheck();

            if (!config.FoldoutExpanded)
            {
                return;
            }

            _realtimeElementVisible = true;
                
            var graphStyle = config.GraphExpanded ? _graphStyleExpanded : _graphStyleCollapsed;
            GUILayout.Box(cache.emptyContent, graphStyle);

            var currentEventType = Event.current.type;
                
            Rect graphRect = GUILayoutUtility.GetLastRect();
            const float graphRectPadding = 2f;
            graphRect.x += graphRectPadding;
            graphRect.y += graphRectPadding;
            graphRect.width -= graphRectPadding;
            graphRect.height -= graphRectPadding;
                
            if (currentEventType == EventType.Repaint ||
                currentEventType == EventType.MouseMove || 
                currentEventType == EventType.MouseDown)
            {
                // If the mouse cursor is inside our Unity IMGUI rect
                var mousePos = Event.current.mousePosition;
                if (graphRect.Contains(mousePos) && graphData.points.Count > 1)
                {
                    Zebug.RaiseEditorRepaint();

                    DrawGraphMouseInspection(mousePos, graphRect, graphData);
                }
            }

            //  --- Draw the info button in the top right
            var infoButtonRect = new Rect(graphRect.x + graphRect.width - 22, graphRect.y + 2, 20, 20);
            if (GUI.Button(infoButtonRect, cache.infoButtonContent, cache.infoButtonStyle))
            {
                config.GraphExpanded = !config.GraphExpanded;
                GraphGuiPrefsSaveCheck();
            }
            
            var channelColor = channel.GetColor();
            Color lineColor = VisibleColorOrDefault(graphData.lineColor, channelColor);
                
            DrawGridLines(graphData, graphRect);
            
            DrawGraphPointsIntoRect(channel, graphRect, graphData, graphData, lineColor);
                
            foreach (var (subGraphName, subGraphData) in graphData.subGraphs)
            {
                lineColor = VisibleColorOrDefault(subGraphData.lineColor, channelColor);
                    
                DrawGraphPointsIntoRect(channel, graphRect, graphData, subGraphData, lineColor);
            }
                
            if (config.GraphExpanded)
            {
                DrawGraphInfoSection(channel, graphData);
            }
                
            if (config.SettingsExpanded)
            {
                DrawGraphSettingsSection(channel, graphData);
            }
        }
        
        //  ----------------------------------------------------------------------------------------
        
        private void DrawGraphInfoSection(IChannel channel, GraphData graphData)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            var channelColor = channel.GetColor();
            var channelName = channel.Name();
            
            // Draw main graph line
            Color mainLineColor = VisibleColorOrDefault(graphData.lineColor, channelColor);
            DrawGraphLineInfo(channelName, mainLineColor);
            
            // Draw subgraph lines
            foreach (var (subGraphName, subGraphData) in graphData.subGraphs)
            {
                Color subLineColor = VisibleColorOrDefault(subGraphData.lineColor, channelColor);
                DrawGraphLineInfo(subGraphName, subLineColor);
            }
            
            // Draw sample count
            GUILayout.Space(4);
            GUILayout.Label($"Samples: {graphData.points.Count}", EditorStyles.miniLabel);
            
            GUILayout.EndVertical();
            
            var infoSectionRect = GUILayoutUtility.GetLastRect();
            
            //  --- Draw the settings button in the top right
            var infoButtonRect = new Rect(infoSectionRect.x + infoSectionRect.width - 22, infoSectionRect.y + 2, 20, 20);
           
            if (GUI.Button(infoButtonRect, _graphSettingsIconContent, _graphSettingsIconStyle))
            {
                GetGraphGuiPrefs(channel, out var config);
                config.SettingsExpanded = !config.SettingsExpanded;
                GraphGuiPrefsSaveCheck();
            }
        }
        
        //  ----------------------------------------------------------------------------------------
        
        private void DrawGraphSettingsSection(IChannel channel, GraphData graphData)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Settings", EditorStyles.largeLabel);
            ZebugGUIStyles.Line();
            GUILayout.Space(4);
            
            GetGraphGuiPrefs(channel, out var config);
            
            config.SampleCountOverride 
                = EditorGUILayout.IntField("Sample count override", config.SampleCountOverride);
            GraphGuiPrefsSaveCheck();
            
            if (GUI.changed)
            {
                graphData.maxPoints = config.SampleCountOverride;   
            }
            
            GUILayout.EndVertical();
        }
        
        //  ----------------------------------------------------------------------------------------
        
        private void DrawGraphLineInfo(string name, Color color)
        {
            // Draw colored line indicator on the left
            Rect lineRect = GUILayoutUtility.GetRect(20, EditorGUIUtility.singleLineHeight);
            if (Event.current.type == EventType.Repaint)
            {
                Handles.color = color;
                float lineY = lineRect.y + lineRect.height * 0.5f;
                Handles.DrawLine(new Vector3(lineRect.x, lineY, 0), 
                               new Vector3(lineRect.x + 20, lineY, 0));
            }
            
            var thingStyle = new GUIStyle(EditorStyles.miniLabel);
            thingStyle.margin = new RectOffset(30, 0, 0, 0);
            
            // Draw name
            lineRect.x += 20;
            lineRect.width -= 20;
            
            GUI.Label(lineRect, name, thingStyle);
        }

        //  ----------------------------------------------------------------------------------------
        
        private static void DrawGraphMouseInspection(Vector2 mousePos, Rect graphRect, GraphData graphData)
        {
            float mouseXT = (mousePos.x - graphRect.x) / graphRect.width;
            float mouseYT = (mousePos.y - graphRect.y) / graphRect.height;
                        
            float firstTime = graphData.First().time;
            float lastTime = graphData.Last().time;
            float minValue = graphData.minValue;
            float maxValue = graphData.maxValue;
                        
            float cursorTime = mouseXT * (lastTime - firstTime) + firstTime;
            float cursorValue = (1f - mouseYT) * (maxValue - minValue) + minValue;

            if (!s_addingValueBreakpoint)
            {
                var closest = default(GraphData.Sample);
                float closestDistance = float.MaxValue;

                var points = graphData.points;
                int pointCount = points.Count;
                int startIdxOffset = graphData.startIdxOffset;
                
                for (var i = 0; i < pointCount; i++)
                {
                    var idx = (startIdxOffset + i) % pointCount;
                    
                    var sample = points[idx];
                    float distance = MathF.Abs(sample.time - cursorTime);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = sample;
                    } 
                    else
                    {
                        //  --- Assumes monotonically increasing values. Could do a binary search?
                        break;
                    }
                }

                Handles.color = new Color(0.47f, 0.47f, 0.47f, 0.46f);
                Handles.DrawLine(new Vector3(mousePos.x, graphRect.y, 0),
                                 new Vector3(mousePos.x, graphRect.y + graphRect.height, 0));
                
                var labelPos = mousePos;
                labelPos.x += 10f;
                labelPos.y -= 20f;
                
                if (labelPos.x > graphRect.x + graphRect.width - 60f)
                {
                    labelPos.x = graphRect.x + graphRect.width - 60f;
                }
                
                Handles.Label(labelPos, 
                        $"Time: {closest.time:F2}\n" +
                        $"Value: {closest.value:F2}\n" +
                        $"Frame: {closest.frame}",
                        EditorStyles.miniLabel);
            }
            else
            {
                Handles.color = new Color(0.84f, 0f, 0f, 0.78f);
                Handles.DrawLine(new Vector3(graphRect.x, mousePos.y, 0),
                                 new Vector3(graphRect.x + graphRect.width, mousePos.y, 0));
                
                var labelPos = mousePos;
                labelPos.x += 10f;
                labelPos.y -= 20f;
                
                if (labelPos.x > graphRect.x + graphRect.width - 60f)
                {
                    labelPos.x = graphRect.x + graphRect.width - 60f;
                }
                
                Handles.Label(labelPos, $"Value: {cursorValue:F2}\n" + EditorStyles.miniLabel);
                
                if (Event.current.type == EventType.MouseDown)
                {
                    graphData.AddBreakpoint(cursorValue);
                    Zebug.Log($"Added value breakpoint: {cursorValue}");
                    s_addingValueBreakpoint = false;
                }
                
            }
            
        }

        //  ----------------------------------------------------------------------------------------
        
        private void DrawGridLines(GraphData graphData, Rect graphRect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            
            //  --- When not specifying a texture, this is about the same as 1px... not sure why
            //  --- Bug: However, on at least one machine this resulted a very wide line with no fill.
            //           So we'll go back to thin 1-ish pixel lines :(. Might be platform specific.
            //const float lineWidth = 2.5f;
            const float lineWidth = 0f;
            
            float valueMin = graphData.minValue;
            float valueMax = graphData.maxValue;
            
            void DrawGridline(float value, Color color, bool dotted)
            {
                var oldColor = Handles.color;
                Handles.color = color;
                
                float gridLineY = RemapRange(value, 
                                    valueMin, 
                                    valueMax, 
                                    graphRect.y + graphRect.height,
                                    graphRect.y);
                
                var start = new Vector3(graphRect.x, gridLineY, 0);
                var end = new Vector3(graphRect.x + graphRect.width, gridLineY, 0);
                if (!dotted)
                {
                    Handles.DrawLine(start, end, lineWidth);
                }
                else
                {
                    Handles.DrawDottedLine(start, end, lineWidth);
                }
                
                Handles.color = oldColor;
            }
                    
            foreach (var (value, gridColor, dotted) in graphData.gridLines)
            {
                if (value < graphData.maxValue && value > graphData.minValue)
                {
                    DrawGridline(value, gridColor, dotted);
                }
            }
            
            if (graphData.hasBreakValue)
            {
                DrawGridline(graphData.breakValue,  
                    new Color(1f, 0f, 0f, 0.85f), 
                    dotted: true);
            }
        }

        //  ----------------------------------------------------------------------------------------
        
        private void DrawGraphPointsIntoRect(IChannel channel, Rect rect, GraphData referenceData,
            GraphData graphData, Color color)
        {
            List<GraphData.Sample> points = graphData.points;
            
            int pointCount = points.Count;
            if (pointCount < 2)
            {
                DrawUiForNoSamplesFound(channel, rect);
                return;
            }
            
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            
            Handles.color = color;
            var firstSample = referenceData.First();
            var lastSample = referenceData.Last();

            float startTime = firstSample.time;
            float startFrame = firstSample.frame;

            float endTime = lastSample.time;
            float endFrame = lastSample.frame;

            const float sixtyFpsFrameTimeThousandth = 0.001f / 60f;

            float invTimeScale = 1f / Math.Max(endTime - startTime, sixtyFpsFrameTimeThousandth);

            float xMin = rect.x;
            float xRange = rect.width;

            float valueMin = referenceData.minValue;
            float valueMax = referenceData.maxValue;

            float yMin = rect.y;
            float yRange = rect.height;

            float invValueScale = 1f / Math.Max(valueMax - valueMin, sixtyFpsFrameTimeThousandth);

            var pooledArray = ArrayPool<Vector3>.CheckOut(pointCount);
            
            float freshMinValue = float.MaxValue;
            float freshMaxValue = float.MinValue;
            int startIdxOffset = graphData.startIdxOffset;
            
            for (int i = 0; i < pointCount; i++)
            {
                int idx = (startIdxOffset + i) % pointCount;
                GraphData.Sample sample = points[idx];

                float xT = (sample.time - startTime) * invTimeScale;
                float xVal = xT * xRange + xMin;
                
                float value = sample.value;

                //  --- Math min/max don't get optimized to intrinsics when in-editor
                freshMinValue = (value < freshMinValue) ? value : freshMinValue;
                freshMaxValue = (value > freshMaxValue) ? value : freshMaxValue;
                
                float yT = (value - valueMin) * invValueScale;
                
                //  --- Clamp line to within graph rect
                yT = (yT > 1f) ? 1f : (yT < 0f ? 0f : yT); 
                
                float yVal = (1f - yT)*yRange + yMin;
                
                pooledArray[i] = new Vector3(xVal, yVal, 0);
            }
            
            graphData.SmoothUpdateMinValue(freshMinValue);
            graphData.SmoothUpdateMaxValue(freshMaxValue);
                
            //  --- When not specifying a texture, this is about the same as 1px... not sure why
            const float lineWidth = 2.5f;
            
            Handles.DrawAAPolyLine(lineWidth, pointCount, pooledArray);
            ArrayPool<Vector3>.Return(pooledArray);
        }
        
        //  ----------------------------------------------------------------------------------------
        
        private void DrawUiForNoSamplesFound(IChannel channel, Rect rect)
        {
            if (channel.GizmosEnabled())
            {
                var labelRect = new Rect(rect.x + 40, rect.y, rect.width - 80, rect.height);
                GUI.Label(labelRect, _graphNoSamplesText, _graphNoSamplesTextStyle);
            }
            else
            {
                const float minLabelWidth = 125f;
                const float height = 30f;
                const float minButtonWidth = 60f;
                    
                float totalSpace = rect.width - minLabelWidth - minButtonWidth;
                    
                const float xMarginP = 1f;
                const float xLabelP = 1f;
                const float xGapP = 0.25f;
                const float xButtonP = 0.5f;
                const float xEndMarginP = 1f;
                const float spaceRatio = 1f / (xMarginP + xLabelP + xGapP + xButtonP + xEndMarginP);
                    
                float spacePerP = totalSpace * spaceRatio;  
                    
                float top = rect.y + (int)(0.5*rect.height)-0.5f*height; 
                    
                var labelRect = new Rect(rect.x + xMarginP*spacePerP, top, minLabelWidth + xLabelP+spacePerP, height);
                GUI.Label(labelRect, _graphSamplesDisabledText, _graphNoSamplesTextStyle);
                    
                float buttonWidth = minButtonWidth + xButtonP*spacePerP;
                buttonWidth = Mathf.Max(buttonWidth, minButtonWidth);
                var buttonRect = new Rect(labelRect.xMax + xGapP*spacePerP, top, buttonWidth, height);
                    
                if (GUI.Button(buttonRect, _graphSamplesReenableText))
                {
                    channel.SetGizmosEnabled(true);
                }
            }
        }
        
        //  ----------------------------------------------------------------------------------------
        
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
                    GUILayout.Label(_clearUnusedTxtContent);
                    if (GUILayout.Button(_clearTextContent))
                    {

                        ClearRedundantChannelData();
                    }
                }
                
                //  --- Show Test-only channels -------------------------------

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(_showTestChannelsTxtContent);
                    
                    _showTestChannels = PlayerPrefs.GetInt(kShowTestChannelsPref) > 0;
                    bool newShowTestValue = GUILayout.Toggle(_showTestChannels, GUIContent.none); 
                    if (_showTestChannels != newShowTestValue)
                    {
                        _showTestChannels = newShowTestValue;
                        PlayerPrefs.SetInt(kShowTestChannelsPref, newShowTestValue ? 1 : 0);
                    }
                }

                //  --- iOS logging prefix ------------------------------------

                using (new GUILayout.HorizontalScope())
                {
                    // GUILayout.Label(_iosTogglePrefixLabelTxtContent);
                    
                    bool oldValue = ZebugPreferences.Instance.UseAdditionalPrefixOnIos; 
                    bool newValue = EditorGUILayout.Toggle(_iosTogglePrefixLabelTxtContent, oldValue);
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
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = ZebugPreferences.Instance.UseAdditionalPrefixOnIos;
                    string oldPrefix = ZebugPreferences.Instance.AdditionalIosPrefix;
                    string newPrefix = EditorGUILayout.DelayedTextField(_iosPrefixLabelTxtContent, oldPrefix);
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
                foreach (var (channel, variables) in Zebug.s_ChannelWindowVariables)
                {
                    GetCachedChannelData(channel, out ChannelGuiCacheData cache);
                    
                    GUILayout.Label(cache.channelNameContent);
                    
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        foreach (var (key, value) in variables)
                        {
                            _realtimeElementVisible = true;
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
                foreach (var child in channel.Children())
                {
                    AddChannels(child, list);
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