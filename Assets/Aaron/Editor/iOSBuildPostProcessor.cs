#nullable enable

// aisara => This project has no dedicated Editor asmdef; Assets/Main.asmdef compiles
// Assets/Aaron/Editor/ into the shared runtime assembly. Guarding on UNITY_EDITOR (not
// just UNITY_IOS) keeps this out of the player build, where UnityEditor and the Xcode
// plist API are not referenced. The BuildTarget check below scopes it to iOS builds.
#if UNITY_EDITOR && UNITY_IOS

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
