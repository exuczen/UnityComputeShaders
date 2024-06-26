﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedFlocking : MonoBehaviour
{
    private const int SIZE_BOID = 7 * sizeof(float);

    private struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;

        public Boid(Vector3 pos, Vector3 dir, float offset)
        {
            position = pos;
            direction = dir;
            noise_offset = offset;
        }
    }

    public ComputeShader shader;
    public Material boidMaterial;
    public Mesh boidMesh;
    public Transform target;

    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public int boidsCount;
    public float spawnRadius;

    private int kernelHandle;
    private ComputeBuffer boidsBuffer = null;
    private ComputeBuffer argsBuffer = null;
    private readonly uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Boid[] boidsArray = null;
    private int groupSizeX;
    private int numOfBoids;
    private Bounds bounds;

    private void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        shader.GetKernelThreadGroupSizes(kernelHandle, out uint x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / x);
        numOfBoids = groupSizeX * (int)x;

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        InitBoids();
        InitShader();
    }

    private void InitBoids()
    {
        boidsArray = new Boid[numOfBoids];

        var flockPosition = transform.position;
        var flockRotation = transform.rotation;

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = flockPosition + Random.insideUnitSphere * spawnRadius;
            transform.rotation = Quaternion.Slerp(flockRotation, Random.rotation, 0.3f);
            float offset = Random.value * 1000.0f;
            boidsArray[i] = new Boid(pos, transform.forward, offset);
        }
        transform.rotation = flockRotation;
    }

    private void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, SIZE_BOID);
        boidsBuffer.SetData(boidsArray);

        //Initialize args buffer
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

        if (boidMesh)
        {
            args[0] = boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }
        argsBuffer.SetData(args);

        shader.SetBuffer(this.kernelHandle, "boidsBuffer", boidsBuffer);
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", numOfBoids);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
    }

    private void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer);
    }

    private void OnDestroy()
    {
        boidsBuffer?.Dispose();

        argsBuffer?.Dispose();
    }
}

