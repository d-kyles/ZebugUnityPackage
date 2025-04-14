// -------------------------------------------------------------------------------------------------
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZebugProject
{

    public partial class Channel<T>
    {
        public static void LogToWindow(string key, string value)
        {
            if (!Instance.LogEnabled())
            {
                return;
            }
            
            
            if (!Zebug.s_ChannelWindowVariables.TryGetValue(Instance, out var list))
            {
                list = new Dictionary<string, string>();
                Zebug.s_ChannelWindowVariables.Add(Instance, list);
            }
            if(list.TryGetValue(key, out var existingValue))
            {
                if(existingValue == value)
                {
                    return;
                } 
            } 
            
            list[key] = value;
            
            Zebug.RaiseEditorRepaint();
        }
        
        public static void AddWindowButton(string key, Action action)
        {
            if (!Instance.LogEnabled())
            {
                return;
            }
            
            if (!Zebug.s_ChannelWindowButtons.TryGetValue(Instance, out var list))
            {
                list = new Dictionary<string, Action>();
                Zebug.s_ChannelWindowButtons.Add(Instance, list);
            }
            
            list[key] = action;
            
            Zebug.RaiseEditorRepaint();
        }
    }

}