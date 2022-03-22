//  --- Zebug --------------------------------------------------------------------------------------
//  Copyright (c) 2022 Dan Kyles
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
//   BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//   NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//   DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//  ------------------------------------------------------------------------------------------------

using UnityEngine;

namespace ZebugProject {

    /*
     |  --- ZebugPreferences
     |      The settings for which channel is enabled will need to persist into builds, so
     |      PlayerPrefs won't do the trick.
     |
     |      Currently this file isn't used, and channels with important production diagnostic info
     |      can default to being on in builds by calling the follwing in their constructor:
     |          `if (!Application.isEditor) { SetEnabled(true) }`
     |
     |      Ideally you want to make sure your version control system ignores this file, as your
     |      preferred debug channels will likely be different than theirs.
     |
     |      --- TODO(dan): Figure out a good way to do defaults for a CI that can be overridden by
     |                     the local user.
     |      --- TODO(dan): It would also be good to be able to easily specify what will be editor
     |                     vs what will be set in a build. Using this one file like this will result
     |                     in editor settings persisting into the build by default
     |
     | Author: Dan Kyles
     */
    public class ZebugPreferences : ScriptableObject {

        //  --- TODO(dan): Find a good way to auto find settings, as people probably want to
        //                 customise where it is and what it's called.
        private const string kAssetName = "ZebugPreferences";

        private static ZebugPreferences s_Instance;
        public static ZebugPreferences Instance {
            get {
                if (s_Instance == null) {
                    return GetPreferences();
                }
                return s_Instance;
            }
        }

        private ZebugPreferences() { s_Instance = this; }

        private void OnDestroy() { s_Instance = null; }

        //  ----------------------------------------------------------------------------------------

        private static ZebugPreferences GetPreferences() {
            if (s_Instance == null) {
                s_Instance = Resources.Load<ZebugPreferences>(kAssetName);
            }
            //  --- TODO(dan): Addressables load here? it seems unlikely that there'd be significant
            //                 enough resources in the plugin to require a bundle. Maybe if we start
            //                 including non-procedural primitive geometry for physics visualization
            //                 or something, but I'd prefer to generate that at runtime.
            if (s_Instance == null) {
                s_Instance = CreateInstance<ZebugPreferences>();

                #if UNITY_EDITOR
                {
                    string folderPath = "Assets/Ignore/Resources";

                    if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath)) {
                        string[] folders = folderPath.Split('/');
                        int folderCount = folders.Length;
                        string currentValid = (folderCount > 2)
                                                  ? folders[0]
                                                  : "";

                        for (int i = 1; i < folderCount -1; i++) {
                            string nextFolder = folders[i];
                            string testFolder = currentValid + "/" + nextFolder;
                            if (!UnityEditor.AssetDatabase.IsValidFolder(testFolder)) {
                                UnityEditor.AssetDatabase.CreateFolder(currentValid, nextFolder);
                            }
                            currentValid = testFolder;
                        }
                    }
                    UnityEditor.AssetDatabase.CreateAsset(s_Instance, folderPath + kAssetName + ".asset");

                    UnityEditor.EditorUtility.SetDirty(s_Instance);

                    if (!UnityEditor.EditorApplication.isPlaying) {
                        //  --- There are definite issues with saving at runtime, I had a different
                        //      project stop rendering all cameras, and the in-game GUI was
                        //      unresponsive. Just skip all that.
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
                }
                #endif
            }
            return s_Instance;
        }
    }
}
