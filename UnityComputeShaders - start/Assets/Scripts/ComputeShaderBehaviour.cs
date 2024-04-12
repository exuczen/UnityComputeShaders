using MustHave.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ComputeShaderBehaviour : MonoBehaviour
{
    [SerializeField]
    protected ComputeShader shader = null;

    protected RenderTexture outputTexture = null;

    protected new Renderer renderer = null;

    protected int[] kernelIDs = null;

    protected readonly List<ComputeBuffer> computeBuffers = new();

    protected private void Start()
    {
        renderer = GetComponent<Renderer>();
        renderer.enabled = true;

        if (!Application.isPlaying)
        {
            return;
        }
        CreateTextures();
        InitOnStart();
    }

    protected virtual void OnDestroy()
    {
        ReleaseComputeBuffers(true);
    }

    protected abstract void InitOnStart();

    protected abstract void CreateTextures();

    protected void FindKernels<T>() where T : Enum
    {
        var kernelNames = EnumUtils.GetNames<T>();
        kernelIDs = new int[kernelNames.Length];

        for (int i = 0; i < kernelNames.Length; i++)
        {
            kernelIDs[i] = shader.FindKernel(kernelNames[i]);
        }
    }

    //protected int GetKernelID(Enum kernel)
    //{
    //    return kernelIDs[(int)(object)kernel];
    //}

    protected int GetKernelID<T>(T kernel) where T : Enum
    {
        return kernelIDs[(int)(object)kernel];
    }

    protected void GetKernelThreadGroupSizes<T>(T kernel, uint[] numthreads) where T : Enum
    {
        shader.GetKernelThreadGroupSizes(GetKernelID(kernel), out numthreads[0], out numthreads[1], out numthreads[2]);
    }

    protected int GetThreadGroupCount(uint numthreads, int size, bool clamp = true)
    {
        if (numthreads == 0)
        {
            return 0;
        }
        int n = (int)numthreads;
        int count = (size + n - 1) / n;
        return clamp ? Mathf.Clamp(count, 1, 65535) : count;
    }

    protected void DispatchKernel(int kernelID, Vector3Int threadGroups)
    {
        shader.Dispatch(kernelID, threadGroups.x, threadGroups.y, threadGroups.z);
    }

    protected void DispatchKernel<T>(T kernel, Vector3Int threadGroups) where T : Enum
    {
        DispatchKernel(GetKernelID(kernel), threadGroups);
    }

    protected RenderTexture CreateTexture(int width, int height)
    {
        var texture = new RenderTexture(width, height, 0)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear
        };
        texture.Create();
        return texture;
    }

    protected ComputeBuffer CreateAddComputeBuffer(int count, int stride)
    {
        var buffer = new ComputeBuffer(count, stride);
        computeBuffers.Add(buffer);
        return buffer;
    }
    protected ComputeBuffer CreateAddComputeBuffer(int count, int stride, ComputeBufferType type)
    {
        var buffer = new ComputeBuffer(count, stride, type);
        computeBuffers.Add(buffer);
        return buffer;
    }

    protected ComputeBuffer CreateAddComputeBuffer(int count, int stride, ComputeBufferType type, ComputeBufferMode usage)
    {
        var buffer = new ComputeBuffer(count, stride, type, usage);
        computeBuffers.Add(buffer);
        return buffer;
    }

    protected void ReleaseComputeBuffers(bool dispose = false)
    {
        if (dispose)
        {
            foreach (var buffer in computeBuffers)
            {
                buffer?.Dispose();
            }
        }
        else
        {
            foreach (var buffer in computeBuffers)
            {
                buffer?.Release();
            }
        }
        computeBuffers.Clear();
    }
}
