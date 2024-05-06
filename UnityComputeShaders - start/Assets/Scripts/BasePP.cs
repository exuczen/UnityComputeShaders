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

        ClearTextures();
        CreateTextures();

        init = true;
    }

    protected void ClearTexture(ref RenderTexture textureToClear)
    {
        if (null != textureToClear)
        {
            textureToClear.Release();
            textureToClear = null;
        }
    }

    protected virtual void ClearTextures()
    {
        ClearTexture(ref output);
        ClearTexture(ref renderedSource);
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
    }

    protected virtual void OnDisable()
    {
        ClearTextures();
        init = false;
    }

    protected virtual void OnDestroy()
    {
        ClearTextures();
        init = false;
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
            ClearTextures();
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
            DispatchWithSource(ref source, ref destination);
        }
    }
}
