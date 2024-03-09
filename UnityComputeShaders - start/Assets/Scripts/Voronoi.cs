using UnityEngine;
using System.Collections;
using System;
using MustHave.Utils;

public class Voronoi : MonoBehaviour
{
    public const int ParticlesCapacity = 1 << 20;
    private const int ParticleSize = 2 * sizeof(int) + 5 * sizeof(float) + sizeof(uint) + sizeof(int);
    private const int TexResolution = 1 << 10;
    private const int PairAngularDivisions = 9; //181;

    private readonly Color[] CircleColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };

    private struct ShaderData
    {
        public int TimeID;
        public int RadiusID;
        public int RadiusSqrID;
        public int CircleRadiusFID;
        public int PointsCountID;
        public int PointsCapacityID;
        public int SqrtPointsCountID;

        public int OutputTextureID;
        public int ColorsBufferID;
        public int ParticlesBufferID;
        public int IndexBufferID;
        public int AngularPairBufferID;
        public int TempBufferID;

        public ShaderData(ComputeShader shader)
        {
            TimeID = Shader.PropertyToID("Time");
            RadiusID = Shader.PropertyToID("Radius");
            RadiusSqrID = Shader.PropertyToID("RadiusSqr");
            CircleRadiusFID = Shader.PropertyToID("CircleRadiusF");
            PointsCountID = Shader.PropertyToID("PointsCount");
            PointsCapacityID = Shader.PropertyToID("PointsCapacity");
            SqrtPointsCountID = Shader.PropertyToID("SqrtPointsCount");

            OutputTextureID = Shader.PropertyToID("outputTexture");
            ColorsBufferID = Shader.PropertyToID("colorsBuffer");
            ParticlesBufferID = Shader.PropertyToID("particlesBuffer");
            IndexBufferID = Shader.PropertyToID("indexBuffer");
            AngularPairBufferID = Shader.PropertyToID("angularPairBuffer");
            TempBufferID = Shader.PropertyToID("tempBuffer");
        }
    }

    public int PointsCount { get => pointsCount; set => pointsCount = value; }
    public int TargetPointsCount { get => targetPointsCount; set => targetPointsCount = value; }
    public int CircleRadius => circleRadius;

    [SerializeField]
    private ComputeShader shader = null;
    [SerializeField]
    private Color clearColor = Color.clear;
    [SerializeField, Range(1, ParticlesCapacity), HideInInspector]
    private int pointsCount = 16;
    [SerializeField, Range(1, ParticlesCapacity), HideInInspector]
    private int targetPointsCount = 16;
    [SerializeField, Range(1, 5)]
    private float pointsChangeDuration = 3;

    private Vector3Int circleThreadGroups = Vector3Int.one; //[Range(1, 65535)]
    private Vector3Int clearThreadGroups = Vector3Int.one;

    private new Renderer renderer = null;
    private RenderTexture outputTexture = null;

    private ComputeBuffer particlesBuffer = null;
    private ComputeBuffer colorsBuffer = null;
    private ComputeBuffer indexBuffer = null;
    private ComputeBuffer angularPairBuffer = null;
    private ComputeBuffer tempBuffer = null;

    private ShaderData shaderData = default;

    private int circlesKernel;
    private int diamondsKernel;
    private int fillCirclesKernel;
    private int lineKernel;
    private int clearTexturesKernel;
    private int initParticlesKernel;
    private int randomParticlesKernel;
    private int particlesKernel;
    private int pointsKernel;

    private int circleRadius = 16;

    private readonly uint[] circleNumThreads = new uint[3];
    private readonly uint[] clearNumThreads = new uint[3];

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
            indexBuffer?.Release();
            angularPairBuffer?.Release();
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
        indexBuffer?.Dispose();
        angularPairBuffer?.Dispose();
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
    }

    private void FindKernels()
    {
        circlesKernel = shader.FindKernel("Circles");
        clearTexturesKernel = shader.FindKernel("ClearTextures");
        diamondsKernel = shader.FindKernel("Diamonds");
        fillCirclesKernel = shader.FindKernel("FillCircles");
        lineKernel = shader.FindKernel("Line");
        initParticlesKernel = shader.FindKernel("InitParticles");
        randomParticlesKernel = shader.FindKernel("RandomParticles");
        particlesKernel = shader.FindKernel("Particles");
        pointsKernel = shader.FindKernel("Points");
    }

    private void SetThreadGroupCounts()
    {
        shader.GetKernelThreadGroupSizes(circlesKernel, out circleNumThreads[0], out circleNumThreads[1], out circleNumThreads[2]);
        circleThreadGroups.x = GetThreadGroupCount(circleNumThreads[0], pointsCount);
        circleThreadGroups.y = 1;
        circleThreadGroups.z = 1;

        shader.GetKernelThreadGroupSizes(clearTexturesKernel, out clearNumThreads[0], out clearNumThreads[1], out clearNumThreads[2]);
        clearThreadGroups.x = GetThreadGroupCount(clearNumThreads[0], TexResolution);
        clearThreadGroups.y = GetThreadGroupCount(clearNumThreads[1], TexResolution);
        clearThreadGroups.z = 1;

        Debug.Log($"{GetType().Name}.SetThreadGroupCounts: circleThreadGroups: {circleThreadGroups}");
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
        shader.SetVector("ClearColor", ColorUtils.ColorWithAlpha(clearColor, 0f));
        shader.SetFloat(shaderData.CircleRadiusFID, Math.Max(2, circleRadius - 1));
        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);
        shader.SetInt(shaderData.PointsCountID, pointsCount);
        shader.SetInt(shaderData.PointsCapacityID, ParticlesCapacity);

        int[] kernels = new int[]
        {
            circlesKernel,
            diamondsKernel,
            fillCirclesKernel,
            lineKernel,
            clearTexturesKernel,
            initParticlesKernel,
            randomParticlesKernel,
            particlesKernel,
            pointsKernel
        };
        colorsBuffer = new ComputeBuffer(CircleColors.Length, 4 * sizeof(float));
        colorsBuffer.SetData(CircleColors);
        particlesBuffer = new ComputeBuffer(ParticlesCapacity, ParticleSize);
        indexBuffer = new ComputeBuffer(TexResolution * TexResolution, sizeof(int));
        angularPairBuffer = new ComputeBuffer(ParticlesCapacity * PairAngularDivisions, 2 * sizeof(int));
        tempBuffer = new ComputeBuffer(1, sizeof(int));

        for (int i = 0; i < kernels.Length; i++)
        {
            shader.SetTexture(kernels[i], shaderData.OutputTextureID, outputTexture);
            shader.SetBuffer(kernels[i], shaderData.ColorsBufferID, colorsBuffer);
            shader.SetBuffer(kernels[i], shaderData.ParticlesBufferID, particlesBuffer);
            shader.SetBuffer(kernels[i], shaderData.IndexBufferID, indexBuffer);
            shader.SetBuffer(kernels[i], shaderData.AngularPairBufferID, angularPairBuffer);
            shader.SetBuffer(kernels[i], shaderData.TempBufferID, tempBuffer);
        }
        renderer.material.SetTexture("_MainTex", outputTexture);

        shader.Dispatch(clearTexturesKernel, clearThreadGroups.x, clearThreadGroups.y, clearThreadGroups.z);

        shader.Dispatch(initParticlesKernel, GetThreadGroupCount(circleNumThreads[0], ParticlesCapacity), 1, 1);

        shader.Dispatch(randomParticlesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
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
        circleThreadGroups.x = GetThreadGroupCount(circleNumThreads[0], pointsCount);
        circleThreadGroups.y = 1;
        circleThreadGroups.z = 1;

        circleRadius = Math.Clamp((int)(TexResolution * 2.3 / Mathf.Sqrt(pointsCount)), 3, 32);

        shader.SetFloat(shaderData.CircleRadiusFID, Math.Max(2, circleRadius + 1));
    }

    private void DispatchKernels()
    {
        //var tempData = new int[tempBuffer.count];
        //tempBuffer.SetData(tempData);

        shader.SetInt(shaderData.PointsCountID, pointsCount);
        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);

        shader.Dispatch(clearTexturesKernel, clearThreadGroups.x, clearThreadGroups.y, clearThreadGroups.z);

        shader.Dispatch(pointsKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
        shader.Dispatch(particlesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);

        for (int i = 1; i < circleRadius; i++)
        {
            shader.SetInt(shaderData.RadiusID, i);
            shader.SetInt(shaderData.RadiusSqrID, i * i);
            shader.Dispatch(circlesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
            //shader.Dispatch(diamondsKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
            //shader.Dispatch(fillCirclesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
        }
        //shader.Dispatch(lineKernel, 1, 1, 1);

        //tempBuffer.GetData(tempData);
        //Debug.Log($"{GetType().Name}.DispatchKernels: {tempData[0]}");
    }
}
