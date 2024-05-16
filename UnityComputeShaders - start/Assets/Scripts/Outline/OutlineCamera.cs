using UnityEngine;

[ExecuteInEditMode]
public class OutlineCamera : BasePP
{
    public const int LineMaxThickness = 100;

    public OutlineObjectCamera ObjectCamera => objectCamera;
    public int LineThickness => lineThickness;
    public DebugShaderMode ShaderDebugMode { get => debugShaderMode; set => SetDebugShaderMode(value); }

    public enum DebugShaderMode
    {
        NONE,
        DEBUG_SHAPES,
        DEBUG_CIRCLES,
        DEBUG_DEPTH
    }

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

    [SerializeField, HideInInspector]
    private DebugShaderMode debugShaderMode = default;

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

        var shapeTexSize = GetShapeTexSize(out var shapeTexOffset);

        objectCamera.CreateRuntimeAssets(shapeTexSize);

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

    private Vector2Int GetShapeTexSize(out Vector2Int shapeTexOffset)
    {
        Vector2Int extendedSize = default;
        Vector2Int texOffset = default;
        int offset = LineMaxThickness;

        if (texSize.x > texSize.y)
        {
            extendedSize.y = texSize.y + 2 * offset;
            extendedSize.x = extendedSize.y * texSize.x / texSize.y;

            texOffset.y = offset;
            texOffset.x = (int)(texOffset.y * texSize.x / texSize.y + 0.5f);
        }
        else
        {
            extendedSize.x = texSize.x + 2 * offset;
            extendedSize.y = extendedSize.x * texSize.y / texSize.x;

            texOffset.x = offset;
            texOffset.y = (int)(texOffset.x * texSize.y / texSize.x + 0.5f);
        }
        shapeTexOffset = texOffset;

        return extendedSize;
    }

    private void SetDebugShaderMode(DebugShaderMode debugMode)
    {
        Shader.DisableKeyword(debugShaderMode.ToString());
        debugShaderMode = debugMode;
        Shader.EnableKeyword(debugShaderMode.ToString());
    }
}
