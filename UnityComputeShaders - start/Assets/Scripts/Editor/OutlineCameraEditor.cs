using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OutlineCamera))]
public class OutlineCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var camera = target as OutlineCamera;

        EditorGUI.BeginChangeCheck();

        OutlineCamera.DebugShaderMode shaderDebugMode = (OutlineCamera.DebugShaderMode)EditorGUILayout.EnumPopup("Debug Mode", camera.ShaderDebugMode);

        if (EditorGUI.EndChangeCheck())
        {
            camera.ShaderDebugMode = shaderDebugMode;
        }
        base.OnInspectorGUI();
    }
}
