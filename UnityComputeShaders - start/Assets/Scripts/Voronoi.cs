#define DEBUG_GUI
#define DEBUG_THREAD_GROUPS

using UnityEngine;
using System;
using MustHave.Utils;
using MustHave;

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
        public int PointsCapacityID;
        public int PointsCountID;
        public int PointsRowThreadsCountID;

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
            PointsCapacityID = Shader.PropertyToID("PointsCapacity");
            PointsCountID = Shader.PropertyToID("PointsCount");
            PointsRowThreadsCountID = Shader.PropertyToID("PointsRowThreadsCount");

            OutputTextureID = Shader.PropertyToID("outputTexture");
            ColorsBufferID = Shader.PropertyToID("colorsBuffer");
            ParticlesBufferID = Shader.PropertyToID("particlesBuffer");
            IndexBufferID = Shader.PropertyToID("indexBuffer");
            AngularPairBufferID = Shader.PropertyToID("angularPairBuffer");
            TempBufferID = Shader.PropertyToID("tempBuffer");
        }
    }

    public int PointsCount { get => pointsCount; set => pointsCount = value; }
    public int TargetPointsCount { get => 1 << TargetLogPointsCount; }
    public int TargetLogPointsCount { get => (int)(targetLogPointsCount + Mathv.Epsilon); set => targetLogPointsCount = value; }
    public int CircleRadius => circleRadius;

    [SerializeField]
    private ComputeShader shader = null;
    [SerializeField]
    private Color clearColor = Color.clear;
    [SerializeField]
    private bool squareThreadGroups = false;
    [SerializeField, Range(1, ParticlesCapacity), HideInInspector]
    private int pointsCount = 16;
    [SerializeField, Range(1, 20), HideInInspector]
    private float targetLogPointsCount = 4;
    [SerializeField, Range(1, 5)]
    private float pointsChangeDuration = 3;

    private Vector3Int circleThreadGroups = Vector3Int.one; //[Range(1, 65535)]
    private Vector3Int clearThreadGroups = Vector3Int.one;
    private uint circleThreadGroupSize;

    private new Renderer renderer = null;
    private RenderTexture outputTexture = null;

    private ComputeBuffer particlesBuffer = null;
    private ComputeBuffer colorsBuffer = null;
    private ComputeBuffer indexBuffer = null;
    private ComputeBuffer angularPairBuffer = null;
    private ComputeBuffer tempBuffer = null;

    private ShaderData shaderData = default;

    private int drawPointsKernel;
    private int drawCirclesKernel;
    private int drawDiamondsKernel;
    private int drawLineKernel;
    private int fillCirclesKernel;
    private int clearTexturesKernel;
    private int initParticlesKernel;
    private int randomParticlesKernel;
    private int updateParticlesKernel;

    private int circleRadius = 16;

    private readonly uint[] circleNumThreads = new uint[3];
    private readonly uint[] clearNumThreads = new uint[3];

    private float pointsCountChangeStartTime = -1f;
    private float startLogPointsCount;

    public void StartPointsCountChange()
    {
        if (pointsCount != TargetPointsCount)
        {
            //Debug.Log($"{GetType().Name}.StartPointsCountChange: {pointsCount} -> {TargetPointsCount}");
            pointsCountChangeStartTime = Time.time;
            startLogPointsCount = Mathf.Log(pointsCount, 2f);
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
            GetThreadGroupSizes();
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
        drawPointsKernel = shader.FindKernel("DrawPoints");
        drawCirclesKernel = shader.FindKernel("DrawCircles");
        drawDiamondsKernel = shader.FindKernel("DrawDiamonds");
        drawLineKernel = shader.FindKernel("DrawLine");
        fillCirclesKernel = shader.FindKernel("FillCircles");
        clearTexturesKernel = shader.FindKernel("ClearTextures");
        initParticlesKernel = shader.FindKernel("InitParticles");
        randomParticlesKernel = shader.FindKernel("RandomParticles");
        updateParticlesKernel = shader.FindKernel("UpdateParticles");
    }

    private void GetThreadGroupSizes()
    {
        shader.GetKernelThreadGroupSizes(drawCirclesKernel, out circleNumThreads[0], out circleNumThreads[1], out circleNumThreads[2]);
        circleThreadGroupSize = circleNumThreads[0] * circleNumThreads[1] * circleNumThreads[2];

        shader.GetKernelThreadGroupSizes(clearTexturesKernel, out clearNumThreads[0], out clearNumThreads[1], out clearNumThreads[2]);
        clearThreadGroups.x = GetThreadGroupCount(clearNumThreads[0], TexResolution);
        clearThreadGroups.y = GetThreadGroupCount(clearNumThreads[1], TexResolution);
        clearThreadGroups.z = 1;
    }

    private void SetPointsCount(int pointsCount, bool log)
    {
        this.pointsCount = pointsCount;

        int groupsCount = GetThreadGroupCount(circleThreadGroupSize, pointsCount, !squareThreadGroups);

        if (squareThreadGroups)
        {
            int sqrtGroupsCount = (int)(Mathf.Sqrt(groupsCount) + Mathv.Epsilon);
            int xCount = sqrtGroupsCount;
            int yCount = sqrtGroupsCount;
            if (xCount * yCount < groupsCount)
            {
                xCount++;
                if (xCount * yCount < groupsCount)
                {
                    yCount++;
                    if (xCount * yCount < groupsCount)
                    {
                        throw new Exception($"SetPointsCount: [{xCount}, {yCount}]");
                    }
                }
            }
            circleThreadGroups.Set(xCount, yCount, 1);
        }
        else
        {
            circleThreadGroups.Set(groupsCount, 1, 1);
        }
        shader.SetInt(shaderData.PointsRowThreadsCountID, (int)circleNumThreads[0] * circleThreadGroups.x);
        shader.SetInt(shaderData.PointsCountID, pointsCount);

        circleRadius = Mathf.Clamp((int)(TexResolution * 2.3 / Mathf.Sqrt(pointsCount)), 3, 32);
        shader.SetFloat(shaderData.CircleRadiusFID, Mathf.Max(2, circleRadius + 1));

        if (log)
        {
            Debug.Log($"{GetType().Name}.SetPointsCount: circleThreadGroups: {circleThreadGroups} circleNumThreads: ({circleNumThreads[0]}, {circleNumThreads[1]}, {circleNumThreads[2]})");
        }
    }

    private int GetThreadGroupCount(uint numthreads, int size, bool clamp = true)
    {
        int n = (int)numthreads;
        int count = (size + n - 1) / n;
        return clamp ? Mathf.Clamp(count, 1, 65535) : count;
    }

    private void InitShader()
    {
        shaderData = new(shader);

        shader.SetInt("TexResolution", TexResolution);
        shader.SetVector("ClearColor", ColorUtils.ColorWithAlpha(clearColor, 0f));
        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);
        shader.SetInt(shaderData.PointsCapacityID, ParticlesCapacity);

        int[] kernels = new int[]
        {
            drawCirclesKernel,
            drawDiamondsKernel,
            fillCirclesKernel,
            drawLineKernel,
            clearTexturesKernel,
            initParticlesKernel,
            randomParticlesKernel,
            updateParticlesKernel,
            drawPointsKernel
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

        int pointsCount = this.pointsCount;

        SetPointsCount(ParticlesCapacity, true);

        shader.Dispatch(initParticlesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);

        SetPointsCount(this.pointsCount = pointsCount, true);

        shader.Dispatch(randomParticlesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
    }

    private void UpdatePointsCount()
    {
        if (pointsCountChangeStartTime >= 0f)
        {
            if (pointsCount != TargetPointsCount)
            {
                float t = (Time.time - pointsCountChangeStartTime) / pointsChangeDuration;
                float power = Mathf.Lerp(startLogPointsCount, targetLogPointsCount, t);
                pointsCount = (int)(Mathf.Pow(2f, power) + Mathv.Epsilon);
            }
            else
            {
                startLogPointsCount = targetLogPointsCount;
                pointsCountChangeStartTime = -1f;
            }
        }
        SetPointsCount(pointsCount, false);
    }

    private void DispatchKernels()
    {
        //var tempData = new int[tempBuffer.count];
        //tempBuffer.SetData(tempData);

        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);

        shader.Dispatch(clearTexturesKernel, clearThreadGroups.x, clearThreadGroups.y, clearThreadGroups.z);

        shader.Dispatch(drawPointsKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
        shader.Dispatch(updateParticlesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);

        for (int i = 1; i < circleRadius; i++)
        {
            shader.SetInt(shaderData.RadiusID, i);
            shader.SetInt(shaderData.RadiusSqrID, i * i);
            shader.Dispatch(drawCirclesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
            //shader.Dispatch(drawDiamondsKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
            //shader.Dispatch(fillCirclesKernel, circleThreadGroups.x, circleThreadGroups.y, circleThreadGroups.z);
        }
        //shader.Dispatch(drawLineKernel, 1, 1, 1);

        //tempBuffer.GetData(tempData);
        //Debug.Log($"{GetType().Name}.DispatchKernels: {tempData[0]}");
    }

#if DEBUG_GUI
    private void OnGUI()
    {
        int y = 0;
        int dy = 15;

        Color activeColor = GUI.color;
        GUI.color = Color.black;
        GUI.Box(new Rect(0, 0, 100, 100), string.Empty);
        GUI.color = activeColor;

#if DEBUG_THREAD_GROUPS
        GUI.Label(new Rect(10, y += dy, 100, 200), $"({pointsCount})");
        GUI.Label(new Rect(10, y += dy, 100, 200), $"({circleThreadGroups.x * circleThreadGroups.y * circleThreadGroupSize})");
        GUI.Label(new Rect(10, y += dy, 100, 200), $"({GetThreadGroupCount(circleThreadGroupSize, pointsCount, false)})");
        GUI.Label(new Rect(10, y += dy, 100, 200), $"{circleThreadGroups}");
        GUI.Label(new Rect(10, y += dy, 100, 200), $"({circleNumThreads[0]}, {circleNumThreads[1]}, {circleNumThreads[2]})");
#endif
    }
#endif
}
