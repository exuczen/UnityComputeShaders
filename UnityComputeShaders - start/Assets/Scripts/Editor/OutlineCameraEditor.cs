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

        if (camera.DebugShader)
        {
            EditorGUI.BeginChangeCheck();

            var shaderDebugMode = (OutlineCamera.DebugShaderMode)EditorGUILayout.EnumPopup("Debug Mode", camera.ShaderDebugMode);

            if (EditorGUI.EndChangeCheck())
            {
                camera.ShaderDebugMode = shaderDebugMode;

                //EditorUtils.SetSceneOrObjectDirty(target);
            }
        }
    }
}
