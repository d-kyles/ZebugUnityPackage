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

        [SerializeField] private ExpandedChannelsSet _channelExpandedSet = new ExpandedChannelsSet();  
        
        [Serializable]
        private class ExpandedChannelsSet : Dictionary<string, bool>, ISerializationCallbackReceiver
        {
            [SerializeField, HideInInspector] private List<string> _keys = new List<string>();
            [SerializeField, HideInInspector] private List<bool> _values = new List<bool>();

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

        private static HashSet<IChannel> s_TestChannels = new HashSet<IChannel>();
        private static int s_ExpandedCount = 0; 
        private GUIStyle _channelRowStyleTop;
        private GUIStyle _channelRowStyleInner;
        private GUIStyle _channelRowStyleBottom;

        private Vector2 _scrollPosition;
        
        private const string kAllOnPreprocessor = "ZEBUG_ALL_ON"; 
        private bool _preprocessorAllOnSet;
        private float _lastFetchedPreprocessorTime;
        private string[] _symbols;
        private bool _advOptionsExpanded;
        private bool _showTestChannels;

        protected void OnEnable() {

            _lastFetchedPreprocessorTime = 0;
            
            // ZebugEditorUtils.LoadFromZebugRelative Packages/com.zebugger.zebug or Assets/Plugins/Zebug/

            //  --- Make sure preferences are loaded 
            var _ = ZebugPreferences.Instance;

            if (Zebug.s_Channels == null || Zebug.s_Channels.Count == 0) {
                
                TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<IChannel>();
                foreach (Type type in types)
                {
                    if (!typeof(Channel<>).IsAssignableFrom(type)
                        && !type.IsConstructedGenericType) { 
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
                        
                        if (!_channelExpandedSet.ContainsKey(fullName))
                        { 
                            //  --- Default to expanding to show new channels
                            _channelExpandedSet.Add(fullName, true);
                        }  
                        
                        if (type.AssemblyQualifiedName.Contains("EditorTests"))
                        {
                            s_TestChannels.Add(channel); 
                        }
                    }
                }
                
                foreach (KeyValuePair<string,bool> kvp in _channelExpandedSet)
                {
                    s_ExpandedCount += kvp.Value ? 1 : 0;
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

            var lineColor = new Color(0.34f, 0.34f, 0.34f);
            
            /*
            if (GUILayout.Button("Refresh Window"))
            {
                OnEnable();
            }
            ZebugGUIStyles.Line(lineColor, 2);
            */
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            int currentChannel = 0;
            int visibleChannelCount = s_ExpandedCount;
            
            s_ExpandedCount = 0;
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.grey;
            GUI.backgroundColor = Color.white;
            GUILayout.Space(5);
            GUILayout.Label("Channels", EditorStyles.largeLabel);
            DrawChannel(Zebug.Instance);
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
                    PreprocessorUpdate();
                }                    
                
                using (new GUILayout.VerticalScope())
                {
                    oldValue = _preprocessorAllOnSet; 
                    newValue = GUILayout.Toggle(_preprocessorAllOnSet, "Force All On");
                    if (newValue != oldValue)
                    {
                        PreprocessorSetString(kAllOnPreprocessor, newValue);
                        PreprocessorUpdate();
                    }
                }
                
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Scripting Define Symbols");
                    using (new GUILayout.VerticalScope(EditorStyles.textArea))
                    {
                        if (_symbols!= null)
                        {
                            for (int sIdx = 0; sIdx < _symbols.Length; sIdx++)
                            {
                                string symbol = _symbols[sIdx];
                                GUILayout.Label(symbol);
                            }
                        }
                    }
                }
            }
            
            // maybe?
                    //EditorGUILayout.BeginFoldoutHeaderGroup()
                    //EditorGUIUtility.hierarchyMode

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
                
                _advOptionsExpanded = EditorGUILayout.Foldout(_advOptionsExpanded
                                                             , "Advanced Options"
                                                             , toggleOnLabelClick: true);
                if (_advOptionsExpanded)
                {
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Clear unused channel data");
                            if (GUILayout.Button("Clear"))
                            {

                                ClearRedundantChannelData();
                            }
                        }
                        
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Show test channels");
                            const string kShowTestChannels = "ZebugShowTestChannels";
                            if (!PlayerPrefs.HasKey(kShowTestChannels))
                            {
                                PlayerPrefs.SetInt(kShowTestChannels, 0);
                            }
                            
                            _showTestChannels = PlayerPrefs.GetInt(kShowTestChannels) > 0;
                            bool newShowTestValue = GUILayout.Toggle(_showTestChannels, ""); 
                            if (_showTestChannels != newShowTestValue)
                            {
                                _showTestChannels = newShowTestValue;
                                PlayerPrefs.SetInt(kShowTestChannels, newShowTestValue ? 1 : 0);
                            }
                        }
                        
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Add an additional prefix on iOS?");
                            oldValue = ZebugPreferences.Instance.UseAdditionalPrefixOnIos; 
                            newValue = EditorGUILayout.Toggle("", oldValue);
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
            }
            
            GUILayout.EndScrollView();

            //   -----------------------------------------------------------------------------------
            //   -----------------------------------------------------------------------------------
            
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

                            if (!_channelExpandedSet.TryGetValue(channel.FullName(), out bool expanded)) {
                                _channelExpandedSet.Add(channel.FullName(), false);
                            }

                            channelExpanded = EditorGUILayout.Foldout(expanded, channel.Name(), true, foldoutTextStyle);
                            if (channelExpanded != expanded) {
                                _channelExpandedSet[channel.FullName()] = channelExpanded;
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
                            foreach (IChannel child in channel.Children())
                            {
                                if (_showTestChannels || !s_TestChannels.Contains(child))
                                {
                                    DrawChannel(child);
                                } 
                            }
                        }
                    }
                    
                }
            }
        }

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

        private void PreprocessorUpdate()
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            string symbolString = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            string[] symbolArray = symbolString.Split(';');
            _symbols = symbolArray;
            _preprocessorAllOnSet = PreprocessorHasString(kAllOnPreprocessor);
            _lastFetchedPreprocessorTime = Time.time;
        }
        
        private bool PreprocessorHasString(string target)
        {
            bool result = _symbols.Contains(target); 
            return result;
        }
        
        private void PreprocessorSetString(string target, bool targetEnabled)
        {
            bool existing = PreprocessorHasString(target);
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