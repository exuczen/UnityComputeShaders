// This is renamed, refactored and extended version of BasePP class from https://github.com/NikLever/UnityComputeShaders/blob/main/UnityComputeShaders%20-%20start/Assets/Scripts/BasePP.cs
// BasePP is part of a resource project for Udemy's Compute Shader's course by Nicholas Lever and Penny de Byl: https://www.udemy.com/course/compute-shaders

#if UNITY_EDITOR
//#define USE_EDITOR_SCENE_EVENTS
#endif

using MustHave;
using MustHave.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[RequireComponent(typeof(Camera))]
public class PostProcessor : MonoBehaviour
{
    protected static readonly int SourceTextureID = Shader.PropertyToID("Source");
    protected static readonly int OutputTextureID = Shader.PropertyToID("Output");

    public Camera Camera => thisCamera;

    [SerializeField]
    protected ComputeShader shader = null;

    protected virtual string MainKernelName => "CSMain";
    protected virtual bool SkipDispatch => false;

    protected Vector2Int textureSize = Vector2Int.zero;
    /// <summary>
    /// Main kernel's thread group counts
    /// </summary>
    protected Vector2Int threadGroups = Vector2Int.zero;

    protected Camera thisCamera = null;
    protected CameraChangeListener cameraChangeListener = null;

    protected RenderTexture outputTexture = null;
    protected RenderTexture sourceTexture = null;

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
        mainKernelID = shader.FindKernel(MainKernelName);

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

    protected void CreateTexture(ref RenderTexture textureToMake)
    {
        textureToMake = new RenderTexture(textureSize.x, textureSize.y, 0)
        {
            enableRandomWrite = true
        };
        textureToMake.Create();
    }

    protected virtual void ReleaseTextures()
    {
        ReleaseTexture(ref outputTexture);
        ReleaseTexture(ref sourceTexture);
    }

    protected virtual void CreateTextures()
    {
        textureSize.x = thisCamera.pixelWidth;
        textureSize.y = thisCamera.pixelHeight;

        if (shader)
        {
            shader.GetKernelThreadGroupSizes(mainKernelID, out uint x, out uint y, out _);
            threadGroups.x = ShaderUtils.GetThreadGroupsCount(x, textureSize.x);
            threadGroups.y = ShaderUtils.GetThreadGroupsCount(y, textureSize.y);
        }
        CreateTexture(ref outputTexture);
        CreateTexture(ref sourceTexture);

        shader.SetTexture(mainKernelID, SourceTextureID, sourceTexture);
        shader.SetTexture(mainKernelID, OutputTextureID, outputTexture);
    }

    protected virtual void OnInit() { }

    protected virtual void OnCameraPropertyChange() { }

    protected virtual void OnScreenSizeChange() { }

    protected virtual void SetupOnRenderImage() { }

    protected virtual void DispatchWithSource(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, sourceTexture);

        shader.Dispatch(mainKernelID, threadGroups.x, threadGroups.y, 1);

        Graphics.Blit(outputTexture, destination);
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
#if USE_EDITOR_SCENE_EVENTS
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChangedInEditMode;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChangedInEditMode;
#endif
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
#if USE_EDITOR_SCENE_EVENTS
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChangedInEditMode;
#endif
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
        if (initialized && shader && !SkipDispatch)
        {
            CheckResolution(out _);
            SetupOnRenderImage();
            DispatchWithSource(source, destination);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    private void CheckResolution(out bool changed)
    {
        changed = textureSize.x != thisCamera.pixelWidth || textureSize.y != thisCamera.pixelHeight;

        if (changed)
        {
            ReleaseTextures();
            CreateTextures();
            OnScreenSizeChange();
        }
    }

#if UNITY_EDITOR
#if USE_EDITOR_SCENE_EVENTS
    protected virtual void OnActiveSceneChangedInEditMode(Scene prevScene, Scene scene)
    {
        Debug.Log($"{GetType().Name}.OnActiveSceneChangedInEditMode");
    }

    protected virtual void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        Debug.Log($"{GetType().Name}.OnSceneOpened");
    }
#endif
    private void OnAllAssetsPostprocessed()
    {
        ReInit();
    }
#endif
}
