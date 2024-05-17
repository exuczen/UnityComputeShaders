using MustHave;
using MustHave.Utils;
using UnityEngine;

[ExecuteInEditMode]
public class OutlineCamera : BasePP
{
    public const int LineMaxThickness = 100;

    public OutlineObjectCamera ObjectCamera => objectCamera;
    public int LineThickness
    {
        get => lineThickness;
        set => lineThickness = Mathf.Clamp(value, 1, LineMaxThickness);
    }
    public DebugShaderMode ShaderDebugMode { get => debugShaderMode; set => SetDebugShaderMode(value); }
    public bool DebugShader => debugShader;

    protected override bool SkipDispatch => objectCamera.ObjectsCount == 0;

    public enum DebugShaderMode
    {
        DEBUG_NONE,
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

    [SerializeField, HideInInspector]
    private DebugShaderMode debugShaderModeOnInit = default;

    [SerializeField, Range(1, LineMaxThickness)]
    private int lineThickness = 5;

    [SerializeField]
    private bool debugShader = false;

    private void Update()
    {
        objectCamera.OnUpdate(this);
    }

    protected override void OnEnable()
    {
        if (!objectCamera)
        {
            objectCamera = GetComponentInChildren<OutlineObjectCamera>();

            if (!objectCamera)
            {
                var objectCameraPrefab = Resources.Load<OutlineObjectCamera>(OutlineObjectCamera.PrefabName);
                if (objectCameraPrefab)
                {
#if UNITY_EDITOR
                    objectCamera = UnityEditor.PrefabUtility.InstantiatePrefab(objectCameraPrefab, transform) as OutlineObjectCamera;
#else
                    objectCamera = Instantiate(objectCameraPrefab, transform);
#endif
                    objectCamera.name = OutlineObjectCamera.PrefabName;
                }
                else
                {
                    Debug.LogError($"{GetType().Name}.OnEnable: No {OutlineObjectCamera.PrefabName}.prefab in Resources folder.");
                    enabled = false;
                    return;
                }
            }
            objectCamera.transform.Reset();

            shader = objectCamera.ComputeShader;
        }
        base.OnEnable();
        objectCamera.enabled = true;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (objectCamera)
        {
            objectCamera.enabled = false;
        }
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

        SetDebugShaderMode(debugShaderModeOnInit = debugShaderMode);
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

        if (objectCamera)
        {
            objectCamera.DestroyRuntimeAssets();
        }
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

    protected override void OnDestroy()
    {
        base.OnDestroy();

        ObjectUtils.DestroyGameObject(ref objectCamera);
        ObjectUtils.DestroyComponent(ref cameraChangeListener);

        SetDebugShaderMode(debugShaderModeOnInit);
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
        //Debug.Log($"{GetType().Name}.SetDebugShaderMode: {debugMode}");
        Shader.DisableKeyword(debugShaderMode.ToString());
        debugShaderMode = debugMode;
        Shader.EnableKeyword(debugShaderMode.ToString());
    }
}
