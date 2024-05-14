using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BlurHighlight : BasePP
{
    [Range(0, 50)]
    public int blurRadius = 20;
    [Range(0.0f, 100.0f)]
    public float radius = 10;
    [Range(0.0f, 100.0f)]
    public float softenEdge = 30;
    [Range(0.0f, 1.0f)]
    public float shade = 0.5f;
    public Transform trackedObject;

    protected override string MainKernelName => "Highlight";

    private RenderTexture horzOutput = null;

    private ComputeBuffer horzBuffer = null;

    private int kernelHorzPassID;

    protected override void OnInit()
    {
        kernelHorzPassID = shader.FindKernel("HorzPass");
        SetProperties();
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();

        CreateTexture(ref horzOutput);

        horzBuffer?.Dispose();
        horzBuffer = new ComputeBuffer(renderedSource.width * renderedSource.height, 4 * sizeof(float));

        SetShaderTextures();
    }

    protected override void ReleaseTextures()
    {
        base.ReleaseTextures();
        horzBuffer?.Dispose();
        horzBuffer = null;
    }

    private void SetShaderTextures()
    {
        shader.SetTexture(mainKernelID, "source", renderedSource);
        shader.SetTexture(mainKernelID, "output", output);
        shader.SetTexture(mainKernelID, "horzOutput", horzOutput);

        shader.SetTexture(kernelHorzPassID, "source", renderedSource);
        shader.SetTexture(kernelHorzPassID, "output", output);
        shader.SetTexture(kernelHorzPassID, "horzOutput", horzOutput);

        shader.SetBuffer(kernelHorzPassID, "horzBuffer", horzBuffer);
        shader.SetBuffer(mainKernelID, "horzBuffer", horzBuffer);
    }

    protected void SetProperties()
    {
        float rad = (radius / 100.0f) * texSize.y;
        shader.SetFloat("radius", rad);
        shader.SetFloat("edgeWidth", rad * softenEdge / 100.0f);
        shader.SetFloat("shade", shade);
        shader.SetInt("blurRadius", blurRadius);
    }

    protected override void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        Graphics.Blit(source, renderedSource);

        shader.Dispatch(kernelHorzPassID, groupSize.x, groupSize.y, 1);
        shader.Dispatch(mainKernelID, groupSize.x, groupSize.y, 1);

        Graphics.Blit(output, destination);
    }

    protected override void OnScreenSizeChange()
    {
        SetProperties();
    }

    protected override void SetupOnRenderImage()
    {
        if (trackedObject && thisCamera)
        {
            Vector2 center = thisCamera.WorldToScreenPoint(trackedObject.position);
            shader.SetVector("center", center);
        }
    }
}
