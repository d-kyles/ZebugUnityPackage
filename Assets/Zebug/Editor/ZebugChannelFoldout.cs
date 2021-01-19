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
using UnityEngine;
using UnityEngine.UIElements;
using ZebugProject;

public class ZebugChannelFoldout : BindableElement, INotifyValueChanged<bool> {
    internal static readonly string ussFoldoutDepthClassName = "unity-foldout--depth-";
    internal static readonly int ussFoldoutMaxDepth = 4;

    //private ZebugChannelToggle m_Toggle;
    private Toggle m_Toggle;
    private VisualElement m_Container;

    [SerializeField] private bool m_Value;

    /// <summary>
    ///   <para>USS class name of elements of this type.</para>
    /// </summary>
    public static readonly string ussClassName = "unity-foldout";

    /// <summary>
    ///   <para>USS class name of toggle elements in elements of this type.</para>
    /// </summary>
    public static readonly string toggleUssClassName = ussClassName + "__toggle";

    /// <summary>
    ///   <para>USS class name of content element in a ZebugChannelFoldout.</para>
    /// </summary>
    public static readonly string contentUssClassName = ussClassName + "__content";

    private static VisualTreeAsset s_ChannelDataTemplate;

    public override VisualElement contentContainer => m_Container;

    public string text {
        get => m_Toggle.text;
        set => m_Toggle.text = value;
    }

    /// <summary>
    ///   <para>Contains the collapse state. True if the ZebugChannelFoldout is open and the contents are visible. False if it's collapsed.</para>
    /// </summary>
    public bool value {
        get => m_Value;
        set {
            if (m_Value == value) {
                return;
            }

            using (ChangeEvent<bool> pooled = ChangeEvent<bool>.GetPooled(m_Value, value)) {
                pooled.target = this;
                SetValueWithoutNotify(value);
                SendEvent(pooled);
                //this.SaveViewData();
            }
        }
    }

