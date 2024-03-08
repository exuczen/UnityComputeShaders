using MustHave;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Voronoi))]
public class VoronoiEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var voronoi = target as Voronoi;

        int logMax = Maths.Log2(Voronoi.ParticlesCapacity);
        voronoi.PointsCount = 1 << EditorGUILayout.IntSlider($"PointsCount: {voronoi.PointsCount}", Maths.Log2((uint)voronoi.PointsCount), 0, logMax);
        voronoi.TargetPointsCount = 1 << EditorGUILayout.IntSlider($"TargetPoints: {voronoi.TargetPointsCount}", Maths.Log2((uint)voronoi.TargetPointsCount), 0, logMax);

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
