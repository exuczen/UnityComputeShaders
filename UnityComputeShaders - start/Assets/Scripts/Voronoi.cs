using UnityEngine;
using System.Collections;
using System;

public class Voronoi : MonoBehaviour
{
    private const int ParticlesCapacity = 1 << 16;
    private const int ParticleSize = 2 * sizeof(int) + 5 * sizeof(float) + sizeof(uint);
    private const int TexResolution = 128;
    private const int CircleRadius = 16;

    private readonly Color[] CircleColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };

    private struct ShaderData
    {
        public int TimeID;
        public int RadiusID;
        public int RadiusSqrID;
        public int PointsCountID;

        public ShaderData(ComputeShader shader)
        {
            TimeID = Shader.PropertyToID("Time");
            RadiusID = Shader.PropertyToID("Radius");
            RadiusSqrID = Shader.PropertyToID("RadiusSqr");
            PointsCountID = Shader.PropertyToID("PointsCount");
        }
    }

    [SerializeField]
    private ComputeShader shader = null;
    [SerializeField]
    private Color clearColor = Color.blue;
    [SerializeField, Range(1, 65536)]
    private int pointsCount = 16;
    [SerializeField, Range(1, 65536)]
    private int targetPointsCount = 16;
    [SerializeField, Range(1, 5)]
    private float pointsChangeDuration = 3;

    private int circleThreadGroupCount = 1; //[Range(1, 65535)]
    private int clearThreadGroupCount = 1;

    private new Renderer renderer = null;
    private RenderTexture outputTexture = null;

    private ComputeBuffer particlesBuffer = null;
    private ComputeBuffer colorsBuffer = null;

    private ShaderData shaderData = default;

    private int circlesKernel;
    private int diamondsKernel;
    private int fillCirclesKernel;
    private int clearKernel;
    private int lineKernel;
    private int randomParticlesKernel;
    private int particlesKernel;

    private uint circleNumThreadsX;
    //private uint circleNumThreadsZ;

    private float pointsCountChangeStartTime;
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

            InitData();
            InitShader();

            StartPointsCountChange();
        }
    }

    private void Start()
    {
        outputTexture = new RenderTexture(TexResolution, TexResolution, 0)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point
        };
        outputTexture.Create();

        renderer = GetComponent<Renderer>();
        renderer.enabled = true;

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
    }

    private void InitData()
    {
        circlesKernel = shader.FindKernel("Circles");
        clearKernel = shader.FindKernel("Clear");
        diamondsKernel = shader.FindKernel("Diamonds");
        fillCirclesKernel = shader.FindKernel("FillCircles");
        lineKernel = shader.FindKernel("Line");
        randomParticlesKernel = shader.FindKernel("RandomParticles");
        particlesKernel = shader.FindKernel("Particles");

        shader.GetKernelThreadGroupSizes(circlesKernel, out circleNumThreadsX, out _, out _);
        circleThreadGroupCount = GetThreadGroupCount(circleNumThreadsX, pointsCount);

        ////shader.GetKernelThreadGroupSizes(circlesKernel, out _, out _, out circleNumThreadsZ);
        //shader.GetKernelThreadGroupSizes(fillCirclesKernel, out _, out _, out circleNumThreadsZ);
        //circleThreadGroupCount = GetThreadGroupCount(circleNumThreadsZ, pointsCount);

        shader.GetKernelThreadGroupSizes(clearKernel, out uint numthreadsX, out _, out _);
        clearThreadGroupCount = GetThreadGroupCount(numthreadsX, TexResolution);

        Debug.Log($"{GetType().Name}.InitData: circleThreadGroupCount: {circleThreadGroupCount}");
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
        shader.SetFloat("CircleRadiusF", Math.Max(1, CircleRadius - 1));
        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);
        shader.SetInt(shaderData.PointsCountID, pointsCount);

        int[] textureKernels = new int[] { circlesKernel, diamondsKernel, fillCirclesKernel, lineKernel, clearKernel };
        int[] pointsKernels = new int[] { circlesKernel, diamondsKernel, fillCirclesKernel, lineKernel, randomParticlesKernel, particlesKernel };

        colorsBuffer = new ComputeBuffer(CircleColors.Length, 4 * sizeof(float));
        colorsBuffer.SetData(CircleColors);

        for (int i = 0; i < textureKernels.Length; i++)
        {
            shader.SetTexture(textureKernels[i], "output", outputTexture);
            shader.SetBuffer(textureKernels[i], "colorsBuffer", colorsBuffer);
        }
        particlesBuffer = new ComputeBuffer(ParticlesCapacity, ParticleSize);

        for (int i = 0; i < pointsKernels.Length; i++)
        {
            shader.SetBuffer(pointsKernels[i], "particlesBuffer", particlesBuffer);
        }
        renderer.material.SetTexture("_MainTex", outputTexture);

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
    }

    private void DispatchKernels()
    {
        shader.SetInt(shaderData.PointsCountID, pointsCount);
        shader.SetFloat(shaderData.TimeID, Time.realtimeSinceStartup);

        shader.Dispatch(clearKernel, clearThreadGroupCount, clearThreadGroupCount, 1);
        shader.Dispatch(particlesKernel, circleThreadGroupCount, 1, 1);

        for (int i = 1; i < CircleRadius; i++)
        {
            shader.SetInt(shaderData.RadiusID, i);
            shader.SetInt(shaderData.RadiusSqrID, i * i);
            shader.Dispatch(circlesKernel, circleThreadGroupCount, 1, 1);
            //shader.Dispatch(circlesKernel, 1, 1, circleThreadGroupCount);
            //shader.Dispatch(diamondsKernel, 1, 1, circleThreadGroupCount);
            //shader.Dispatch(fillCirclesKernel, 1, 1, circleThreadGroupCount);
        }
        shader.Dispatch(lineKernel, 1, 1, 1);
    }
}
