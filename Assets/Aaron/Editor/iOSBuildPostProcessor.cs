#nullable enable

#if UNITY_IOS

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class iOSBuildPostProcessor
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        plist.WriteToFile(plistPath);
    }
}

#endif
