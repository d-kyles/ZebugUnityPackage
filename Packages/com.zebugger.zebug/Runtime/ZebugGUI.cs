// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.iOS;

namespace ZebugProject
{
    public class ZebugGuiElement
    {
        public string name;
        public ZebugGuiElement parent;
        public List<ZebugGuiElement> children = new List<ZebugGuiElement>();

        public ZebugGuiElement() {}

        public ZebugGuiElement(string name, ZebugGuiElement parent)
        {
            this.name = name;
            this.parent = parent;
        }

        public delegate void ButtonClickedHandler();
        public event ButtonClickedHandler buttonClicked;

        public void RaiseButtonClicked()
        {
            buttonClicked?.Invoke();
        }
    }

    public class ZebugDebugGuiLayout
    {

        private static ZebugDebugGuiLayout _instance;
        public static ZebugDebugGuiLayout Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ZebugDebugGuiLayout();
                }
                return _instance;
            }
        }

        private ZebugGuiElement _root = new ZebugGuiElement("//", null);
        public ZebugGuiElement Root => _root;

        // drawing stuff
        private ZebugGuiElement _currentElement;
        private bool _showWindow = false;

        public void RegisterAutoButton(string path, ZebugGuiElement.ButtonClickedHandler handler)
        {
            string[] pathElements = path.Split('/');

            ZebugGuiElement current = _root;

            for (int cIdx = 0; cIdx < pathElements.Length; cIdx++)
            {
                string pathElement = pathElements[cIdx];
                ZebugGuiElement child = current.children.Find((x) => x.name == pathElement);
                if (child != null)
                {
                     current = child;
                     continue;
                }
                else
                {
                    child = new ZebugGuiElement(pathElement, current);
                    current.children.Add(child);
                    current = child;
                }
            }
            Zebug.Assert(path.EndsWith(current.name));
            current.buttonClicked += handler;
        }


        //  --- TODO(dan): Migrate to proper UI
        public void OnGUI()
        {
            if (_currentElement == null)
            {
                _currentElement = _root;
            }

            if (!ZebugPreferences.Instance.ShowDebugGUI)
            {
                return;
            }

            const float defaultScale = 100f;
            const float refWidth = 1170;
            const float refHeight = 2532;
            const float averageRef = (refWidth + refHeight) * 0.5f;
            
            float dpiScale = Screen.dpi / defaultScale;
            
            (int width, int height) = (Screen.width, Screen.height);
            
            float refComparison = (width + height) * 0.5f;
            float refScale = averageRef / refComparison; 
            
            float scale = (refScale + dpiScale) * 0.5f;
            
            Matrix4x4 oldMat = GUI.matrix;
            GUI.matrix = GUI.matrix * Matrix4x4.Scale(Vector3.one * scale);
            
            Rect screenRect = UnityEngine.Device.Screen.safeArea;

            screenRect.position /= scale;
            screenRect.size /= scale;
            
            // use top third
            screenRect.height = screenRect.height / 3f; 

            GUILayout.Window( 0
                            , screenRect
                            , DrawDebugWindow
                            , "Debug");

            GUI.matrix = oldMat;

            void DrawDebugWindow(int id)
            {
                List<string> path = new List<string>();
                List<ZebugGuiElement> callChain = new List<ZebugGuiElement>();
                ZebugGuiElement parentElem = _currentElement.parent;
                while (parentElem != null)
                {
                    callChain.Add(parentElem);
                    path.Add(parentElem.name + "/");
                    parentElem = parentElem.parent;
                }

                GUILayout.BeginHorizontal();

                GUI.enabled = _currentElement.parent != null;
                if (GUILayout.Button("<< Back", GUILayout.MaxWidth(100)))
                {
                    if (_currentElement.parent != null)
                    {
                        _currentElement = _currentElement.parent;
                    }
                }
                GUI.enabled = true;

                for (var i = path.Count - 1; i >= 0; i--)
                {
                    if (GUILayout.Button(callChain[i].name))
                    {
                        _currentElement = callChain[i];
                    }
                    GUILayout.Label("/");
                }
                GUILayout.Label(_currentElement.name);
                GUILayout.ExpandWidth(true);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                // draw back button


                for (int i = 0; i < _currentElement.children.Count; i++)
                {
                    ZebugGuiElement child = _currentElement.children[i];
                    if (child.children.Count > 0)
                    {
                        if (GUILayout.Button(">> " + child.name))
                        {
                            _currentElement = child;
                            break;
                        }
                    }
                }

                for (int i = 0; i < _currentElement.children.Count; i++)
                {
                    ZebugGuiElement child = _currentElement.children[i];
                    if (child.children.Count == 0)
                    {
                        if (GUILayout.Button(child.name + "()"))
                        {
                            child.RaiseButtonClicked();
                            break;
                        }
                        GUILayout.Space(10);
                    }
                }
                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

            }
        }
    }

    public partial class Channel<T>
    {
        public static void AddDebugGuiButton(string buttonPath,  ZebugGuiElement.ButtonClickedHandler handler)
        {
            ZebugDebugGuiLayout.Instance.RegisterAutoButton(buttonPath, handler);
        }
    }

}