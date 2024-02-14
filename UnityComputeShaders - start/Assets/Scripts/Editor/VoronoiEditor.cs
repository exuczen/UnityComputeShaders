using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Voronoi))]
public class VoronoiEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var voronoi = target as Voronoi;
        if (GUILayout.Button("Apply"))
        {
            voronoi.Init();
        }
    }
}
