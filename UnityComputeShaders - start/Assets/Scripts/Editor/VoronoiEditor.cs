using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Voronoi))]
public class VoronoiEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var voronoi = target as Voronoi;

        EditorGUILayout.LabelField($"Radius: {voronoi.CircleRadius}");

        if (GUILayout.Button("Initialize"))
        {
            voronoi.Init();
        }
        if (GUILayout.Button("Change Points Count"))
        {
            voronoi.StartPointsCountChange();
        }
    }
}
