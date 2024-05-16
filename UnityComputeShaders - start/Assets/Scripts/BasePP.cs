using MustHave;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BasePP : MonoBehaviour
{
    public Camera Camera => thisCamera;

    [SerializeField]
    protected ComputeShader shader = null;

    protected virtual string MainKernelName => "CSMain";

    protected Vector2Int texSize = Vector2Int.zero;
    protected Vector2Int groupSize = Vector2Int.zero;

    protected Camera thisCamera = null;
    protected CameraChangeListener cameraChangeListener = null;

    protected RenderTexture output = null;
    protected RenderTexture renderedSource = null;

    protected int mainKernelID = -1;
    protected bool initialized = false;

    protected void Init()
    {
        if (initialized)
        {
            return;
        }
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError($"{GetType().Name}.Init: It seems your target Hardware does not support Compute Shaders.");
            return;
        }
        if (!shader)
        {
            Debug.LogError($"{GetType().Name}.Init: No shader.");
            return;
        }

        mainKernelID = shader.FindKernel(MainKernelName);

        thisCamera = GetComponent<Camera>();
        cameraChangeListener = GetComponent<CameraChangeListener>();

        if (!thisCamera)
        {
            Debug.LogError($"{GetType().Name}.Init: Object has no Camera.");
            return;
        }

        ReleaseTextures();
        CreateTextures();

        OnInit();

        initialized = true;
    }

    protected virtual void OnInit() { }

    protected void ReleaseTexture(ref RenderTexture textureToClear)
    {
        if (null != textureToClear)
        {
            textureToClear.Release();
            textureToClear = null;
        }
    }

    protected virtual void ReleaseTextures()
    {
        ReleaseTexture(ref output);
        ReleaseTexture(ref renderedSource);
    }

    protected void CreateTexture(ref RenderTexture textureToMake, int divide = 1)
    {
        textureToMake = new RenderTexture(texSize.x / divide, texSize.y / divide, 0)
        {
            enableRandomWrite = true
        };
        textureToMake.Create();
    }


    protected virtual void CreateTextures()
    {
        texSize.x = thisCamera.pixelWidth;
        texSize.y = thisCamera.pixelHeight;

        if (shader)
        {
            shader.GetKernelThreadGroupSizes(mainKernelID, out uint x, out uint y, out _);
            groupSize.x = Mathf.CeilToInt((float)texSize.x / x);
            groupSize.y = Mathf.CeilToInt((float)texSize.y / y);
        }
        CreateTexture(ref output);
        CreateTexture(ref renderedSource);

        shader.SetTexture(mainKernelID, "Source", renderedSource);
        shader.SetTexture(mainKernelID, "output", output);
    }

    protected virtual void OnValidate()
    {
        if (initialized)
        {
            OnInit();
        }
    }

    protected virtual void OnEnable()
    {
        Init();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorAssetPostprocessor.AllAssetsPostprocessed -= OnAllAssetsPostprocessed;
            EditorAssetPostprocessor.AllAssetsPostprocessed += OnAllAssetsPostprocessed;
        }
#endif
        if (cameraChangeListener)
        {
            cameraChangeListener.PropertyChanged -= OnCameraPropertyChange;
            cameraChangeListener.PropertyChanged += OnCameraPropertyChange;
        }
    }

    protected virtual void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorAssetPostprocessor.AllAssetsPostprocessed -= OnAllAssetsPostprocessed;
        }
#endif
        if (cameraChangeListener)
        {
            cameraChangeListener.PropertyChanged -= OnCameraPropertyChange;
        }
        ReleaseTextures();
        initialized = false;
    }

    protected virtual void OnDestroy()
    {
        ReleaseTextures();
        initialized = false;
    }

    private void OnAllAssetsPostprocessed()
    {
        initialized = false;
        Init();
    }

    protected virtual void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        Graphics.Blit(source, renderedSource);

        shader.Dispatch(mainKernelID, groupSize.x, groupSize.y, 1);

        Graphics.Blit(output, destination);
    }

    protected void CheckResolution(out bool changed)
    {
        changed = texSize.x != thisCamera.pixelWidth || texSize.y != thisCamera.pixelHeight;

        if (changed)
        {
            ReleaseTextures();
            CreateTextures();
            OnScreenSizeChange();
        }
    }

    protected virtual void OnCameraPropertyChange() { }

    protected virtual void OnScreenSizeChange() { }

    protected virtual void SetupOnRenderImage() { }

    protected void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!initialized || !shader)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            CheckResolution(out _);
            SetupOnRenderImage();
            DispatchWithSource(ref source, ref destination);
        }
    }
}
