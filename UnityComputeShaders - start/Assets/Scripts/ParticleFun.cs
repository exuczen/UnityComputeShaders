﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649

public class ParticleFun : MonoBehaviour
{
    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
        public uint randomSeed;
    }

    const int SIZE_PARTICLE = 7 * sizeof(float) + sizeof(uint);

    public int particleCount = 1000000;
    public Material material;
    public ComputeShader shader;
    [Range(1, 10)]
    public int pointSize = 2;

    private int kernelID;
    private ComputeBuffer particleBuffer;

    private int groupSizeX;

    private Vector3 cursorPosition;

    // Use this for initialization
    private void Start()
    {
        Init();
    }

    private void Init()
    {
        GetCursorPosition();

        // initialize the particles
        Particle[] particleArray = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            //TODO: Initialize particle
            var pos = new Vector3
            {
                x = Random.Range(-1f, 1f),
                y = Random.Range(-1f, 1f),
                z = Random.Range(-1f, 1f)
            };
            pos = Random.Range(0f, 0.5f) * pos.normalized;
            pos.z += cursorPosition.z;
            particleArray[i].position = pos;
            particleArray[i].velocity = Vector3.zero;
            particleArray[i].life = Random.Range(1f, 6f);
            particleArray[i].randomSeed = (uint)(1664525 * i + 1013904223);
        }

        // create compute buffer
        particleBuffer = new ComputeBuffer(particleCount, SIZE_PARTICLE);

        particleBuffer.SetData(particleArray);

        // find the id of the kernel
        kernelID = shader.FindKernel("CSParticle");

        shader.GetKernelThreadGroupSizes(kernelID, out uint threadsX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / threadsX);

        // bind the compute buffer to the shader and the compute shader
        shader.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        material.SetBuffer("particleBuffer", particleBuffer);

        material.SetInt("_PointSize", pointSize);
    }

    private void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleCount);
    }

    private void OnDestroy()
    {
        particleBuffer?.Release();
    }

    // Update is called once per frame
    private void Update()
    {
        // Send datas to the compute shader
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetVector("mousePosition", cursorPosition);

        // Update the Particles
        shader.Dispatch(kernelID, groupSizeX, 1, 1);
    }

    private void OnGUI()
    {
        GetCursorPosition();
    }

    private Vector3 GetCursorPosition()
    {
        Camera c = Camera.main;
        Event e = Event.current;
        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        Vector2 mousePos = e != null ? new(e.mousePosition.x, c.pixelHeight - e.mousePosition.y) : Input.mousePosition;
        cursorPosition = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane + 7));
        return cursorPosition;
    }
}
