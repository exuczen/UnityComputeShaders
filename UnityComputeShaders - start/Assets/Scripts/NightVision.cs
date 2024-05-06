﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class NightVision : BasePP
{
    [Range(0.0f, 100.0f)]
    public float radius = 70;
    [Range(0.0f, 1.0f)]
    public float tintStrength = 0.7f;
    [Range(0.0f, 100.0f)]
    public float softenEdge = 3;
    public Color tint = Color.green;
    [Range(50, 500)]
    public int lines = 100;

    protected override void OnValidate()
    {
        if (!init)
        {
            Init();
        }
        SetProperties();
    }

    protected void SetProperties()
    {
        float rad = (radius / 100.0f) * texSize.y;
        shader.SetFloat("radius", rad);
        shader.SetFloat("edgeWidth", rad * softenEdge / 100.0f);
        shader.SetVector("tintColor", tint);
        shader.SetFloat("tintStrength", tintStrength);
        shader.SetInt("lines", lines);
        shader.SetInts("texSize", texSize.x, texSize.y);
    }

    protected override void OnScreenSizeChange()
    {
        SetProperties();
    }

    protected override void SetupOnRenderImage()
    {
        shader.SetFloat("time", Time.time);
    }

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        base.OnRenderImage(source, destination);
    }
}
