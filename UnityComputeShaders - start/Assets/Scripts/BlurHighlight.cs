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
        horzBuffer = new ComputeBuffer(sourceTexture.width * sourceTexture.height, 4 * sizeof(float));

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
        shader.SetTexture(mainKernelID, SourceTextureID, sourceTexture);
        shader.SetTexture(mainKernelID, OutputTextureID, outputTexture);
        shader.SetTexture(mainKernelID, "horzOutput", horzOutput);

        shader.SetTexture(kernelHorzPassID, SourceTextureID, sourceTexture);
        shader.SetTexture(kernelHorzPassID, OutputTextureID, outputTexture);
        shader.SetTexture(kernelHorzPassID, "horzOutput", horzOutput);

        shader.SetBuffer(kernelHorzPassID, "horzBuffer", horzBuffer);
        shader.SetBuffer(mainKernelID, "horzBuffer", horzBuffer);
    }

    protected void SetProperties()
    {
        float rad = (radius / 100.0f) * textureSize.y;
        shader.SetFloat("radius", rad);
        shader.SetFloat("edgeWidth", rad * softenEdge / 100.0f);
        shader.SetFloat("shade", shade);
        shader.SetInt("blurRadius", blurRadius);
    }

    protected override void DispatchShader()
    {
        shader.Dispatch(kernelHorzPassID, threadGroups.x, threadGroups.y, 1);
        shader.Dispatch(mainKernelID, threadGroups.x, threadGroups.y, 1);
    }

    protected override void OnScreenSizeChange()
    {
        SetProperties();
    }

    protected override void OnLateUpdate()
    {
        SetShaderVectorCenter(trackedObject);
    }
}
