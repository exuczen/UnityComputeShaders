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
        int power;
        EditorGUI.BeginChangeCheck();
        {
            power = EditorGUILayout.IntSlider($"PointsCount: {voronoi.PointsCount}", Maths.Log2((uint)voronoi.PointsCount), 0, logMax);
        }
        if (EditorUtils.SetDirtyOnEndChangeCheck(voronoi))
        {
            voronoi.PointsCount = 1 << power;
        }
        EditorGUI.BeginChangeCheck();
        {
            power = EditorGUILayout.IntSlider($"TargetPoints: {voronoi.TargetPointsCount}", voronoi.TargetLogPointsCount, 0, logMax);
        }
        if (EditorUtils.SetDirtyOnEndChangeCheck(voronoi))
        {
            voronoi.TargetLogPointsCount = power;
        }
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
