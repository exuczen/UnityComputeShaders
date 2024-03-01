using UnityEngine;
using System.Collections;
using System;

public class Voronoi : MonoBehaviour
{
    private const int TexResolution = 128;
    private const int CircleRadius = 16;

    [SerializeField]
    private ComputeShader shader = null;
    [SerializeField]
    private Color clearColor = Color.blue;
    [SerializeField]
    private Color circleColor = Color.yellow;
    [SerializeField, Range(1, 65536)]
    private int pointsCount = 16;

    private int circleThreadGroupCount = 1; //[Range(1, 65535)]
    private int clearThreadGroupCount = 1;

    private Renderer rend;
    private RenderTexture outputTexture;

    private int circlesKernel;
    private int diamondsKernel;
    private int fillCirclesKernel;
    private int clearKernel;
    private int lineKernel;
    private int randomParticlesKernel;
    private int particlesKernel;

    private ComputeBuffer particlesBuffer = null;

    public void Init()
    {
        if (Application.isPlaying)
        {
            particlesBuffer?.Release();

            InitData();
            InitShader();
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

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        Init();
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

        shader.GetKernelThreadGroupSizes(circlesKernel, out uint numthreadsX, out _, out _);
        circleThreadGroupCount = Mathf.Clamp((int)((pointsCount + numthreadsX - 1) / numthreadsX), 1, 65535);

        ////shader.GetKernelThreadGroupSizes(circlesKernel, out _, out _, out uint numthreadsZ);
        //shader.GetKernelThreadGroupSizes(fillCirclesKernel, out _, out _, out uint numthreadsZ);
        //circleThreadGroupCount = Mathf.Clamp((int)((pointsCount + numthreadsZ - 1) / numthreadsZ), 1, 65535);

        shader.GetKernelThreadGroupSizes(clearKernel, out numthreadsX, out _, out _);
        clearThreadGroupCount = (int)((TexResolution + numthreadsX - 1) / numthreadsX);

        Debug.Log($"{GetType().Name}.InitData: circleThreadGroupCount: {circleThreadGroupCount}");
    }

    private void InitShader()
    {
        shader.SetInt("TexResolution", TexResolution);
        shader.SetVector("ClearColor", clearColor);
        shader.SetVector("CircleColor", circleColor);
        shader.SetFloat("CircleRadiusF", CircleRadius);
        shader.SetInt("PointsCount", pointsCount);

        int[] textureKernels = new int[] { circlesKernel, diamondsKernel, fillCirclesKernel, lineKernel, clearKernel };
        int[] pointsKernels = new int[] { circlesKernel, diamondsKernel, fillCirclesKernel, lineKernel, randomParticlesKernel, particlesKernel };

        for (int i = 0; i < textureKernels.Length; i++)
        {
            shader.SetTexture(textureKernels[i], "output", outputTexture);
        }
        particlesBuffer = new ComputeBuffer(pointsCount, 2 * sizeof(int) + sizeof(float));

        for (int i = 0; i < pointsKernels.Length; i++)
        {
            shader.SetBuffer(pointsKernels[i], "particlesBuffer", particlesBuffer);
        }
        rend.material.SetTexture("_MainTex", outputTexture);
    }

    private void DispatchKernels()
    {
        int radiusID = Shader.PropertyToID("Radius");
        int radiusSqrID = Shader.PropertyToID("RadiusSqr");

        shader.Dispatch(clearKernel, clearThreadGroupCount, clearThreadGroupCount, 1);
        shader.Dispatch(randomParticlesKernel, circleThreadGroupCount, 1, 1);
        //shader.Dispatch(randomParticlesKernel, 1, 1, circleThreadGroupCount);

        for (int i = 1; i < CircleRadius; i++)
        {
            shader.SetInt(radiusID, i);
            shader.SetInt(radiusSqrID, i * i);
            shader.Dispatch(circlesKernel, circleThreadGroupCount, 1, 1);
            //shader.Dispatch(circlesKernel, 1, 1, circleThreadGroupCount);
            //shader.Dispatch(diamondsKernel, 1, 1, circleThreadGroupCount);
            //shader.Dispatch(fillCirclesKernel, 1, 1, circleThreadGroupCount);
        }
        shader.Dispatch(lineKernel, 1, 1, 1);

        shader.SetFloat("Time", Time.time);
    }

    private void Update()
    {
        DispatchKernels();
    }

    private void OnDestroy()
    {
        particlesBuffer?.Dispose();
    }
}

