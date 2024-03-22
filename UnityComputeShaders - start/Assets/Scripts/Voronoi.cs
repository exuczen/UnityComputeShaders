#define DEBUG_GUI
#define DEBUG_THREAD_GROUPS
#define DEBUG_POINTS_GUI
//#define DEBUG_ATAN2_GUI
//#define USE_DELAUNAY_SHADER

using UnityEngine;
using System;
using MustHave.Utils;
using MustHave;
using System.Collections.Generic;
using MustHave.UI;

[ExecuteInEditMode]
public class Voronoi : MonoBehaviour
{
    public const int ParticlesCapacity = 1 << 20;

    private const int ParticleSize = 2 * sizeof(int) + 5 * sizeof(float) + sizeof(uint) + sizeof(int);
    private const int TexResolution = 1 << 10;

    private readonly Color[] CircleColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };

    private enum Kernel
    {
        DrawPoints,
        DrawCircles,
        DrawDiamonds,
        DrawLine,
        DrawPairLines,
        FillCircles,
        ClearTextures,
        ClearPairs,
        InitParticles,
        RandomParticles,
        UpdateParticles,
        FindPairs
    }

    private struct ShaderData
    {
        public int TimeID;
        public int RadiusID;
        public int RadiusSqrID;
        public int CircleRadiusID;
        public int CircleRadiusInvID;
        public int PointsCapacityID;
        public int PointsCountID;
        public int PointsRowThreadsCountID;
        public int LinesLerpValueID;

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
            CircleRadiusID = Shader.PropertyToID("CircleRadius");
            CircleRadiusInvID = Shader.PropertyToID("CircleRadiusInv");
            PointsCapacityID = Shader.PropertyToID("PointsCapacity");
            PointsCountID = Shader.PropertyToID("PointsCount");
            PointsRowThreadsCountID = Shader.PropertyToID("PointsRowThreadsCount");
            LinesLerpValueID = Shader.PropertyToID("LinesLerpValue");

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

    private bool IsChangingPointsCount => pointsCountChangeStartTime >= 0f;

    [SerializeField]
    private ComputeShader shader = null;
    [SerializeField]
    private Material delaunayMaterial = null;
    [SerializeField]
    private Color clearColor = Color.clear;
    [SerializeField]
    private bool voronoiVisible = true;
    [SerializeField]
    private bool delaunayVisible = true;
    [SerializeField, Range(0f, 1f), ConditionalHide("delaunayVisible", true)]
    private float delaunayLerpValue = 0.5f;
    [SerializeField]
    private bool squareThreadGroups = false;
    [SerializeField, Range(1, ParticlesCapacity), HideInInspector]
    private int pointsCount = 16;
    [SerializeField, Range(1, 20), HideInInspector]
    private float targetLogPointsCount = 4;
    [SerializeField, Range(1, 5), HideInInspector]
    private float pointsChangeDuration = 3;

    private Vector3Int circleThreadGroups = Vector3Int.one; //[Range(1, 65535)]
    private Vector3Int clearThreadGroups = Vector3Int.one;
    private Vector3Int pairsThreadGroups = Vector3Int.one;
    private uint circleThreadGroupSize;

    private new Renderer renderer = null;
    private RenderTexture outputTexture = null;

    private ComputeBuffer particlesBuffer = null;
    private ComputeBuffer colorsBuffer = null;
    private ComputeBuffer indexBuffer = null;
    private ComputeBuffer angularPairBuffer = null;
    private ComputeBuffer tempBuffer = null;

    private ShaderData shaderData = default;

    private int[] kernelIDs = null;

    private int circleRadius = 16;

    private readonly uint[] circleNumThreads = new uint[3];

    private int angularPairsStride = 0;

    private float pointsCountChangeStartTime = -1f;
    private float pointsCountChangeEndTime = -1f;
    private float startLogPointsCount;
    private float targetLogPointsCountPrev;

    public void StartPointsCountChange()
    {
        StartPointsCountChange(pointsChangeDuration);
    }

    public void StartPointsCountChange(float duration)
    {
        if (pointsCount != TargetPointsCount)
        {
            //Debug.Log($"{GetType().Name}.StartPointsCountChange: {pointsCount} -> {TargetPointsCount}");
            pointsCountChangeStartTime = Time.time;
            pointsCountChangeEndTime = pointsCountChangeStartTime + duration;
            startLogPointsCount = Mathf.Log(pointsCount, 2f);
            targetLogPointsCountPrev = targetLogPointsCount;
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

        if (!Application.isPlaying)
        {
            return;
        }
        CreateTextures();
        Init();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        UpdatePointsCount();
        DispatchKernels();
    }

#if USE_DELAUNAY_SHADER
    private void OnRenderObject()
    {
        if (delaunayVisible)
        {
            delaunayMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Lines, 2, pointsCount * angularPairsStride);
        }
    }
#endif

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
            filterMode = FilterMode.Bilinear
        };
        outputTexture.Create();
    }

    private void FindKernels()
    {
        var kernelNames = EnumUtils.GetNames<Kernel>();
        kernelIDs = new int[kernelNames.Length];

        for (int i = 0; i < kernelNames.Length; i++)
        {
            kernelIDs[i] = shader.FindKernel(kernelNames[i]);
        }
    }

    private int GetKernelID(Kernel kernel) => kernelIDs[(int)kernel];

    private void GetKernelThreadGroupSizes(Kernel kernel, uint[] numthreads)
    {
        shader.GetKernelThreadGroupSizes(GetKernelID(kernel), out numthreads[0], out numthreads[1], out numthreads[2]);
    }

    private void GetThreadGroupSizes()
    {
        GetKernelThreadGroupSizes(Kernel.DrawCircles, circleNumThreads);
        circleThreadGroupSize = circleNumThreads[0] * circleNumThreads[1] * circleNumThreads[2];

        var numthreads = new uint[3];
        GetKernelThreadGroupSizes(Kernel.ClearTextures, numthreads);
        clearThreadGroups.x = GetThreadGroupCount(numthreads[0], TexResolution);
        clearThreadGroups.y = GetThreadGroupCount(numthreads[1], TexResolution);
        clearThreadGroups.z = 1;

        GetKernelThreadGroupSizes(Kernel.DrawPairLines, numthreads);
        angularPairsStride = (int)numthreads[2];
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
        pairsThreadGroups.Set(circleThreadGroups.x, circleThreadGroups.y, 1);

        shader.SetInt(shaderData.PointsRowThreadsCountID, (int)circleNumThreads[0] * circleThreadGroups.x);
        shader.SetInt(shaderData.PointsCountID, pointsCount);

        circleRadius = Mathf.Clamp((int)(TexResolution * 2.3 / Mathf.Sqrt(pointsCount)), 3, 32);
        shader.SetInt(shaderData.CircleRadiusID, circleRadius);
        shader.SetFloat(shaderData.CircleRadiusInvID, 1f / circleRadius);

        delaunayLerpValue = Mathf.InverseLerp(20, 0, Mathf.Log(pointsCount, 2f));
        delaunayLerpValue = Mathf.Clamp(delaunayLerpValue, 0.15f, 0.5f);
        shader.SetFloat(shaderData.LinesLerpValueID, delaunayLerpValue);

        if (log)
        {
            Debug.Log($"{GetType().Name}.SetPointsCount: circleThreadGroups: {circleThreadGroups} circleNumThreads: ({circleNumThreads[0]}, {circleNumThreads[1]}, {circleNumThreads[2]})");
        }
    }

    private int GetThreadGroupCount(uint numthreads, int size, bool clamp = true)
    {
        if (numthreads == 0)
        {
            return 0;
        }
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

        colorsBuffer = new ComputeBuffer(CircleColors.Length, 4 * sizeof(float));
        colorsBuffer.SetData(CircleColors);
        particlesBuffer = new ComputeBuffer(ParticlesCapacity, ParticleSize);
        indexBuffer = new ComputeBuffer(TexResolution * TexResolution, sizeof(int));
        angularPairBuffer = new ComputeBuffer(ParticlesCapacity * angularPairsStride, sizeof(int));
        tempBuffer = new ComputeBuffer(1, sizeof(int));

        for (int i = 0; i < kernelIDs.Length; i++)
        {
            int kernelID = kernelIDs[i];
            shader.SetTexture(kernelID, shaderData.OutputTextureID, outputTexture);
            shader.SetBuffer(kernelID, shaderData.ColorsBufferID, colorsBuffer);
            shader.SetBuffer(kernelID, shaderData.ParticlesBufferID, particlesBuffer);
            shader.SetBuffer(kernelID, shaderData.IndexBufferID, indexBuffer);
            shader.SetBuffer(kernelID, shaderData.AngularPairBufferID, angularPairBuffer);
            shader.SetBuffer(kernelID, shaderData.TempBufferID, tempBuffer);
        }
        renderer.material.SetTexture("_MainTex", outputTexture);

        delaunayMaterial.SetTexture("_MainTex", outputTexture);
        delaunayMaterial.SetInteger("_HalfRes", TexResolution >> 1);
        delaunayMaterial.SetInteger("_AngularPairsStride", angularPairsStride);
        delaunayMaterial.SetFloat("_Scale", transform.localScale.x / TexResolution);
        delaunayMaterial.SetBuffer("particlesBuffer", particlesBuffer);
        delaunayMaterial.SetBuffer("angularPairBuffer", angularPairBuffer);

        DispatchKernel(Kernel.ClearTextures, clearThreadGroups);

        int pointsCount = this.pointsCount;

        SetPointsCount(ParticlesCapacity, true);

        DispatchKernel(Kernel.InitParticles, circleThreadGroups);

        SetPointsCount(this.pointsCount = pointsCount, true);

        DispatchKernel(Kernel.RandomParticles, circleThreadGroups);
    }

    private void UpdatePointsCount()
    {
        if (pointsCountChangeStartTime >= 0f)
        {
            if (targetLogPointsCountPrev != targetLogPointsCount)
            {
                StartPointsCountChange(pointsCountChangeEndTime - Time.time);
            }
            targetLogPointsCountPrev = targetLogPointsCount;

            if (pointsCount != TargetPointsCount)
            {
                //float t = (Time.time - pointsCountChangeStartTime) / pointsChangeDuration;
                float t = Mathf.InverseLerp(pointsCountChangeStartTime, pointsCountChangeEndTime, Time.time);
                float power = Mathf.Lerp(startLogPointsCount, targetLogPointsCount, t);
                pointsCount = (int)(Mathf.Pow(2f, power) + Mathv.Epsilon);
            }
            else
            {
                startLogPointsCount = targetLogPointsCount;
                pointsCountChangeStartTime = -1f;
                pointsCountChangeEndTime = -1f;
            }
        }
        SetPointsCount(pointsCount, false);
    }

    private void DispatchKernel(int kernelID, Vector3Int threadGroups)
    {
        shader.Dispatch(kernelID, threadGroups.x, threadGroups.y, threadGroups.z);
    }

    private void DispatchKernel(Kernel kernel, Vector3Int threadGroups)
    {
        DispatchKernel(GetKernelID(kernel), threadGroups);
    }

    private void DispatchKernels()
    {
        //var tempData = new int[tempBuffer.count];
        //tempBuffer.SetData(tempData);

        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);

        DispatchKernel(Kernel.ClearTextures, clearThreadGroups);
        DispatchKernel(Kernel.ClearPairs, pairsThreadGroups);

        DispatchKernel(Kernel.DrawPoints, circleThreadGroups);
        DispatchKernel(Kernel.UpdateParticles, circleThreadGroups);

        int drawCirclesKernelID = GetKernelID(Kernel.DrawCircles);

        for (int i = 1; i < circleRadius; i++)
        {
            shader.SetInt(shaderData.RadiusID, i);
            shader.SetInt(shaderData.RadiusSqrID, i * i);
            DispatchKernel(drawCirclesKernelID, circleThreadGroups);
            //DispatchKernel(drawDiamondsKernelID, circleThreadGroups);
            //DispatchKernel(fillCirclesKernelID, circleThreadGroups);
        }
        if (delaunayVisible)
        {
            DispatchKernel(Kernel.FindPairs, clearThreadGroups);
            if (!voronoiVisible)
            {
                outputTexture.Clear(clearColor);
            }
#if !USE_DELAUNAY_SHADER
            DispatchKernel(Kernel.DrawPairLines, pairsThreadGroups);
#endif
        }
        else
        {
            voronoiVisible = true;
        }
        //DispatchKernel(Kernel.DrawLine, Vector3Int.one);

        //tempBuffer.GetData(tempData);
        //Debug.Log($"{GetType().Name}.DispatchKernels: {tempData[0]}");
    }

