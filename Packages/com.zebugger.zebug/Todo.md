Todo
======================================================

## Ideas

1) Use Debug.Draw so that ZebugGizmoDrawer works in Edit mode too
    2) Most of the uses of Zebug.Draw should probably be that way, as they're not gizmos.
       Gizmos select the drawing object if clicked on, and are probably more heavyweight.
    3) Use of Zebug.Draw to do gizmos should probably be in a specific OnGizmo method.

2) Gizmo shapes
   * Square
   * cross
   * circle
   * donut
   * outline shapes
   * Arrow etc.
    Possible integration with a SVG shapes library would be sweet, with GLLines fallback. 
   * Make these render at edit-time too, if the ZebugGizmo component exists.

3) ZebugGraph:
    * Integrate "subgraphs" with the main graph-per-channel.
    * Currently WIP: "Triggering" for graphs? Stop when it gets to a value (etc?)
    * Collapse channels between trigger values? (compare all graphs at time dt, based on channel X exceeding value)
    * 'MoreInfo' button
      * expands height
      * shows a list of line name and color
      * Axis labels?
    * Settings button:
      * Auto gridlines
      * Fixed scale
      * Serializable settings to PlayerPrefs?
      * Toggle specific sub-lines on and off
      * Specify the number of point samples
    * GameObject specific graphs? They're currently 'per-channel'. 

4) (Allow creation of channels via ScriptableObject) (have something run on-load, finds type of Channel
    from shared project type cache (spin up as separate package)

5) Utility extension for GameObject.FullName()

6) Log/Gizmo enabled should be flags, esp if GUIButton gets added

7) Zebug.GUIButton
    
    On startup (Awake etc?), add your buttons, and callbacks to hook into, then a class can add debug hooks
    for that kind of behaviour, and the main Zebug class can handle all the annoying layout
    and enable/disable stuff.

    Static, or per last-selected object

    Turns out OnGUI is terrible for performance. Absolutely horrendous, significant overhead even if nothing is donewithin
    within it. 

    ```C#
    private void OnGUI() {
        if (channel.DebugGUIEnabled) {
            for (int i = Zebug.s_Buttons.Count - 1; i >= 0; i--) {
                Button b = Zebug.s_Buttons[i];
    
                Rect buttonRect = Zebug.LayoutNextGUIButton(b);
                if (GUI.Button(buttonRect, b.name) {
                    b.callback?.Invoke();
                }
            }
        }
    }
    ```
8) Option for displaying logs in-game?
     
9) StompyRobot has a good debugger, where you can quad tap a small square at the top to open a 
    debug menu, then you can edit features in submenus, editing debug values like LiveDebug used 
    to. The feature I super love though, is that you can hit the pin button and select multiple 
    debug variables to appear on the screen during play. Just exceptionally useful.
     
10) The 'additional prefix' on iOS could benefit from auto expansion of timestamp etc.
11) Maybe we could do Zebug.DrawHandle(something, handleResultDelegate) and get values out?

13) Use [`ScriptableSingleton`](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/ScriptableSingleton_1.html) for prefs?

## Misc Tasks
* `Channel` should be probably be renamed `ZebugChannel`, as it's used naked and
  has no context when you read it in an inheritance declaration.
* Move ColorTagsOnlyInEditor to some sort of true library config
* GetDefaultChannel(stacklevel) newStackFrame().GetMethod() in Dict<MethodBase, Channel>.
Channel name is Method.DeclaringType.name if IsSubclassOf(MonoBehaviour)
* if logFrameNumbers: <color={0}>{1}</color> 〚{2}〛: 
* channel calls static formatmessage with static stringbuilder
* Dictionary<MethodBase, HashSet<ILOffset>> assertOnceLocations
* `[DebuggerStepThrough]` `[DebuggerNonUserCode]` annotations
* Make sure the public API is sane and appropriately limited.
* DebugAssert in ARFoundations is interesting, it uses DebugAssert.That(...)?.Message($"{expensive}")
  to avoid the GC in string interpolation in cases where your assert won't fire. Works nicely with aggressive
  inlining.
* https://bottosson.github.io/posts/oklab/ --- for graph colors, or auto color select. (_good_ uniform luminance)
  - pick different luminances for dark-mode and light-mode 
* C# 10 will have (ref StringBuilder.AppendInterpolatedStringHandler handler), in other words, the called method will be able 
  to do the interpolation. Needless to say, all Zebug calls should use this to avoid all the string interpolation involved.
* [Conditional("UNITY_EDITOR")] for gizmos
* Optionally annotate Log methods with `[BurstDiscard]`, so you can throw managed debug statements into job functions but have them removed for compiled methods. 
