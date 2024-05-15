using UnityEngine;

[ExecuteInEditMode]
public class OutlineCamera : BasePP
{
    public OutlineObjectCamera ObjectCamera => objectCamera;

    private readonly struct ShaderData
    {
        public static readonly int ShapeTexID = Shader.PropertyToID("shapeTexture");
        public static readonly int CircleTexID = Shader.PropertyToID("circleTexture");
        public static readonly int LineThicknessID = Shader.PropertyToID("LineThickness");
    }

    [SerializeField]
    private OutlineObjectCamera objectCamera = null;

    private void Update()
    {
        objectCamera.OnUpdate();
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
        objectCamera.Setup(thisCamera);
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();

        objectCamera.CreateRuntimeAssets(texSize);

        shader.SetTexture(mainKernelID, ShaderData.ShapeTexID, objectCamera.ShapeTexture);
        shader.SetTexture(mainKernelID, ShaderData.CircleTexID, objectCamera.CircleTexture);
    }

    protected override void ReleaseTextures()
    {
        base.ReleaseTextures();

        objectCamera.DestroyRuntimeAssets();
    }

    protected override void OnCameraPropertyChange()
    {
        objectCamera.Setup(thisCamera);
    }

    protected override void OnScreenSizeChange()
    {
        objectCamera.Setup(thisCamera);
    }

    protected override void SetupOnRenderImage()
    {
        objectCamera.RenderShapes();

        shader.SetInt(ShaderData.LineThicknessID, objectCamera.LineThickness);
    }
}
