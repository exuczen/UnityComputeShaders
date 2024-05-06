using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GaussianBlurHighlight : BasePP
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

    private Vector4 center = default;
    private ComputeBuffer weightsBuffer = null;

    private RenderTexture horzOutput = null;

    private ComputeBuffer horzBuffer = null;

    private int kernelHorzPassID;

    protected override void Init()
    {
        kernelName = "Highlight";
        kernelHorzPassID = shader.FindKernel("HorzPass");
        base.Init();

        SetProperties();
        UpdateWeightsBuffer();
    }

    private float[] SetWeightsArray(int radius, float sigma)
    {
        int total = radius * 2 + 1;
        float[] weights = new float[total];
        float sum = 0.0f;
        float c = 1 / Mathf.Sqrt(2 * Mathf.PI * sigma * sigma);

        float setWeight(int n)
        {
            float weight = c * Mathf.Exp(-0.5f * n * n / (sigma * sigma));
            weights[radius + n] = weight;
            weights[radius - n] = weight;
            return weight;
        }
        sum += setWeight(0);
        for (int n = 1; n < radius; n++)
        {
            sum += setWeight(n) * 2.0f;
        }
        // normalize kernels
        for (int i = 0; i < total; i++)
        {
            weights[i] /= sum;
        }
        return weights;
    }

    private void UpdateWeightsBuffer()
    {
        weightsBuffer?.Dispose();

        float sigma = (float)blurRadius / 1.5f;

        weightsBuffer = new ComputeBuffer(blurRadius * 2 + 1, sizeof(float));
        float[] blurWeights = SetWeightsArray(blurRadius, sigma);
        weightsBuffer.SetData(blurWeights);

        shader.SetBuffer(kernelHorzPassID, "weights", weightsBuffer);
        shader.SetBuffer(kernelHandle, "weights", weightsBuffer);
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();
        shader.SetTexture(kernelHorzPassID, "source", renderedSource);

        CreateTexture(ref horzOutput);

        horzBuffer?.Dispose();
        horzBuffer = new ComputeBuffer(renderedSource.width * renderedSource.height, 4 * sizeof(float));

        shader.SetTexture(kernelHorzPassID, "horzOutput", horzOutput);
        shader.SetTexture(kernelHandle, "horzOutput", horzOutput);

        shader.SetBuffer(kernelHorzPassID, "horzBuffer", horzBuffer);
        shader.SetBuffer(kernelHandle, "horzBuffer", horzBuffer);
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
    }

    protected void SetProperties()
    {
        float rad = (radius / 100.0f) * texSize.y;
        shader.SetFloat("radius", rad);
        shader.SetFloat("edgeWidth", rad * softenEdge / 100.0f);
        shader.SetInt("blurRadius", blurRadius);
        shader.SetFloat("shade", shade);
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

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateWeightsBuffer();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        weightsBuffer?.Dispose();
        horzBuffer?.Dispose();
        horzBuffer = null;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        weightsBuffer?.Dispose();
        horzBuffer?.Dispose();
        horzBuffer = null;
    }
}
