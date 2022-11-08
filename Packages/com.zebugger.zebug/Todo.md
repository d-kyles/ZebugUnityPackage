#Todo

## Big Ideas

1)  Togglable Profiling!
    ```C#
    Zebug.Stopwatch.Start()
    ```

2) Gizmo shapes
   * Square
   * cross
   * circle
   * donut
   * outline shapes
   * Arrow etc.
    Possible integration with a SVG shapes library would be sweet, with GLLines fallback. 

3) ZebugGraph:
    * consider using closures to store the cached channel, that way you can avoid editor only fields and preprocessor directives cluttering up the filespace
    * "Triggering" for graphs? Stop when it gets to a value (etc?)
    * Collapse channels between trigger values? (compare all graphs at time dt, based on channel X exceeding value)
    * The api for the underlying UI for ShaderGraph etc should be publicly available by now. (maybe experimental still?)
        * yup, still experimental even as of 2021.2 UnityEditor.Experimental.GraphView. Good grief.

    ```C#
    Zebug.GraphValue("Graph Name", currentDt);
    ```

4) (Allow creation of channels via ScriptableObject) (have something run on-load, finds type of Channel
    from shared project type cache (spin up as separate package)

5)  Utility extension for GameObject.FullName()

6)  Optionally Log to Window, keep track of things in an inspector, rather than flooded into console

7)  Log/Gizmo enabled should be flags, esp if GUIButton gets added

8)  Zebug.GUIButton
    
    On startup (Awake etc?), add your buttons, and callbacks to hook into, then a class can add debug hooks
    for that kind of behaviour, and the main Zebug class can handle all the annoying layout
    and enable/disable stuff.

    Static, or per last-selected object

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
 9) Option for displaying logs in-game?
     
 10) StompyRobot has a good debugger, where you can quad tap a small square at the top to open a 
     debug menu, then you can edit features in submenus, editing debug values like LiveDebug used 
     to. The feature I super love though, is that you can hit the pin button and select multiple 
     debug variables to appear on the screen during play. Just exceptionally useful.
     
 11) The 'additional prefix' on iOS could benefit from auto epansion of timestamp etc.

## Misc Tasks
* `Channel` should be probably be renamed `ZebugChannel`, as it's used naked and
  has no context when you read it in an inheritance declaration.
* Move ColorTagsOnlyInEditor to some sort of true library config
* GetDefaultChannel(stacklevel) newStackFrame().GetMethod() in Dict<MethodBase, Channel>.
Channel name is Method.DeclaringType.name if IsSubclassOf(MonoBehaviour)
* if logFrameNumbers: <color={0}>{1}</color> 〚{2}〛: 
* channel calls static formatmessage with static stringbuilder
* Dictionary<MethodBase, HashSet<ILOffset>> assertOnceLocations
* [DebuggerStepThrough] [DebuggerNonUserCode] annotations
* Make sure the public API is sane and appropriately limited.
* DebugAssert in ARFoundations is interesting, it uses DebugAssert.That(...)?.Message($"{expensive}")
  to avoid the GC in string interpolation in cases where your assert won't fire. Works nicely with aggressive
  inlining.
* https://bottosson.github.io/posts/oklab/ --- for graph colors, or auto color select. (_good_ uniform luminance)
  - pick different luminances for dark-mode and light-mode 
* C# 10 will have (ref StringBuilder.AppendInterpolatedStringHandler handler), in other words, the called method will be able 
  to do the interpolation. Needless to say, all Zebug calls should use this to avoid all the string interpolation involved.
* [Conditional("UNITY_EDITOR")] for gizmos
* `private static int FixedFrame() { return (int)(Time.fixedTime / Time.fixedDeltaTime); }` (log in fixed update spams [0,>1] times, this shows which.)
