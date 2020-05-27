//  --- Zebug --------------------------------------------------------------------------------------
//  Copyright (c) 2020 Dan Kyles
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
//  associated documentation files (the "Software"), to deal in the Software without restriction,
//  including without limitation the rights to use, copy, modify, merge, publish, distribute,
//  sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all copies or
//  substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
//  NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//  DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  -------------------------------------------------------------------------------------------------

public class Todo {
/*
 1)
  ```Debug.Log(nameof(target)+"="+(target == null ? "null" : target.ToString()), this);```
  if you take the field as part of a delegate you can automate this.
  ```Debug.Log(()=>target);```


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




















 */


}