﻿using UnityEngine;

public class OutlineCamera : BasePP
{
    public OutlineObjectCamera ObjectCamera => objectCamera;

    private readonly struct ShaderData
    {
        public static readonly int ShapeTexID = Shader.PropertyToID("shapeTexture");
        public static readonly int LineWidthID = Shader.PropertyToID("LineWidth");
    }

    [SerializeField]
    private OutlineObjectCamera objectCamera = null;

    [SerializeField, Range(1, 100)]
    private int LineWidth = 5;

    protected override void OnValidate()
    {
        if (objectCamera)
        {
            objectCamera.Setup(null);
        }
    }

    protected override void OnInit()
    {
        objectCamera.Setup(thisCamera);
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();

        objectCamera.CreateTextures(texSize);

        shader.SetTexture(mainKernelID, ShaderData.ShapeTexID, objectCamera.ShapeTexture);
    }

    protected override void ReleaseTextures()
    {
        base.ReleaseTextures();

        objectCamera.ReleaseTextures();
    }

    protected override void OnScreenSizeChange()
    {
        objectCamera.Setup(thisCamera);
    }

    protected override void SetupOnRenderImage()
    {
        shader.SetInt(ShaderData.LineWidthID, LineWidth);
    }
}
