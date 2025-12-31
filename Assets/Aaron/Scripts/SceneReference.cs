#nullable enable

using System;

// aisara => The point of this class is to allow us to have a dropdown in the inspector
// for data that represents scene names. Being a serializable class also allows us to
// have lists of these dropdown scene selectors.
[Serializable]
public class SceneReference
{
    // The inner string that stores the scene name/path.
    // Keep the field name in sync with the drawer's FindPropertyRelative.
    [SceneAttribute]
    public string sceneName = string.Empty;
}

