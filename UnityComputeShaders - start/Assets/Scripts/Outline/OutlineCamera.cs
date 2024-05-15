﻿using UnityEngine;

[ExecuteInEditMode]
public class OutlineCamera : BasePP
{
    public const int LineMaxThickness = 100;

    public OutlineObjectCamera ObjectCamera => objectCamera;
    public int LineThickness => lineThickness;


    private readonly struct ShaderData
    {
        public static readonly int ShapeTexID = Shader.PropertyToID("shapeTexture");
        public static readonly int ShapeTexSizeID = Shader.PropertyToID("ShapeTexSize");
        public static readonly int ShapeTexOffsetID = Shader.PropertyToID("ShapeTexOffset");
        public static readonly int CircleTexID = Shader.PropertyToID("circleTexture");
        public static readonly int LineThicknessID = Shader.PropertyToID("LineThickness");
    }

    [SerializeField]
    private OutlineObjectCamera objectCamera = null;

    [SerializeField, Range(1, LineMaxThickness)]
    private int lineThickness = 5;

    private void Update()
    {
        objectCamera.OnUpdate(this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        objectCamera.enabled = true;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        objectCamera.enabled = false;
    }

    protected override void OnValidate()
    {
        if (objectCamera)
        {
            objectCamera.Setup(null);
        }
    }

    protected override void OnInit()
    {
        objectCamera.Setup(this);
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();

        objectCamera.CreateRuntimeAssets(texSize, LineMaxThickness, out var shapeTexSize, out var shapeTexOffset);

        shader.SetTexture(mainKernelID, ShaderData.ShapeTexID, objectCamera.ShapeTexture);
        shader.SetTexture(mainKernelID, ShaderData.CircleTexID, objectCamera.CircleTexture);

        shader.SetInts(ShaderData.ShapeTexSizeID, shapeTexSize.x, shapeTexSize.y);
        shader.SetInts(ShaderData.ShapeTexOffsetID, shapeTexOffset.x, shapeTexOffset.y);
    }

    protected override void ReleaseTextures()
    {
        base.ReleaseTextures();

        objectCamera.DestroyRuntimeAssets();
    }

    protected override void OnCameraPropertyChange()
    {
        objectCamera.Setup(this);
    }

    protected override void OnScreenSizeChange()
    {
        objectCamera.Setup(this);
    }

    protected override void SetupOnRenderImage()
    {
        objectCamera.RenderShapes();

        shader.SetInt(ShaderData.LineThicknessID, lineThickness);
    }
}
