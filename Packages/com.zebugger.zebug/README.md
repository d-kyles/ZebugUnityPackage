Zebug
========

MIT open source Unity debug library.

1. [Usage](#Usage)
1. [Installation](#Installation)
1. [Changelog](#Changelog)
1. [Making a Release](#Making-a-Release)
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
Good luck!
1) Normal Unity Package things.
1) Zebug will create a preferences scriptableObject at `Assets/Resources` if you don't have one 
   already. This can be moved to whichever `Resources` folder you like. NOTE: you should git-ignore 
   this file and it's meta file so that your channel preferences are different than your colleages.

Changelog
---------
[Changelog](CHANGELOG.md)

Making a Release
----------------
* Update version number 
    * in Packages/Zebug/package.json
    * Follow https://semver.org/spec/v2.0.0.html 
    * It must be able to be found on it's own line with the following regex
      `^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$`
* Create new entry in CHANGELOG.md
* Commit with new version number in the following format `v 0.4.1`
* Tag that commit with the same name as the commit format above.

License
-------

[MIT](https://choosealicense.com/licenses/mit/)

The license file can be found [here](license.md), and in each source file's header.