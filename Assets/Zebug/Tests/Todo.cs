//  --- Zebug v0.2 ---------------------------------------------------------------------------------
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
     1)
      ```Debug.Log(nameof(target)+"="+(target == null ? "null" : target.ToString()), this);```
      if you take the field as part of a delegate you can automate this.
      ```Debug.Log(()=>target);```
      ^^^ is this true? nameof(...) is compile time, taking the field name... this would need a language
      intrinsic called something like `... [callerParameterNameOf] string prevParamName, ..` which would
      be far to clunky for the language designers to include.


      maybe even as an extension, multi values ```Debug.Log(()=>speed, ()=>heading, ()=>damage);

    2)
      have logging toggleable from a pref/shortcut too, so you don't just have to compile them out.
      --> extend this to channels

    3) Log to Window, keep track of things in an inspector, rather than flooded into console

    ```
      public class Zebug : global::Zebug.Channel<Zebug> {
          public Zebug() : base("TestZebug", Color.blue) { }
      }
    ```

    public class Zebug : Channel<Zebug> {

        public Zebug() : base("ChannelName"
                             , new Color(0.75f, 0.75f, 0.75f)
                             , ZebugProject.Zebug.Instance) {

        }

        public static List<(Vector3, Vector3, Color, float)> s_Lines
            = new List<(Vector3, Vector3, Color, float)>();

        public static void DrawLine(Vector3 startPosition, Vector3 endPosition
                                    , Color color, float duration) {
            s_Lines.Add((startPosition, endPosition, color, Time.time + duration));
        }
    }

    public void OnDrawGizmos() {
        if (Zebug.s_Lines.Count > 0) {
            for (int i = Zebug.s_Lines.Count - 1; i >= 0; i--) {
                (Vector3 start, Vector3 end , Color color, float time) = Zebug.s_Lines[i];
                Gizmos.color = color;
                Gizmos.DrawLine(start, end);
                if (Time.time > time) {
                    Zebug.s_Lines.RemoveAt(i);
                }
            }
        }
    }

    // Do the auto on-load creation of hidden scene element + OnSceneUI or whatever.

    */
}