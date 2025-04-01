using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ZebugProject;

public class ZebugDebugGui : MonoBehaviour
{
    [SerializeField] private Button _tabButtonPrefab;
    [SerializeField] private GameObject _tabPanelPrefab;
    [SerializeField] private ZebugTabVarElement _tabPanelVarElementPrefab;
    
    [SerializeField] private GameObject _tabButtonPanelRoot;
    [SerializeField] private GameObject _tabPanelRoot;
    
    [SerializeField] private InputActionProperty _toggleZebugGui;
    
    private static ZebugDebugGui s_Instance;

    //  ----------------------------------------------------------------------------------------
    
    [RuntimeInitializeOnLoadMethod]
    protected static void InitializeOnLoad()
    {
        if (s_Instance != null)
        {
            return;
        }

        var debugGuiPrefabGo = ZebugPreferences.Instance.DebugGuiPrefab;
        if (debugGuiPrefabGo == null)
        {
            return;
        }
        
        if (!debugGuiPrefabGo.TryGetComponent(out ZebugDebugGui debugGuiPrefab))
        {
            Debug.LogError("ZebugDebugGui prefab must have ZebugDebugGui component attached");
            return;
        }
        
        s_Instance = Instantiate(debugGuiPrefab);
        
        if (EnableAndGetAction(s_Instance._toggleZebugGui, out var guiToggleAction))
        {
            guiToggleAction.performed += (ctx) =>
            {
                s_Instance.gameObject.SetActive(!s_Instance.gameObject.activeSelf);
            };
        }
        s_Instance.gameObject.SetActive(false);
        s_Instance.SetActiveTab(null);

        DontDestroyOnLoad(s_Instance.gameObject);
    }
    
    private static bool EnableAndGetAction(InputActionProperty prop, out InputAction action)
    {
        action = null;
        
        if (prop.action != null)
        {
            prop.action.Enable();
            action = prop.action;
            return true;
        }
        else if (prop.reference != null)
        {
            prop.reference.asset.Enable();
            prop.reference.action.Enable();
            action = prop.reference.action;
            return true;
        }
        return false;
    }
    
    protected void Update()
    {
        CheckInit();
    }
    
    
    private class Tab
    {
        public Button button;
        public GameObject varElementPanel;
        public List<ZebugTabVarElement> vars = new();
    }
    
    
    private Dictionary<IChannel, Tab> _channelTabs = new();

    private void CheckInit()
    {
        foreach (var (channel, windowVar) in Zebug.s_ChannelWindowVariables)
        {
            // find tab for channel
            if (!_channelTabs.TryGetValue(channel, out var tab))
            {
                var tabButton = Instantiate(_tabButtonPrefab, _tabButtonPanelRoot.transform);
                tabButton.GetComponentInChildren<TMP_Text>().text = channel.Name();
                tabButton.onClick.AddListener(() =>
                {
                    SetActiveTab(channel);
                });
                
                var tabPanel = Instantiate(_tabPanelPrefab, _tabPanelRoot.transform);
                tabPanel.SetActive(false);
                
                tab = new Tab()
                {
                    button = tabButton,
                    varElementPanel = tabPanel
                };
                
                _channelTabs.Add(channel, tab);
            }
            
            if (!tab.varElementPanel.activeSelf)
            {
                continue;
            }

            int varCount = windowVar.Count;
            for (int i = tab.vars.Count - 1; i >= varCount; i--)
            {
                Destroy(tab.vars[i].gameObject);
                tab.vars.RemoveAt(i);
            }
            for (int i = tab.vars.Count; i < varCount; i++)
            {
                var tabVar = Instantiate(_tabPanelVarElementPrefab, tab.varElementPanel.transform);
                tab.vars.Add(tabVar);
            }
            
            int varIdx = 0;
            foreach (var (varName, value) in windowVar)
            {
                var tabVar = tab.vars[varIdx++];
                tabVar.varName.text = varName;
                tabVar.varValue.text = value;
            }
        }
    }

    private void SetActiveTab([CanBeNull]IChannel targetChannel)
    {
        foreach (var (channel, _) in Zebug.s_ChannelWindowVariables)
        {
            if (channel == null)
            {
                continue;
            }
            
            bool enable = targetChannel == channel;
            
            if (_channelTabs.TryGetValue(channel, out var tab))
            {
                if (enable)
                {
                    bool already = tab.varElementPanel.activeSelf;
                    if (already)
                    {
                        //  --- Toggle off.
                        enable = false;
                    }
                }
                
                tab.button.GetComponent<Image>().color = enable ? Color.white : new Color(0.72f, 0.72f, 0.72f);
                tab.varElementPanel.SetActive(enable);
            }
        }
    }
}
