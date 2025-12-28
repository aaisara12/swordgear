#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer
{
    const string k_FieldName = "sceneName";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Draw the inner string field directly so attribute-based drawers (SceneAttributeDrawer)
        // on that field will be invoked by Unity.
        var inner = property.FindPropertyRelative(k_FieldName);
        if (inner == null)
        {
            EditorGUI.LabelField(position, label.text, $"Missing field '{k_FieldName}'");
            return;
        }

        EditorGUI.PropertyField(position, inner, label);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var inner = property.FindPropertyRelative(k_FieldName);
        if (inner == null) return base.GetPropertyHeight(property, label);
        return EditorGUI.GetPropertyHeight(inner, label, true);
    }
}

#endif
