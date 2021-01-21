Zebug
========

MIT open source Unity debug library.

1. [Usage](#Usage)
1. [Installation](#Installation)
1. [Release Notes](#release-notes)
1. [License](#license)

Usage
-----

Example channel definition:
```C#
public class ExampleChannel : Channel<ExampleChannel> {
    public ExampleChannel() : base("Example", Color.magenta) {

        //  --- Optional, will allow skip logging errors and warnings in a non-editor build
        // AllowWarningAndErrorMuting = true;

        //  --- Can force logging to default to on like this:
        // m_LogEnabled = true; // does not override subsequent UI interactions
    }
}
```
Example channel usage:
```C#
public class ExampleUsage : MonoBehaviour {
    [SerializeField] private GameObject m_Prefab;
    protected void OnEnable() {
        if (m_Prefab == null) {
            ExampleChannel.LogError("m_Prefab needs to be set"):
        }
    }
}
```

Lines:
```C#
public class LineDrawer : MonoBehaviour {
    protected void Update() {
        Vector3 transformPos = m_Transform.position;
        Vector3 pos = transformPos ;
        float t = Time.time;
        
        Vector3 offset = new Vector3((float) Math.Sin(6.28f*t)
                                    , 0
                                    , (float) Math.Cos(6.28f*t));
        //  --- By default this line will appear on screen for 1 frame
        //      if the game update rate is higher than the Scene GUI, 
        //      there may be multiple lines on screen at once.      
        Zebug.DrawLine(pos, pos + offset);
    }
}
```

Installation
------------
For now, embed by copying the base `com.zebugger.zebug` folder into the `<project>/Packages` folder.

Changelog
---------
[Changelog](CHANGELOG.md)

License
-------

[MIT](https://choosealicense.com/licenses/mit/)

The license file can be found [here](license.md), and in each source file's header.