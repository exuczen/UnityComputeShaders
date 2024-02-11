using UnityEditor;
using UnityEngine;

public class AppTargetFramerateSlider : EditorWindow
{
    [MenuItem("Extensions/AppTargetFramerateSlider")]
    private static void Init()
    {
        var window = GetWindow<AppTargetFramerateSlider>();
        window.Show();
    }

    private void OnGUI()
    {
        Application.targetFrameRate = EditorGUILayout.IntSlider(Application.targetFrameRate, 0, 1000);
    }
}
