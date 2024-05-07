using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BasePP : MonoBehaviour
{
    [SerializeField]
    protected ComputeShader shader = null;

    protected string kernelName = "CSMain";

    protected Vector2Int texSize = Vector2Int.zero;
    protected Vector2Int groupSize = Vector2Int.zero;
    protected Camera thisCamera;

    protected RenderTexture output = null;
    protected RenderTexture renderedSource = null;

    protected int kernelHandle = -1;
    protected bool init = false;

    protected bool editorGainedFocus;
    protected bool editorGainedFocusPrev;

    protected virtual void Init()
    {
        if (init)
        {
            return;
        }
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("It seems your target Hardware does not support Compute Shaders.");
            return;
        }
        if (!shader)
        {
            Debug.LogError("No shader");
            return;
        }

        kernelHandle = shader.FindKernel(kernelName);

        thisCamera = GetComponent<Camera>();

        if (!thisCamera)
        {
            Debug.LogError("Object has no Camera");
            return;
        }

        ReleaseTextures();
        CreateTextures();

        init = true;
    }

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
            shader.GetKernelThreadGroupSizes(kernelHandle, out uint x, out uint y, out _);
            groupSize.x = Mathf.CeilToInt((float)texSize.x / x);
            groupSize.y = Mathf.CeilToInt((float)texSize.y / y);
        }
        CreateTexture(ref output);
        CreateTexture(ref renderedSource);

        shader.SetTexture(kernelHandle, "source", renderedSource);
        shader.SetTexture(kernelHandle, "output", output);
    }

    protected virtual void OnValidate()
    {
        Init();
    }

    protected virtual void OnEnable()
    {
        Init();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            MustHave.EditorUtils.UnityEditorFocusChanged -= OnEditorFocus;
            MustHave.EditorUtils.UnityEditorFocusChanged += OnEditorFocus;
        }
#endif
    }

    protected virtual void OnDisable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            MustHave.EditorUtils.UnityEditorFocusChanged -= OnEditorFocus;
        }
#endif
        ReleaseTextures();
        init = false;
    }

    protected virtual void OnDestroy()
    {
        ReleaseTextures();
        init = false;
    }

    private void OnEditorFocus(bool focus)
    {
        editorGainedFocusPrev = false;
        editorGainedFocus = focus;
    }

    protected virtual void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        Graphics.Blit(source, renderedSource);

        shader.Dispatch(kernelHandle, groupSize.x, groupSize.y, 1);

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

    protected virtual void OnScreenSizeChange() { }

    protected virtual void SetupOnRenderImage() { }

    protected void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!init || !shader)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            CheckResolution(out _);
            SetupOnRenderImage();
#if UNITY_EDITOR
            if (!Application.isPlaying && editorGainedFocusPrev && !editorGainedFocus)
            {
                //Debug.Log($"{GetType().Name}.OnRenderImage: editorGainedFocusPrev && !editorGainedFocus");
                init = false;
                Init();
            }
#endif
            DispatchWithSource(ref source, ref destination);
        }
        editorGainedFocusPrev = editorGainedFocus;
        editorGainedFocus = false;
    }
}
