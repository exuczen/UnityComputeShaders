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

    private int pointsHandle;
    private int circlesHandle;
    private int diamondsHandle;
    private int fillCirclesHandle;
    private int clearHandle;
    private int lineHandle;

    private ComputeBuffer pointsBuffer = null;

    public void Init()
    {
        if (Application.isPlaying)
        {
            pointsBuffer?.Release();

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
        pointsHandle = shader.FindKernel("Points");
        circlesHandle = shader.FindKernel("Circles");
        clearHandle = shader.FindKernel("Clear");
        diamondsHandle = shader.FindKernel("Diamonds");
        fillCirclesHandle = shader.FindKernel("FillCircles");
        lineHandle = shader.FindKernel("Line");

        shader.GetKernelThreadGroupSizes(circlesHandle, out uint numthreadsX, out _, out _);
        circleThreadGroupCount = Mathf.Clamp((int)((pointsCount + numthreadsX - 1) / numthreadsX), 1, 65535);

        ////shader.GetKernelThreadGroupSizes(circlesHandle, out _, out _, out uint numthreadsZ);
        //shader.GetKernelThreadGroupSizes(fillCirclesHandle, out _, out _, out uint numthreadsZ);
        //circleThreadGroupCount = Mathf.Clamp((int)((pointsCount + numthreadsZ - 1) / numthreadsZ), 1, 65535);

        shader.GetKernelThreadGroupSizes(clearHandle, out numthreadsX, out _, out _);
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

        int[] textureHandles = new int[] { clearHandle, circlesHandle, diamondsHandle, fillCirclesHandle, lineHandle };
        int[] pointsHandles = new int[] { pointsHandle, circlesHandle, diamondsHandle, fillCirclesHandle, lineHandle };

        for (int i = 0; i < textureHandles.Length; i++)
        {
            shader.SetTexture(textureHandles[i], "Result", outputTexture);
        }
        pointsBuffer = new ComputeBuffer(pointsCount, 2 * sizeof(int));

        for (int i = 0; i < pointsHandles.Length; i++)
        {
            shader.SetBuffer(pointsHandles[i], "PointsBuffer", pointsBuffer);
        }
        rend.material.SetTexture("_MainTex", outputTexture);
    }

    private void DispatchKernels()
    {
        int radiusID = Shader.PropertyToID("Radius");
        int radiusSqrID = Shader.PropertyToID("RadiusSqr");

        shader.Dispatch(clearHandle, clearThreadGroupCount, clearThreadGroupCount, 1);
        shader.Dispatch(pointsHandle, circleThreadGroupCount, 1, 1);
        //shader.Dispatch(pointsHandle, 1, 1, circleThreadGroupCount);

        for (int i = 1; i < CircleRadius; i++)
        {
            shader.SetInt(radiusID, i);
            shader.SetInt(radiusSqrID, i * i);
            shader.Dispatch(circlesHandle, circleThreadGroupCount, 1, 1);
            //shader.Dispatch(circlesHandle, 1, 1, circleThreadGroupCount);
            //shader.Dispatch(diamondsHandle, 1, 1, circleThreadGroupCount);
            //shader.Dispatch(fillCirclesHandle, 1, 1, circleThreadGroupCount);
        }
        shader.Dispatch(lineHandle, 1, 1, 1);

        shader.SetFloat("Time", Time.time);
    }

    private void Update()
    {
        DispatchKernels();
    }

    private void OnDestroy()
    {
        pointsBuffer.Dispose();
    }
}