    public void SetValueWithoutNotify(bool newValue) {
        m_Value = newValue;
        m_Toggle.value = m_Value;
        contentContainer.style.display = newValue ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public ZebugChannelFoldout() {
        Construct(null);
    }

    public ZebugChannelFoldout(IChannel zebugBase) {
        Construct(zebugBase);
    }

    private static Action<bool> s_EmptyBoolAction = _ => {};

    private void Construct(IChannel channel) {
        m_Value = true;
        AddToClassList(ussClassName);
        AddToClassList("zebug-channel-element");

        //ZebugChannelToggle toggle = new ZebugChannelToggle();
        Toggle toggle = new Toggle();
        toggle.value = true;
        m_Toggle = toggle;
        m_Toggle.RegisterValueChangedCallback(evt => {
            value = m_Toggle.value;
            evt.StopPropagation();
        });
        m_Toggle.AddToClassList(toggleUssClassName);
        hierarchy.Add(m_Toggle);

        if (channel != null) {
            text = channel.Name();
            m_Toggle.Q<Label>().style.color = channel.GetColor();
        }

        // if (s_ChannelDataTemplate == null) {
        //     // this is terrible! :D
        //     const string kEditorLocation = "Assets/Zebug/Editor";
        //     const string kChannelElementName = "ZebugChannelListElement";
        //     const string kChannelElementPath = kEditorLocation + "/" + kChannelElementName;
        //     const string kChannelElementLayout = kChannelElementPath + ".uxml";
        //     s_ChannelDataTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(kChannelElementLayout);
        // }
        var channelData = new VisualElement {
            name = "HorizontalElement",

        };
        IStyle horizontalElemStyle = channelData.style;
        horizontalElemStyle.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

        const string kZebugToggleElement = "zebug-toggle-element";
        var logToggle = new Toggle { name="LogToggle", text = "Log" };
        logToggle.AddToClassList(kZebugToggleElement);

        var gizmosToggle = new Toggle { name="LogToggle", text = "Gizmos" };
        gizmosToggle.AddToClassList(kZebugToggleElement);

        if (channel != null) {
            bool logEnabled = channel.LocalLogEnabled();
            Action<bool> logValueSetter = val => { channel.SetLogEnabled(val); };
            Action<Action<bool>> logValueEventSubscriber = callback => channel.OnLocalLogEnabled += callback;
            _SetupZebugToggle(logToggle, logValueSetter, logValueEventSubscriber, logEnabled);

            bool gizmosEnabled = channel.LocalGizmosEnabled();
            Action<bool> gizmosValueSetter = val => { channel.SetGizmosEnabled(val); };
            Action<Action<bool>> gizmosValueEventSubscriber = callback => channel.OnLocalGizmosEnabled += callback;
            _SetupZebugToggle(gizmosToggle, gizmosValueSetter, gizmosValueEventSubscriber, gizmosEnabled);

        } else {
            _SetupZebugToggle(logToggle, s_EmptyBoolAction);
            _SetupZebugToggle(gizmosToggle, s_EmptyBoolAction);
        }

        void _SetupZebugToggle(Toggle t,
                               Action<bool> setValue,
                               Action<Action<bool>> callbackSubscription = null,
                               bool initialValue = false) {
            t.value = initialValue;
            _SetZebugToggleClasses(t, initialValue);
            t.RegisterValueChangedCallback(evt => {
                bool newVal = evt.newValue;
                setValue(newVal);
                _SetZebugToggleClasses(t, newVal);
            });

            if (callbackSubscription != null) {
                callbackSubscription(newVal => {
                    t.value = newVal;
                });
            }

            void _SetZebugToggleClasses(VisualElement ve, bool enabled) {
                const string kToggleOn = "zebug-toggle-on";
                const string kToggleOff = "zebug-toggle-off";
                if (enabled) {
                    ve.RemoveFromClassList(kToggleOff);
                    ve.AddToClassList(kToggleOn);
                } else {
                    ve.RemoveFromClassList(kToggleOn);
                    ve.AddToClassList(kToggleOff);
                }
            }
        }

        channelData.Add(logToggle);
        channelData.Add(gizmosToggle);

        m_Toggle.Add(channelData);

        m_Container = new VisualElement {
            name = "unity-content"
        };
        m_Container.AddToClassList(contentUssClassName);
        hierarchy.Add(m_Container);

        if (channel != null) {
            var children = channel.Children();
            if (children.Count == 0) {
                m_Toggle.Q("unity-checkmark").style.visibility = Visibility.Hidden;
            }

            foreach (var child in children) {
                m_Container.Add(new ZebugChannelFoldout(child));
            }

        }

        //RegisterCallback(new EventCallback<AttachToPanelEvent>(OnAttachToPanel));
    }




    // private void OnAttachToPanel(AttachToPanelEvent evt) {
    //     //  --- TODO Don't really need this
    //     int num = 0;
    //     for (int index = 0; index <= ussFoldoutMaxDepth; ++index) {
    //         RemoveFromClassList(ussFoldoutDepthClassName + index);
    //     }
    //
    //     RemoveFromClassList(ussFoldoutDepthClassName + "max");
    //     if (this.parent != null) {
    //         for (VisualElement parent = this.parent; parent != null; parent = parent.parent) {
    //             if (parent.GetType() == typeof(Foldout)) {
    //                 ++num;
    //             }
    //         }
    //     }
    //
    //     if (num > ussFoldoutMaxDepth) {
    //         AddToClassList(ussFoldoutDepthClassName + "max");
    //     } else {
    //         AddToClassList(ussFoldoutDepthClassName + num);
    //     }
    // }

    /// <summary>
    ///   <para>Instantiates a Foldout using the data read from a UXML file.</para>
    /// </summary>
    public new class UxmlFactory : UxmlFactory<ZebugChannelFoldout, UxmlTraits> { }

    public new class UxmlTraits : BindableElement.UxmlTraits {
        private UxmlStringAttributeDescription m_Text;
        private UxmlBoolAttributeDescription m_Value;

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
            base.Init(ve, bag, cc);
            if (!(ve is ZebugChannelFoldout foldout)) {
                return;
            }

            foldout.text = m_Text.GetValueFromBag(bag, cc);
            foldout.SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
        }

        public UxmlTraits() {
            UxmlStringAttributeDescription attributeDescription1 = new UxmlStringAttributeDescription();
            attributeDescription1.name = "text";
            m_Text = attributeDescription1;
            UxmlBoolAttributeDescription attributeDescription2 = new UxmlBoolAttributeDescription();
            attributeDescription2.name = "value";
            attributeDescription2.defaultValue = true;
            m_Value = attributeDescription2;
        }
    }
}