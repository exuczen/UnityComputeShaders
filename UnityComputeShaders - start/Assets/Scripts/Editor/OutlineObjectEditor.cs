using MustHave;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OutlineObject))]
public class OutlineObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var camera = CameraUtils.MainOrCurrent;
        var outlineCamera = camera ? camera.GetComponent<OutlineCamera>() : null;
        if (outlineCamera)
        {
            EditorGUI.BeginChangeCheck();

            int lineThickness = EditorGUILayout.IntSlider("Line Thickness", outlineCamera.LineThickness, 1, OutlineCamera.LineMaxThickness);

            if (EditorGUI.EndChangeCheck())
            {
                outlineCamera.LineThickness = lineThickness;

                if (!EditorApplication.isPlaying)
                {
                    EditorUtils.SetSceneOrObjectDirty(outlineCamera);
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }
    }
}
