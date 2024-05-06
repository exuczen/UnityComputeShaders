using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Challenge3 : BasePP
{
    [Range(0.0f, 1.0f)]
    public float height = 0.3f;
    [Range(0.0f, 100.0f)]
    public float softenEdge;
    [Range(0.0f, 1.0f)]
    public float shade;
    [Range(0.0f, 1.0f)]
    public float tintStrength;
    public Color tintColor = Color.white;

    protected override void Init()
    {
        base.Init();

        SetProperties();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
    }

    protected void SetProperties()
    {
        float tintHeight = height * texSize.y;
        shader.SetFloat("tintHeight", tintHeight);
        shader.SetFloat("edgeWidth", tintHeight * softenEdge / 100.0f);
        shader.SetFloat("shade", shade);
        shader.SetFloat("tintStrength", tintStrength);
        shader.SetVector("tintColor", tintColor);
    }
}
