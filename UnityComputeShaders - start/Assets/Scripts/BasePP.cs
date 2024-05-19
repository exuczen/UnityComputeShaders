using MustHave;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[RequireComponent(typeof(Camera))]
public class BasePP : MonoBehaviour
{
    public Camera Camera => thisCamera;

    [SerializeField]
    protected ComputeShader shader = null;

    protected virtual string MainKernelName => "CSMain";
    protected virtual bool SkipDispatch => false;

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
        if (!cameraChangeListener)
        {
            cameraChangeListener = gameObject.AddComponent<CameraChangeListener>();
        }

        ReleaseTextures();
        CreateTextures();

        OnInit();

        initialized = true;
    }

    protected void ReInit()
    {
        initialized = false;
        Init();
    }

    protected void ReleaseTexture(ref RenderTexture textureToClear)
    {
        if (null != textureToClear)
        {
            textureToClear.Release();
            textureToClear = null;
        }
    }

    protected void CreateTexture(ref RenderTexture textureToMake, int divide = 1)
    {
        textureToMake = new RenderTexture(texSize.x / divide, texSize.y / divide, 0)
        {
            enableRandomWrite = true
        };
        textureToMake.Create();
    }

    protected virtual void ReleaseTextures()
    {
        ReleaseTexture(ref output);
        ReleaseTexture(ref renderedSource);
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
        shader.SetTexture(mainKernelID, "Output", output);
    }

    protected virtual void OnInit() { }

    protected virtual void OnCameraPropertyChange() { }

    protected virtual void OnScreenSizeChange() { }

    protected virtual void SetupOnRenderImage() { }

    protected virtual void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        Graphics.Blit(source, renderedSource);

        shader.Dispatch(mainKernelID, groupSize.x, groupSize.y, 1);

        Graphics.Blit(output, destination);
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
            //EditorSceneManager.sceneOpened -= OnSceneOpened;
            //EditorSceneManager.sceneOpened += OnSceneOpened;
            //EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChangedInEditMode;
            //EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;

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
            //EditorSceneManager.sceneOpened -= OnSceneOpened;
            //EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChangedInEditMode;

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

    protected void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!initialized || !shader || SkipDispatch)
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

    private void CheckResolution(out bool changed)
    {
        changed = texSize.x != thisCamera.pixelWidth || texSize.y != thisCamera.pixelHeight;

        if (changed)
        {
            ReleaseTextures();
            CreateTextures();
            OnScreenSizeChange();
        }
    }

#if UNITY_EDITOR
    //protected virtual void OnActiveSceneChangedInEditMode(Scene prevScene, Scene scene)
    //{
    //    Debug.Log($"{GetType().Name}.OnActiveSceneChangedInEditMode");
    //}

    //protected virtual void OnSceneOpened(Scene scene, OpenSceneMode mode)
    //{
    //    Debug.Log($"{GetType().Name}.OnSceneOpened");
    //}

    private void OnAllAssetsPostprocessed()
    {
        ReInit();
    }
#endif
}
