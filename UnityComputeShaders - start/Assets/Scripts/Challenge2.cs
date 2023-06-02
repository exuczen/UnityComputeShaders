﻿using UnityEngine;
using System.Collections;

public class Challenge2 : MonoBehaviour
{
    public ComputeShader shader = null;
    public int texResolution = 1024;
    public int sides = 5;

    private Renderer rend = null;
    private RenderTexture outputTexture = null;

    private int kernelHandle;

    public Color fillColor = new(1.0f, 1.0f, 0.0f, 1.0f);
    public Color clearColor = new(0, 0, 0.3f, 1.0f);

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0)
        {
            enableRandomWrite = true
        };
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitShader();
    }

    private void InitShader()
    {
        kernelHandle = shader.FindKernel("CSMain");

        shader.SetVector("fillColor", fillColor);
        shader.SetVector("clearColor", clearColor);

        shader.SetInt("sides", sides);
        shader.SetInt("texResolution", texResolution);
        shader.SetTexture(kernelHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);
    }

    private void DispatchShader(int x, int y)
    {
        shader.SetFloat("time", Time.time);
        shader.SetInt("sides", sides);
        shader.Dispatch(kernelHandle, x, y, 1);
    }

    void Update()
    {
        DispatchShader(texResolution / 8, texResolution / 8);
    }
}

