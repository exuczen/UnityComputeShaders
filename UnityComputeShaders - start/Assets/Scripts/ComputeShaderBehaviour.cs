using MustHave.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ComputeShaderBehaviour : MonoBehaviour
{
    protected readonly struct KernelData
    {
        public int Index { get; }
        public Vector3Int NumThreads { get; }

        public KernelData(ComputeShader shader, string kernelName)
        {
            Index = shader.FindKernel(kernelName);
            shader.GetKernelThreadGroupSizes(Index, out uint numX, out uint numY, out uint numZ);
            NumThreads = new((int)numX, (int)numY, (int)numZ);
        }
    };

    [SerializeField]
    protected ComputeShader shader = null;

    protected RenderTexture outputTexture = null;

    protected new Renderer renderer = null;

    protected readonly Dictionary<Enum, KernelData> kernelsDict = new();

    protected readonly List<ComputeBuffer> computeBuffers = new();

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        renderer.enabled = true;

        if (!Application.isPlaying)
        {
            return;
        }
        ReleaseComputeBuffers();
        CreateTextures();
        CreateComputeBuffers();
        InitOnStart();
    }

    protected virtual void OnDestroy()
    {
        ReleaseComputeBuffers(true);
    }

    protected abstract void CreateTextures();

    protected abstract void CreateComputeBuffers();

    protected abstract void InitOnStart();

    protected void FindKernels<T>() where T : Enum
    {
        kernelsDict.Clear();
        var kernelKeys = EnumUtils.GetValues<T>();

        foreach (var kernelKey in kernelKeys)
        {
            kernelsDict.Add(kernelKey, new KernelData(shader, kernelKey.ToString()));
        }
    }

    protected Vector3Int GetKernelNumThreads<T>(T kernel) where T : Enum
    {
        return GetKernelData(kernel).NumThreads;
    }

    protected KernelData GetKernelData<T>(T kernel) where T : Enum
    {
        return kernelsDict[kernel];
    }

    protected int GetKernelID<T>(T kernel) where T : Enum
    {
        return kernelsDict[kernel].Index;
    }

    protected void GetKernelThreadGroupSizes<T>(T kernel, uint[] numthreads) where T : Enum
    {
        shader.GetKernelThreadGroupSizes(GetKernelID(kernel), out numthreads[0], out numthreads[1], out numthreads[2]);
    }

    protected int GetThreadGroupCount(int numthreads, int size, bool clamp = true)
    {
        if (numthreads == 0)
        {
            return 0;
        }
        int n = numthreads;
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

    protected void ForEachKernel(Action<int> action)
    {
        foreach (var kernel in kernelsDict.Values)
        {
            action(kernel.Index);
        }
    }

    protected RenderTexture CreateTexture(int width, int height, FilterMode filterMode = FilterMode.Bilinear)
    {
        var texture = new RenderTexture(width, height, 0)
        {
            enableRandomWrite = true,
            filterMode = filterMode
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
