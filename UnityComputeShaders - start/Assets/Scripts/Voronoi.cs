﻿using UnityEngine;
using System.Collections;
using System;

public class Voronoi : MonoBehaviour
{
    private const int ParticlesCapacity = 1 << 16;
    private const int ParticleSize = 2 * sizeof(int) + 9 * sizeof(float) + sizeof(uint);
    private const int TexResolution = 128;

    private readonly Color[] CircleColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };

    private struct ShaderData
    {
        public int TimeID;
        public int RadiusID;
        public int RadiusSqrID;
        public int PointsCountID;
        public int PointsCapacityID;

        public int OutputTextureID;
        public int IndexTextureID;
        public int ColorsBufferID;
        public int ParticlesBufferID;
        public int TempBufferID;

        public ShaderData(ComputeShader shader)
        {
            TimeID = Shader.PropertyToID("Time");
            RadiusID = Shader.PropertyToID("Radius");
            RadiusSqrID = Shader.PropertyToID("RadiusSqr");
            PointsCountID = Shader.PropertyToID("PointsCount");
            PointsCapacityID = Shader.PropertyToID("PointsCapacity");

            OutputTextureID = Shader.PropertyToID("outputTexture");
            IndexTextureID = Shader.PropertyToID("indexTexture");
            ColorsBufferID = Shader.PropertyToID("colorsBuffer");
            ParticlesBufferID = Shader.PropertyToID("particlesBuffer");
            TempBufferID = Shader.PropertyToID("tempBuffer");
        }
    }

    public int CircleRadius => circleRadius;

    [SerializeField]
    private ComputeShader shader = null;
    [SerializeField]
    private Color clearColor = Color.blue;
    [SerializeField, Range(1, 1 << 16)]
    private int pointsCount = 16;
    [SerializeField, Range(1, 1 << 16)]
    private int targetPointsCount = 16;
    [SerializeField, Range(1, 5)]
    private float pointsChangeDuration = 3;

    private int circleThreadGroupCount = 1; //[Range(1, 65535)]
    private int clearThreadGroupCount = 1;

    private new Renderer renderer = null;
    private RenderTexture outputTexture = null;
    private RenderTexture indexTexture = null;

    private ComputeBuffer particlesBuffer = null;
    private ComputeBuffer colorsBuffer = null;
    private ComputeBuffer tempBuffer = null;

    private ShaderData shaderData = default;

    private int circlesKernel;
    private int diamondsKernel;
    private int fillCirclesKernel;
    private int lineKernel;
    private int clearOutputTextureKernel;
    private int clearIndexTextureKernel;
    private int randomParticlesKernel;
    private int particlesKernel;

    private int circleRadius = 16;

    private uint circleNumThreadsX;
    //private uint circleNumThreadsZ;

    private float pointsCountChangeStartTime = -1f;
    private int startPointsCount;

    public void StartPointsCountChange()
    {
        if (pointsCount != targetPointsCount)
        {
            //Debug.Log($"{GetType().Name}.StartPointsCountChange: {pointsCount} -> {targetPointsCount}");
            pointsCountChangeStartTime = Time.time;
            startPointsCount = pointsCount;
        }
    }

    public void Init()
    {
        if (Application.isPlaying)
        {
            particlesBuffer?.Release();
            colorsBuffer?.Release();
            tempBuffer?.Release();

            FindKernels();
            SetThreadGroupCounts();
            InitShader();
        }
    }

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        renderer.enabled = true;

        CreateTextures();
        Init();
    }

    private void Update()
    {
        UpdatePointsCount();
        DispatchKernels();
    }

    private void OnDestroy()
    {
        particlesBuffer?.Dispose();
        colorsBuffer?.Dispose();
        tempBuffer?.Dispose();
    }

    private void CreateTextures()
    {
        outputTexture = new RenderTexture(TexResolution, TexResolution, 0)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        outputTexture.Create();

        indexTexture = new RenderTexture(outputTexture.descriptor);
        indexTexture.Create();
    }

    private void FindKernels()
    {
        circlesKernel = shader.FindKernel("Circles");
        clearOutputTextureKernel = shader.FindKernel("ClearOutputTexture");
        clearIndexTextureKernel = shader.FindKernel("ClearIndexTexture");
        diamondsKernel = shader.FindKernel("Diamonds");
        fillCirclesKernel = shader.FindKernel("FillCircles");
        lineKernel = shader.FindKernel("Line");
        randomParticlesKernel = shader.FindKernel("RandomParticles");
        particlesKernel = shader.FindKernel("Particles");
    }

    private void SetThreadGroupCounts()
    {
        shader.GetKernelThreadGroupSizes(circlesKernel, out circleNumThreadsX, out _, out _);
        circleThreadGroupCount = GetThreadGroupCount(circleNumThreadsX, pointsCount);

        ////shader.GetKernelThreadGroupSizes(circlesKernel, out _, out _, out circleNumThreadsZ);
        //shader.GetKernelThreadGroupSizes(fillCirclesKernel, out _, out _, out circleNumThreadsZ);
        //circleThreadGroupCount = GetThreadGroupCount(circleNumThreadsZ, pointsCount);

        shader.GetKernelThreadGroupSizes(clearOutputTextureKernel, out uint numthreadsX, out _, out _);
        clearThreadGroupCount = GetThreadGroupCount(numthreadsX, TexResolution);

        Debug.Log($"{GetType().Name}.SetThreadGroupCounts: circleThreadGroupCount: {circleThreadGroupCount}");
    }

    private int GetThreadGroupCount(uint numthreadsX, int size)
    {
        //circleThreadGroupCount = Mathf.Clamp((int)((pointsCount + circleNumThreadsX - 1) / circleNumThreadsX), 1, 65535);
        //circleThreadGroupCount = Mathf.Clamp((int)((pointsCount + circleNumThreadsZ - 1) / circleNumThreadsZ), 1, 65535);
        //clearThreadGroupCount = (int)((TexResolution + numthreadsX - 1) / numthreadsX);
        int n = (int)numthreadsX;
        return Mathf.Clamp((size + n - 1) / n, 1, 65535);
    }

    private void InitShader()
    {
        shaderData = new(shader);

        shader.SetInt("TexResolution", TexResolution);
        shader.SetVector("ClearColor", clearColor);
        shader.SetFloat("CircleRadiusF", Math.Max(2, circleRadius - 1));
        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);
        shader.SetInt(shaderData.PointsCountID, pointsCount);
        shader.SetInt(shaderData.PointsCapacityID, ParticlesCapacity);

        int[] kernels = new int[]
        {
            circlesKernel,
            diamondsKernel,
            fillCirclesKernel,
            lineKernel,
            clearOutputTextureKernel,
            clearIndexTextureKernel,
            randomParticlesKernel,
            particlesKernel
        };
        colorsBuffer = new ComputeBuffer(CircleColors.Length, 4 * sizeof(float));
        colorsBuffer.SetData(CircleColors);
        particlesBuffer = new ComputeBuffer(ParticlesCapacity, ParticleSize);
        tempBuffer = new ComputeBuffer(1, sizeof(int));
        tempBuffer.SetData(new int[tempBuffer.count]);

        for (int i = 0; i < kernels.Length; i++)
        {
            shader.SetTexture(kernels[i], shaderData.OutputTextureID, outputTexture);
            shader.SetTexture(kernels[i], shaderData.IndexTextureID, indexTexture);
            shader.SetBuffer(kernels[i], shaderData.ColorsBufferID, colorsBuffer);
            shader.SetBuffer(kernels[i], shaderData.ParticlesBufferID, particlesBuffer);
            shader.SetBuffer(kernels[i], shaderData.TempBufferID, tempBuffer);
        }
        renderer.material.SetTexture("_MainTex", outputTexture);

        shader.Dispatch(clearIndexTextureKernel, clearThreadGroupCount, clearThreadGroupCount, 1);

        shader.Dispatch(randomParticlesKernel, GetThreadGroupCount(circleNumThreadsX, ParticlesCapacity), 1, 1);
        //shader.Dispatch(randomParticlesKernel, 1, 1, GetThreadGroupCount(circleNumThreadsX, ParticlesCapacity));
    }

    private void UpdatePointsCount()
    {
        if (pointsCountChangeStartTime >= 0f)
        {
            if (pointsCount != targetPointsCount)
            {
                float t = (Time.time - pointsCountChangeStartTime) / pointsChangeDuration;
                pointsCount = (int)Mathf.Lerp(startPointsCount, targetPointsCount, t);
            }
            else
            {
                startPointsCount = targetPointsCount;
                pointsCountChangeStartTime = -1f;
            }
        }
        circleThreadGroupCount = GetThreadGroupCount(circleNumThreadsX, pointsCount);

        circleRadius = Math.Clamp((int)(TexResolution * 3 / Mathf.Sqrt(pointsCount)), 4, 32);

        shader.SetFloat("CircleRadiusF", Math.Max(2, circleRadius - 1));
    }

    private void DispatchKernels()
    {
        //var tempData = new int[tempBuffer.count];
        //tempBuffer.SetData(tempData);

        shader.SetInt(shaderData.PointsCountID, pointsCount);
        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);

        shader.Dispatch(clearOutputTextureKernel, clearThreadGroupCount, clearThreadGroupCount, 1);
        shader.Dispatch(clearIndexTextureKernel, clearThreadGroupCount, clearThreadGroupCount, 1);
        shader.Dispatch(particlesKernel, circleThreadGroupCount, 1, 1);

        for (int i = 1; i < circleRadius; i++)
        {
            shader.SetInt(shaderData.RadiusID, i);
            shader.SetInt(shaderData.RadiusSqrID, i * i);
            shader.Dispatch(circlesKernel, circleThreadGroupCount, 1, 1);
            //shader.Dispatch(circlesKernel, 1, 1, circleThreadGroupCount);
            //shader.Dispatch(diamondsKernel, circleThreadGroupCount, 1, 1);
            //shader.Dispatch(fillCirclesKernel, 1, 1, circleThreadGroupCount);
        }
        shader.Dispatch(lineKernel, 1, 1, 1);

        //tempBuffer.GetData(tempData);
        //Debug.Log($"{GetType().Name}.DispatchKernels: {tempData[0]}");
    }
}
