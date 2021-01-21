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

public class Todo {
    /*
1)  GUI tab for customizing build options and defaults. Auto-add the correct preprocessor directives.
    Dev (on), Perf (off), Prod (stripped). Have logging toggleable from a pref/shortcut too,
    so you don't just have to compile them out.

2)  Zebug.Stopwatch.Start()
     - Togglable Profiling!

3)  Gizmo shapes
    Square, cross, circle, donut? outline shapes? Arrow etc. Possible integration with the SVG
    shapes library would be sweet, with GLLines fallback. Zebug.Draw3Cross(optionally flat on one axis)

4)  ZebugGraph:
    * consider using closures to store the cached channel, that way you can avoid editor only fields and preprocessor directives cluttering up the filespace
    * "Triggering" for graphs? Stop when it gets to a value (etc?)
    * Collapse channels between trigger values? (compare all graphs at time dt, based on channel X exceeding value)

    ```C#
    Zebug.GraphValue("Graph Name", currentDt);
    ```

5)  Component to drop onto a transform and design-time customize a Gizmo for a selected channel
    (Allow creation of channels via ScriptableObject) (have something run on-load, finds type of Channel
    from shared project type cache (spin up as separate package)

6)  Utility extension for GameObject.FullName()

7)  Optionally Log to Window, keep track of things in an inspector, rather than flooded into console

8)  Log/Gizmo enabled should be flags, esp if GUIButton gets added

9)  Zebug.GUIButton
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
 10)  Option for displaying logs in-game?

    */
}