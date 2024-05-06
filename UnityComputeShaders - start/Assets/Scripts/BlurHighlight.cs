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

    private RenderTexture horzOutput = null;

    private ComputeBuffer horzBuffer = null;

    private int kernelHorzPassID;

    private Vector4 center = default;

    protected override void Init()
    {
        center = new Vector4();
        kernelName = "Highlight";
        kernelHorzPassID = shader.FindKernel("HorzPass");
        base.Init();
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();

        CreateTexture(ref horzOutput);

        horzBuffer?.Dispose();
        horzBuffer = new ComputeBuffer(renderedSource.width * renderedSource.height, 4 * sizeof(float));

        SetShaderTextures();
    }

    protected override void ClearTextures()
    {
        base.ClearTextures();
        horzBuffer?.Dispose();
        horzBuffer = null;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        
        SetProperties();
    }

    private void SetShaderTextures()
    {
        shader.SetTexture(kernelHandle, "source", renderedSource);
        shader.SetTexture(kernelHandle, "output", output);
        shader.SetTexture(kernelHandle, "horzOutput", horzOutput);

        shader.SetTexture(kernelHorzPassID, "source", renderedSource);
        shader.SetTexture(kernelHorzPassID, "output", output);
        shader.SetTexture(kernelHorzPassID, "horzOutput", horzOutput);

        shader.SetBuffer(kernelHorzPassID, "horzBuffer", horzBuffer);
        shader.SetBuffer(kernelHandle, "horzBuffer", horzBuffer);
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
        shader.Dispatch(kernelHandle, groupSize.x, groupSize.y, 1);

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
            Vector3 pos = thisCamera.WorldToScreenPoint(trackedObject.position);
            center.x = pos.x;
            center.y = pos.y;
            shader.SetVector("center", center);
        }
    }
}
