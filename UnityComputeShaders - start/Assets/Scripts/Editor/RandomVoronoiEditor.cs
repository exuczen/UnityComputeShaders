using MustHave;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RandomVoronoi))]
public class RandomVoronoiEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var voronoi = target as RandomVoronoi;

        int logMax = Maths.Log2(RandomVoronoi.ParticlesCapacity);
        int power;
        EditorGUI.BeginChangeCheck();
        {
            power = (int)(EditorGUILayout.IntSlider($"PointsCount: {voronoi.PointsCount}", Maths.Log2((uint)voronoi.PointsCount), 0, logMax) + 0.5f);
        }
        if (EditorUtils.SetDirtyOnEndChangeCheck(voronoi))
        {
            voronoi.PointsCount = 1 << power;
        }
        EditorGUI.BeginChangeCheck();
        {
            power = (int)(EditorGUILayout.IntSlider($"TargetPoints: {voronoi.TargetPointsCount}", voronoi.TargetLogPointsCount, 0, logMax) + 0.5f);
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
