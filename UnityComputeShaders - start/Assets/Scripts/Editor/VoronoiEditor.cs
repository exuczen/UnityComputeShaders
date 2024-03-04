using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Voronoi))]
public class VoronoiEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var voronoi = target as Voronoi;
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