#if DEBUG_GUI
    private void OnGUI()
    {
        int dy = 15;
        int y = -dy;
        int lineHeight = 20;
        Rect getTextLineRect(int width = 100, int height = 20) => new(10, y += dy, width, height);

        Color guiColor = GUI.color;
        GUI.color = Color.black;
        GUI.Box(new Rect(0, 0, 200, 100), string.Empty);
        GUI.color = guiColor;
#if DEBUG_ATAN2_GUI
        var cursorPos = Camera.main.ScreenToViewportPoint(Input.mousePosition) - 0.5f * Vector3.one;
        cursorPos.x *= 1f * Screen.width / Screen.height;
        float angle = Mathf.PI - Mathf.Atan2(cursorPos.y, cursorPos.x);
        int angleDivisions = 16;
        float deltaAngle = Maths.M_2PI / angleDivisions;
        //int angleSection = (int)(angle / deltaAngle + 0.5f) % angleDivisions;
        int angleSection = (int)(angle / deltaAngle);
        GUI.Label(getTextLineRect(200), $"atan2({cursorPos.y:f2}, {cursorPos.x:f2}) = {angle * Mathf.Rad2Deg:f2}");
        GUI.Label(getTextLineRect(200), $"angleSection: {angleSection}");
#endif
#if DEBUG_THREAD_GROUPS
        GUI.Label(getTextLineRect(), $"({pointsCount})");
        GUI.Label(getTextLineRect(), $"({circleThreadGroups.x * circleThreadGroups.y * circleThreadGroupSize})");
        GUI.Label(getTextLineRect(), $"({GetThreadGroupCount(circleThreadGroupSize, pointsCount, false)})");
        GUI.Label(getTextLineRect(), $"{circleThreadGroups}");
        GUI.Label(getTextLineRect(), $"({circleNumThreads[0]}, {circleNumThreads[1]}, {circleNumThreads[2]})");
#endif
#if DEBUG_POINTS_GUI
        y += Screen.height >> 2;
        GUI.color = Color.white;
        GUI.Box(new Rect(0, y, 100, 120), string.Empty);
        GUI.color = guiColor;
        voronoiVisible = GUI.Toggle(getTextLineRect(), voronoiVisible, " voronoi");
        y += 5;
        delaunayVisible = GUI.Toggle(getTextLineRect(), delaunayVisible, " delaunay");
        y += lineHeight;
        GUI.Label(getTextLineRect(100, 40), $"({pointsCount})");
        GUI.Label(getTextLineRect(100, 40), $"({TargetPointsCount})");
        y += lineHeight;
        y += lineHeight;
        // Sliders
        {
            int logMax = Maths.Log2(ParticlesCapacity);
            int logPointsCount = Maths.Log2((uint)pointsCount);
            int power = (int)GUI.VerticalSlider(new Rect(10, y, 20, 200), logPointsCount, logMax, 0);
            if (logPointsCount != power)
            {
                pointsCount = 1 << power;
            }
            targetLogPointsCount = (int)GUI.VerticalSlider(new Rect(30, y, 20, 200), targetLogPointsCount, logMax, 0);
        }
        y += 200;
        GUI.color = IsChangingPointsCount ? Color.red : guiColor;
        if (GUI.Button(getTextLineRect(30), ">"))
        {
            StartPointsCountChange();
        }
        GUI.color = guiColor;
#endif
    }
#endif
}
