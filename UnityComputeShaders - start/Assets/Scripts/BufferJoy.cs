﻿using UnityEngine;

public class BufferJoy : MonoBehaviour
{
    public ComputeShader shader;
    public int texResolution = 1024;

    private Renderer rend;
    private RenderTexture outputTexture;

    private int circlesHandle;
    private int clearHandle;

    public Color clearColor = new();
    public Color circleColor = new();

    private struct Circle
    {
        public Vector2 origin;
        public Vector2 velocity;
        public float radius;
    }

    private readonly int count = 10;
    private Circle[] circleData = null;

    private ComputeBuffer buffer = null;

    // Use this for initialization
    private void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0)
        {
            enableRandomWrite = true
        };
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitData();

        InitShader();
    }

    private void InitData()
    {
        circlesHandle = shader.FindKernel("Circles");

        shader.GetKernelThreadGroupSizes(circlesHandle, out uint threadGroupSizeX, out _, out _);

        int total = (int)threadGroupSizeX * count;
        circleData = new Circle[total];

        float speed = 100f;
        float halfSpeed = speed * 0.5f;
        float minRadius = 10f;
        float maxRadius = 30f;
        float radiusRangle = maxRadius - minRadius;

        for (int i = 0; i < total; i++)
        {
            var circle = circleData[i];
            circle.origin.x = Random.value * texResolution;
            circle.origin.y = Random.value * texResolution;
            circle.velocity.x = Random.value * speed - halfSpeed;
            circle.velocity.y = Random.value * speed - halfSpeed;
            circle.radius = Random.value * radiusRangle + minRadius;
            circleData[i] = circle;
        }
    }

    private void InitShader()
    {
        clearHandle = shader.FindKernel("Clear");

        shader.SetVector("clearColor", clearColor);
        shader.SetVector("circleColor", circleColor);
        shader.SetInt("texResolution", texResolution);

        shader.SetTexture(clearHandle, "Result", outputTexture);
        shader.SetTexture(circlesHandle, "Result", outputTexture);

        int stride = (2 + 2 + 1) * sizeof(float);
        buffer = new ComputeBuffer(circleData.Length, stride);
        buffer.SetData(circleData);
        shader.SetBuffer(circlesHandle, "circlesBuffer", buffer);

        rend.material.SetTexture("_MainTex", outputTexture);
    }

    private void DispatchKernels(int count)
    {
        shader.Dispatch(clearHandle, texResolution / 8, texResolution / 8, 1);
        shader.Dispatch(circlesHandle, count, 1, 1);
        shader.SetFloat("time", Time.time);
    }

    private void Update()
    {
        DispatchKernels(count);
    }

    private void OnDestroy()
    {
        buffer.Dispose();
    }
}

