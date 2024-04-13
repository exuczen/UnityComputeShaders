using System;
using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class OrbitalVoronoi : ComputeShaderBehaviour
{
    private const int TexResolution = 1 << 7;

    private enum Kernel
    {
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }
    }

    protected override void CreateComputeBuffers() { }

    protected override void InitOnStart()
    {
        if (Application.isPlaying)
        {
            FindKernels<Kernel>();
            GetThreadGroupSizes();
            InitShader();
        }
    }

    protected override void CreateTextures()
    {
        outputTexture = CreateTexture(TexResolution, TexResolution);
    }

    private void GetThreadGroupSizes()
    {
        //throw new NotImplementedException();
    }

    private void InitShader()
    {
        //throw new NotImplementedException();
    }
}
