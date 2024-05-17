using MustHave;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OutlineCamera))]
public class OutlineCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var camera = target as OutlineCamera;

        EditorGUI.BeginChangeCheck();

        camera.DebugShader = EditorGUILayout.Toggle("Debug Shader", camera.DebugShader);

        var shaderDebugMode = camera.ShaderDebugMode;

        if (camera.DebugShader)
        {
            shaderDebugMode = (OutlineCamera.DebugShaderMode)EditorGUILayout.EnumPopup("Debug Mode", shaderDebugMode);
        }
        if (EditorGUI.EndChangeCheck())
        {
            if (camera.DebugShader)
            {
                camera.ShaderDebugMode = shaderDebugMode;
            }
            EditorUtils.SetSceneOrObjectDirty(target);
        }
    }
}
