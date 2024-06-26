﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HUDOverlay : BasePP
{
    public Color axisColor = new(0.8f, 0.8f, 0.8f, 1);
    public Color sweepColor = new(0.1f, 0.3f, 0.1f, 1);

    protected override void OnInit()
    {
        SetProperties();
    }

    protected void SetProperties()
    {
        shader.SetVector("axisColor", axisColor);
        shader.SetVector("sweepColor", sweepColor);
    }

    protected override void OnLateUpdate()
    {
        SetShaderFloatTime();
    }
}
